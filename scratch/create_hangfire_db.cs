using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace CreateHangfireDb
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                // Connect to the default database (usually 'postgres' or the default platform db) to run CREATE DATABASE
                string connString = "Host=217.216.91.10;Port=8087;Database=postgres;Username=zynstorm_ecf;Password='ZynstormECFPlatform'";
                
                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();
                    
                    // Check if db exists
                    bool exists;
                    using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'zynstorm_ecf_hangfire_db'", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        exists = result != null;
                    }
                    
                    if (!exists)
                    {
                        Console.WriteLine("Creating database zynstorm_ecf_hangfire_db...");
                        using (var cmd = new NpgsqlCommand("CREATE DATABASE zynstorm_ecf_hangfire_db", conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                        Console.WriteLine("Database created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Database already exists.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
