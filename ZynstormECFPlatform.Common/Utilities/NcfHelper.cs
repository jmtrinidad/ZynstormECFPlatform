namespace ZynstormECFPlatform.Common.Utilities;

/// <summary>
/// Utility class for handling e-NCF (Número de Comprobante Fiscal Electrónico) operations.
/// DGII format: E + TipoeCF (2 digits) + Sequence (10 digits) = 13 characters total.
/// Example: E310000000001 → TipoeCF = 31
/// </summary>
public static class NcfHelper
{
    private const int NcfLength = 13;
    private const char NcfPrefix = 'E';
    private const int TypeStartIndex = 1;
    private const int TypeLength = 2;

    private static readonly HashSet<int> ValidTypes = [31, 32, 33, 34, 41, 43, 44, 45, 46, 47];

    /// <summary>
    /// Extracts the e-CF type code from a valid eNCF string.
    /// </summary>
    /// <param name="ncf">The 13-character eNCF string (e.g., "E310000000001").</param>
    /// <returns>The integer TipoeCF (e.g., 31, 32, 33).</returns>
    /// <exception cref="ArgumentException">Thrown when the NCF is null, empty, wrong length, or has an invalid prefix/type.</exception>
    public static int ExtractEcfType(string ncf)
    {
        if (string.IsNullOrWhiteSpace(ncf))
            throw new ArgumentException("El eNCF no puede estar vacío.", nameof(ncf));

        if (ncf.Length != NcfLength)
            throw new ArgumentException(
                $"El eNCF debe tener exactamente {NcfLength} caracteres. Recibido: '{ncf}' ({ncf.Length} chars).",
                nameof(ncf));

        if (char.ToUpperInvariant(ncf[0]) != NcfPrefix)
            throw new ArgumentException(
                $"El eNCF debe comenzar con '{NcfPrefix}'. Recibido: '{ncf[0]}'.",
                nameof(ncf));

        var typeStr = ncf.Substring(TypeStartIndex, TypeLength);

        if (!int.TryParse(typeStr, out var ecfType))
            throw new ArgumentException(
                $"Los caracteres de tipo en el eNCF (posición 1-2) deben ser numéricos. Recibido: '{typeStr}'.",
                nameof(ncf));

        if (!ValidTypes.Contains(ecfType))
            throw new ArgumentException(
                $"TipoeCF '{ecfType}' no es un tipo e-CF válido según la DGII. " +
                $"Tipos permitidos: {string.Join(", ", ValidTypes)}.",
                nameof(ncf));

        return ecfType;
    }

    /// <summary>
    /// Tries to extract the e-CF type code from the NCF string without throwing.
    /// </summary>
    public static bool TryExtractEcfType(string? ncf, out int ecfType)
    {
        ecfType = 0;
        try
        {
            if (ncf is null) return false;
            ecfType = ExtractEcfType(ncf);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
