using Microsoft.Data.SqlClient;

namespace Library.Data;

public static class ConnDB
{
    public static string ConnectionString { get; set; } = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServerConnection") 
        ?? @"Server=ZOMBIE_PASTA\SQLEXPRESS;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;";

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}
