using System.Data;
using Library.Data.Entities;
using Microsoft.Data.SqlClient;

namespace Library.Data;

public sealed class AuthorRepository
{
    public async Task<List<Author>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var authors = new List<Author>();
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT Id, Name FROM Authors", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            authors.Add(new Author
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return authors;
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT Id, Name FROM Authors WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Author
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        return null;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT COUNT(1) FROM Authors WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    public async Task AddAsync(Author author, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "INSERT INTO Authors (Name) OUTPUT INSERTED.Id VALUES (@Name)";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", author.Name);

        author.Id = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task UpdateAsync(Author author, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "UPDATE Authors SET Name = @Name WHERE Id = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", author.Id);
        command.Parameters.AddWithValue("@Name", author.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "DELETE FROM Authors WHERE Id = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT COUNT(1) FROM Authors";
        await using var command = new SqlCommand(sql, connection);
        
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }
}
