using System.Data;
using Library.Data.Entities;
using Microsoft.Data.SqlClient;

namespace Library.Data;

public sealed class GenreRepository
{
    public async Task<List<Genre>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var genres = new List<Genre>();
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT Id, Name FROM Genres", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            genres.Add(new Genre
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return genres;
    }

    public async Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT Id, Name FROM Genres WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Genre
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        return null;
    }

    public async Task<List<Genre>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return new List<Genre>();

        var genres = new List<Genre>();
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = string.Join(",", idList.Select((id, i) => $"@p{i}"));
        var sql = $"SELECT Id, Name FROM Genres WHERE Id IN ({parameters})";

        await using var command = new SqlCommand(sql, connection);
        for (int i = 0; i < idList.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", idList[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            genres.Add(new Genre
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return genres;
    }

    public async Task AddAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "INSERT INTO Genres (Name) OUTPUT INSERTED.Id VALUES (@Name)";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", genre.Name);

        genre.Id = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "UPDATE Genres SET Name = @Name WHERE Id = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", genre.Id);
        command.Parameters.AddWithValue("@Name", genre.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "DELETE FROM Genres WHERE Id = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT COUNT(1) FROM Genres";
        await using var command = new SqlCommand(sql, connection);
        
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }
}
