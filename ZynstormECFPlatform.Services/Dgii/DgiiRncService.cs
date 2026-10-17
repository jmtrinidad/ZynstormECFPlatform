using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZynstormECFPlatform.Services.Dgii;

public class DgiiRncService : IDgiiRncService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static bool _isDownloading;
    private static bool _isIndexing;
    private static string? _statusMessage;
    private static string? _lastError;

    private readonly IConfiguration _configuration;
    private readonly ILogger<DgiiRncService> _logger;
    private readonly HttpClient _httpClient;

    private const string DgiiZipUrl = "https://dgii.gov.do/app/WebApps/Consultas/RNC/DGII_RNC.zip";

    public DgiiRncService(
        IConfiguration configuration,
        ILogger<DgiiRncService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("DGII");
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    private string DgiiFolder
    {
        get
        {
            // Misma convencion que los APIs de EasyInvoice: el padron vive en uploads/dgii y
            // en Dokploy /app/uploads es el volumen "<appName>-uploads" que monta
            // dokploy-agregar-volumen.cmd. DGII_ROOT permite apuntarlo a otra ruta montada.
            var folder = (_configuration["DGII_ROOT"] ?? _configuration["Dgii:RootPath"]) is { Length: > 0 } dgiiRoot
                ? dgiiRoot
                : Path.Combine(UploadsRoot, "dgii");

            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    // UPLOADS_ROOT > Uploads:RootPath > <app>/uploads. En el contenedor la app se publica en
    // /app, asi que el valor por defecto cae justo en el volumen /app/uploads.
    private string UploadsRoot =>
        (_configuration["UPLOADS_ROOT"] ?? _configuration["Uploads:RootPath"]) is { Length: > 0 } p
            ? p
            : Path.Combine(AppContext.BaseDirectory, "uploads");

    private string DbFilePath => Path.Combine(DgiiFolder, "dgii_rnc.db");
    private string TempDbFilePath => Path.Combine(DgiiFolder, "dgii_rnc_temp.db");
    private string ZipFilePath => Path.Combine(DgiiFolder, "DGII_RNC.zip");
    private string StatusFilePath => Path.Combine(DgiiFolder, "dgii_status.json");
    private string ExtractTempFolder => Path.Combine(DgiiFolder, "extracted_tmp");

    public Task<DgiiRncStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var status = LoadStatusFromFile();

        // El JSON de estado es solo un cache: si se perdio pero el .db sigue ahi, el padron
        // esta cargado. Sin esto un status.json faltante hace creer que no hay datos y
        // dispara una descarga de 60 MB que no hacia falta.
        if (status.TotalRecords <= 0)
        {
            var indexed = CountIndexedRecords();
            if (indexed > 0)
            {
                status.TotalRecords = indexed;
                status.LastUpdated ??= File.GetLastWriteTime(DbFilePath);
            }
        }

        status.IsDownloading = _isDownloading;
        status.IsIndexing = _isIndexing;
        status.StatusMessage = _statusMessage ?? GetCurrentStatusLabel(status);
        status.LastError = _lastError;

        return Task.FromResult(status);
    }

    // Cuenta lo que hay realmente indexado. Devuelve 0 si el archivo no existe o esta
    // corrupto: la verdad la tiene el .db, no el JSON de estado.
    private long CountIndexedRecords()
    {
        if (!File.Exists(DbFilePath))
            return 0;

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DbFilePath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM rnc_records;";

            return Convert.ToInt64(command.ExecuteScalar() ?? 0L);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo contar los registros indexados del padron de la DGII.");
            return 0;
        }
    }

    public Task StartDownloadAndIndexingAsync(CancellationToken ct = default)
    {
        if (_isDownloading || _isIndexing)
        {
            return Task.CompletedTask;
        }

        // Se lanza sin esperar: el botón de Configuraciones y el arranque responden de
        // inmediato y el avance se sigue con GetStatusAsync.
        _ = Task.Run(async () =>
        {
            try
            {
                await RunDownloadAndIndexingAsync(ct);
            }
            catch
            {
                // Ya quedó en el log y en LastError. Aquí no hay a quién avisar: quien
                // llamó no está esperando.
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task<bool> RunDownloadAndIndexingAsync(CancellationToken ct = default)
    {
        if (_isDownloading || _isIndexing)
        {
            return false;
        }

        await _lock.WaitAsync(ct);
        try
        {
            _isDownloading = true;
            _lastError = null;

            // Restos de una corrida que murió sin pasar por el catch: un redeploy, un
            // OOM o el contenedor reiniciado a media descarga. Con la actualización
            // diaria desatendida nadie está mirando para limpiarlos a mano.
            CleanupWorkFiles();

            _statusMessage = "Descargando archivo ZIP desde la DGII...";
            _logger.LogInformation("Iniciando descarga de DGII_RNC.zip desde {Url}", DgiiZipUrl);

            var response = await _httpClient.GetAsync(DgiiZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(ZipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            _isDownloading = false;
            _isIndexing = true;
            _statusMessage = "Descomprimiendo archivo ZIP...";
            _logger.LogInformation("Archivo descargado con éxito. Descomprimiendo...");

            ExtractZip();

            _statusMessage = "Indexando datos de la DGII en SQLite...";
            _logger.LogInformation("Indexando registros de la DGII...");

            var totalRecords = IndexTxtFileToSqlite();

            // Recien aca se borra el TXT: mientras duraba la indexacion era la unica
            // fuente para responder consultas (ver el fallback de LookupRncAsync). Con el
            // .db ya cargado sobra, y son 90 MB mas un escaneo linea por linea en cada RNC
            // que no exista. Si la indexacion no produjo nada se conserva.
            if (totalRecords > 0)
            {
                DeleteTxtSource();
            }

            var status = new DgiiRncStatusDto
            {
                IsDownloading = false,
                IsIndexing = false,
                LastUpdated = DateTime.Now,
                TotalRecords = totalRecords,
                StatusMessage = $"Padrón actualizado exitosamente ({totalRecords:N0} registros cargados).",
                LastError = null
            };

            SaveStatusToFile(status);
            _statusMessage = status.StatusMessage;
            _logger.LogInformation("Indexación de DGII completada. Total registros: {Count}", totalRecords);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando o indexando padrón de la DGII.");
            _lastError = ex.Message;
            _statusMessage = $"Error: {ex.Message}";

            // Una corrida fallida deja el ZIP a medias y la base temporal, que puede pesar
            // tanto como la definitiva. Sin esto se acumulan en cada actualizacion que falle.
            // El TXT no se toca: si quedo completo sigue sirviendo de respaldo para
            // responder consultas mientras no haya .db nuevo.
            DeleteQuietly(ZipFilePath);
            DeleteQuietly(TempDbFilePath);

            // Se relanza para que el job diario lo vea y reintente. Quien llama por el
            // camino fire-and-forget se lo traga.
            throw;
        }
        finally
        {
            _isDownloading = false;
            _isIndexing = false;
            _lock.Release();
        }
    }

    /// <summary>
    /// Borra los archivos de trabajo que hayan quedado de corridas anteriores. El TXT solo
    /// se toca cuando ya hay un índice cargado: sin .db es la única fuente que responde
    /// consultas, y borrarlo dejaría al sistema sin padrón mientras dura la descarga.
    /// </summary>
    private void CleanupWorkFiles()
    {
        DeleteQuietly(ZipFilePath);
        DeleteQuietly(TempDbFilePath);

        try
        {
            if (Directory.Exists(ExtractTempFolder))
            {
                Directory.Delete(ExtractTempFolder, true);
                _logger.LogInformation("Carpeta temporal de extracción de la DGII eliminada: {Path}", ExtractTempFolder);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo eliminar la carpeta temporal {Path}.", ExtractTempFolder);
        }

        if (CountIndexedRecords() > 0)
        {
            DeleteTxtSource();
        }
    }

    public async Task<DgiiRncSearchResultDto> LookupRncAsync(string rnc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rnc))
        {
            return new DgiiRncSearchResultDto { Found = false };
        }

        var cleanRnc = CleanRncString(rnc);
        if (string.IsNullOrEmpty(cleanRnc))
        {
            return new DgiiRncSearchResultDto { Found = false };
        }

        // 1. Intentar buscar en la base de datos SQLite indexada (< 1 ms)
        if (File.Exists(DbFilePath))
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder
                {
                    DataSource = DbFilePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Cache = SqliteCacheMode.Shared,
                    Pooling = false
                }.ToString();

                await using var connection = new SqliteConnection(connStr);
                await connection.OpenAsync(ct);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT rnc, name, commercial_name, activity, status, regime
                    FROM rnc_records
                    WHERE rnc_clean = @rnc
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@rnc", cleanRnc);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var foundRnc = reader.IsDBNull(0) ? cleanRnc : reader.GetString(0);
                    var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var commercialName = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var activity = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var status = reader.IsDBNull(4) ? null : reader.GetString(4);
                    var regime = reader.IsDBNull(5) ? null : reader.GetString(5);

                    return BuildSearchResult(foundRnc, name, commercialName, activity, status, regime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consultando base de datos SQLite DGII RNC. Intentando fallback.");
            }
        }

        // 2. Fallback: Buscar directamente en el archivo .txt si aún no se ha completado la indexación
        var txtPath = LocateTxtFile();
        if (txtPath != null && File.Exists(txtPath))
        {
            var fallbackResult = await Task.Run(() => ScanTxtFileForRnc(txtPath, cleanRnc), ct);
            if (fallbackResult != null)
            {
                return fallbackResult;
            }
        }

        return new DgiiRncSearchResultDto { Found = false, Rnc = cleanRnc };
    }

    private void ExtractZip()
    {
        var tempExtractDir = ExtractTempFolder;
        if (Directory.Exists(tempExtractDir))
        {
            Directory.Delete(tempExtractDir, true);
        }

        ZipFile.ExtractToDirectory(ZipFilePath, tempExtractDir, overwriteFiles: true);

        // DGII_RNC.zip suele descomprimir una carpeta TMP/DGII_RNC.TXT o directamente DGII_RNC.TXT
        var targetTmpDir = Path.Combine(DgiiFolder, "TMP");
        Directory.CreateDirectory(targetTmpDir);

        var extractedFiles = Directory.GetFiles(tempExtractDir, "*.*", SearchOption.AllDirectories);
        foreach (var file in extractedFiles)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("DGII_RNC.TXT", StringComparison.OrdinalIgnoreCase))
            {
                var destPath = Path.Combine(targetTmpDir, "DGII_RNC.TXT");
                File.Copy(file, destPath, overwrite: true);
            }
        }

        try
        {
            Directory.Delete(tempExtractDir, true);
        }
        catch { }

        // El ZIP ya cumplio: nadie mas lo lee y son ~22 MB por descarga. Se borra aca y no
        // en el flujo de descarga para que tambien limpie si alguien llama a ExtractZip solo.
        DeleteQuietly(ZipFilePath);
    }

    // Quita el TXT descomprimido y la carpeta TMP que lo contiene.
    private void DeleteTxtSource()
    {
        var txtPath = LocateTxtFile();

        if (txtPath is not null)
        {
            DeleteQuietly(txtPath);
        }

        var tmpDir = Path.Combine(DgiiFolder, "TMP");

        try
        {
            if (Directory.Exists(tmpDir) && Directory.GetFileSystemEntries(tmpDir).Length == 0)
            {
                Directory.Delete(tmpDir);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo eliminar la carpeta temporal TMP de la DGII.");
        }
    }

    // Borra sin romper el proceso: limpiar basura nunca debe tumbar una descarga que ya salio
    // bien.
    private void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("Archivo temporal de la DGII eliminado: {Path}", path);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo eliminar el archivo temporal {Path}.", path);
        }
    }

    private long IndexTxtFileToSqlite()
    {
        var txtPath = LocateTxtFile();
        if (txtPath == null || !File.Exists(txtPath))
        {
            throw new FileNotFoundException("No se encontró el archivo DGII_RNC.TXT descomprimido.");
        }

        if (File.Exists(TempDbFilePath))
        {
            File.Delete(TempDbFilePath);
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252); // Windows-1252 para dominar tildes y caracteres especiales de DGII

            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = TempDbFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

            long count = 0;

            using (var connection = new SqliteConnection(connStr))
            {
                connection.Open();

                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";
                    pragmaCmd.ExecuteNonQuery();
                }

                using (var createTableCmd = connection.CreateCommand())
                {
                    createTableCmd.CommandText = @"
                        CREATE TABLE rnc_records (
                            rnc_clean TEXT PRIMARY KEY,
                            rnc TEXT,
                            name TEXT,
                            commercial_name TEXT,
                            activity TEXT,
                            status TEXT,
                            regime TEXT
                        );";
                    createTableCmd.ExecuteNonQuery();
                }

                using (var stream = new FileStream(txtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
                using (var reader = new StreamReader(stream, encoding))
                {
                    SqliteTransaction? tx = null;
                    SqliteCommand? insertCmd = null;

                    try
                    {
                        tx = connection.BeginTransaction();
                        insertCmd = connection.CreateCommand();
                        insertCmd.Transaction = tx;
                        insertCmd.CommandText = @"
                            INSERT OR REPLACE INTO rnc_records (rnc_clean, rnc, name, commercial_name, activity, status, regime)
                            VALUES (@rnc_clean, @rnc, @name, @commercial_name, @activity, @status, @regime);";

                        var pClean = insertCmd.Parameters.Add("@rnc_clean", SqliteType.Text);
                        var pRnc = insertCmd.Parameters.Add("@rnc", SqliteType.Text);
                        var pName = insertCmd.Parameters.Add("@name", SqliteType.Text);
                        var pComm = insertCmd.Parameters.Add("@commercial_name", SqliteType.Text);
                        var pAct = insertCmd.Parameters.Add("@activity", SqliteType.Text);
                        var pStat = insertCmd.Parameters.Add("@status", SqliteType.Text);
                        var pReg = insertCmd.Parameters.Add("@regime", SqliteType.Text);

                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = line.Split('|');
                            if (parts.Length < 2) continue;

                            var rawRnc = parts[0].Trim();
                            var cleanRnc = CleanRncString(rawRnc);
                            if (string.IsNullOrEmpty(cleanRnc)) continue;

                            var name = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                            var commercialName = parts.Length > 2 ? CleanField(parts[2]) : null;
                            var activity = parts.Length > 3 ? CleanField(parts[3]) : null;
                            var status = parts.Length > 9 ? CleanField(parts[9]) : null;
                            var regime = parts.Length > 10 ? CleanField(parts[10]) : null;

                            pClean.Value = cleanRnc;
                            pRnc.Value = rawRnc;
                            pName.Value = name;
                            pComm.Value = (object?)commercialName ?? DBNull.Value;
                            pAct.Value = (object?)activity ?? DBNull.Value;
                            pStat.Value = (object?)status ?? DBNull.Value;
                            pReg.Value = (object?)regime ?? DBNull.Value;

                            insertCmd.ExecuteNonQuery();
                            count++;

                            if (count % 50000 == 0)
                            {
                                tx.Commit();
                                tx.Dispose();
                                tx = connection.BeginTransaction();
                                insertCmd.Transaction = tx;
                            }
                        }

                        if (tx != null)
                        {
                            tx.Commit();
                        }
                    }
                    finally
                    {
                        insertCmd?.Dispose();
                        tx?.Dispose();
                    }
                }

                // Crear índice explícito para acelerar aún más las búsquedas
                using (var indexCmd = connection.CreateCommand())
                {
                    indexCmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_rnc_clean ON rnc_records(rnc_clean);";
                    indexCmd.ExecuteNonQuery();
                }
            }

            // Reemplazar la base de datos principal atómicamente con manejo de bloqueos
            ReplaceDatabaseFile(TempDbFilePath, DbFilePath);

            return count;
        }

        private void ReplaceDatabaseFile(string tempDbPath, string targetDbPath)
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (File.Exists(targetDbPath))
                    {
                        File.Delete(targetDbPath);
                    }
                    File.Move(tempDbPath, targetDbPath, overwrite: true);
                    return;
                }
                catch (IOException ex) when (i < 4)
                {
                    _logger.LogWarning(ex, "Archivo SQLite ocupado en intento {Attempt}. Reintentando...", i + 1);
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(500 * (i + 1));
                }
            }

            File.Move(tempDbPath, targetDbPath, overwrite: true);
        }

    private DgiiRncSearchResultDto? ScanTxtFileForRnc(string txtPath, string cleanRnc)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(1252);

            using var stream = new FileStream(txtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, encoding);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length < 2) continue;

                var rawRnc = parts[0].Trim();
                if (CleanRncString(rawRnc) == cleanRnc)
                {
                    var name = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    var commercialName = parts.Length > 2 ? CleanField(parts[2]) : null;
                    var activity = parts.Length > 3 ? CleanField(parts[3]) : null;
                    var status = parts.Length > 9 ? CleanField(parts[9]) : null;
                    var regime = parts.Length > 10 ? CleanField(parts[10]) : null;

                    return BuildSearchResult(rawRnc, name, commercialName, activity, status, regime);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error durante fallback de escaneo directo en TXT de DGII.");
        }

        return null;
    }

    private static DgiiRncSearchResultDto BuildSearchResult(
        string rawRnc,
        string name,
        string? commercialName,
        string? activity,
        string? status,
        string? regime)
    {
        return new DgiiRncSearchResultDto
        {
            Found = true,
            Rnc = rawRnc,
            Name = name,
            CommercialName = commercialName,
            Activity = activity,
            Status = status,
            Regime = regime
        };
    }

    private string? LocateTxtFile()
    {
        var tmpPath = Path.Combine(DgiiFolder, "TMP", "DGII_RNC.TXT");
        if (File.Exists(tmpPath)) return tmpPath;

        var rootPath = Path.Combine(DgiiFolder, "DGII_RNC.TXT");
        if (File.Exists(rootPath)) return rootPath;

        var files = Directory.GetFiles(DgiiFolder, "*.TXT", SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }

    private static string CleanRncString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    private static string? CleanField(string field)
    {
        var trimmed = field.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private DgiiRncStatusDto LoadStatusFromFile()
    {
        if (!File.Exists(StatusFilePath))
        {
            return new DgiiRncStatusDto
            {
                IsDownloading = false,
                IsIndexing = false,
                TotalRecords = 0,
                StatusMessage = "Base de datos RNC de la DGII no ha sido descargada aún."
            };
        }

        try
        {
            var json = File.ReadAllText(StatusFilePath);
            var status = JsonSerializer.Deserialize<DgiiRncStatusDto>(json);
            return status ?? new DgiiRncStatusDto();
        }
        catch
        {
            return new DgiiRncStatusDto();
        }
    }

    private void SaveStatusToFile(DgiiRncStatusDto status)
    {
        try
        {
            var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StatusFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar el archivo de estado de la DGII.");
        }
    }

    private static string GetCurrentStatusLabel(DgiiRncStatusDto status)
    {
        if (status.LastUpdated.HasValue && status.TotalRecords > 0)
        {
            return $"Padrón actualizado al {status.LastUpdated.Value:dd/MM/yyyy HH:mm} ({status.TotalRecords:N0} registros disponibles).";
        }
        return "Base de datos RNC de la DGII disponible para descargar.";
    }
}
