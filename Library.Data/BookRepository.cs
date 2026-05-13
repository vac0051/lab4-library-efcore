using System.Data;
using Library.Data.Entities;
using Microsoft.Data.SqlClient;

namespace Library.Data;

public sealed class BookRepository
{
    public async Task<List<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var bookDictionary = new Dictionary<int, Book>();

        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT b.Id, b.Title, b.PublicationYear, b.AuthorId,
                   a.Name AS AuthorName,
                   g.Id AS GenreId, g.Name AS GenreName
            FROM Books b
            INNER JOIN Authors a ON b.AuthorId = a.Id
            LEFT JOIN BookGenres bg ON b.Id = bg.BookId
            LEFT JOIN Genres g ON bg.GenreId = g.Id
            ORDER BY b.Title";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            int bookId = reader.GetInt32(0);
            if (!bookDictionary.TryGetValue(bookId, out var book))
            {
                book = new Book
                {
                    Id = bookId,
                    Title = reader.GetString(1),
                    PublicationYear = reader.GetInt32(2),
                    AuthorId = reader.GetInt32(3),
                    Author = new Author
                    {
                        Id = reader.GetInt32(3),
                        Name = reader.GetString(4)
                    }
                };
                bookDictionary.Add(bookId, book);
            }

            if (!reader.IsDBNull(5))
            {
                book.Genres.Add(new Genre
                {
                    Id = reader.GetInt32(5),
                    Name = reader.GetString(6)
                });
            }
        }

        return bookDictionary.Values.OrderBy(b => b.Title).ToList();
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Book? book = null;

        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT b.Id, b.Title, b.PublicationYear, b.AuthorId,
                   a.Name AS AuthorName,
                   g.Id AS GenreId, g.Name AS GenreName
            FROM Books b
            INNER JOIN Authors a ON b.AuthorId = a.Id
            LEFT JOIN BookGenres bg ON b.Id = bg.BookId
            LEFT JOIN Genres g ON bg.GenreId = g.Id
            WHERE b.Id = @Id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (book == null)
            {
                book = new Book
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    PublicationYear = reader.GetInt32(2),
                    AuthorId = reader.GetInt32(3),
                    Author = new Author
                    {
                        Id = reader.GetInt32(3),
                        Name = reader.GetString(4)
                    }
                };
            }

            if (!reader.IsDBNull(5))
            {
                book.Genres.Add(new Genre
                {
                    Id = reader.GetInt32(5),
                    Name = reader.GetString(6)
                });
            }
        }

        return book;
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction();

        try
        {
            var insertBookSql = @"
                INSERT INTO Books (Title, PublicationYear, AuthorId) 
                OUTPUT INSERTED.Id 
                VALUES (@Title, @PublicationYear, @AuthorId)";
                
            await using var insertBookCmd = new SqlCommand(insertBookSql, connection, transaction);
            insertBookCmd.Parameters.AddWithValue("@Title", book.Title);
            insertBookCmd.Parameters.AddWithValue("@PublicationYear", book.PublicationYear);
            insertBookCmd.Parameters.AddWithValue("@AuthorId", book.AuthorId);

            book.Id = (int)(await insertBookCmd.ExecuteScalarAsync(cancellationToken))!;

            if (book.Genres.Any())
            {
                var insertGenresSql = "INSERT INTO BookGenres (BookId, GenreId) VALUES (@BookId, @GenreId)";
                await using var insertGenresCmd = new SqlCommand(insertGenresSql, connection, transaction);
                var bookIdParam = insertGenresCmd.Parameters.Add("@BookId", SqlDbType.Int);
                var genreIdParam = insertGenresCmd.Parameters.Add("@GenreId", SqlDbType.Int);

                foreach (var genre in book.Genres)
                {
                    bookIdParam.Value = book.Id;
                    genreIdParam.Value = genre.Id;
                    await insertGenresCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction();

        try
        {
            var updateBookSql = @"
                UPDATE Books 
                SET Title = @Title, PublicationYear = @PublicationYear, AuthorId = @AuthorId 
                WHERE Id = @Id";
                
            await using var updateBookCmd = new SqlCommand(updateBookSql, connection, transaction);
            updateBookCmd.Parameters.AddWithValue("@Id", book.Id);
            updateBookCmd.Parameters.AddWithValue("@Title", book.Title);
            updateBookCmd.Parameters.AddWithValue("@PublicationYear", book.PublicationYear);
            updateBookCmd.Parameters.AddWithValue("@AuthorId", book.AuthorId);

            await updateBookCmd.ExecuteNonQueryAsync(cancellationToken);

            var deleteGenresSql = "DELETE FROM BookGenres WHERE BookId = @BookId";
            await using var deleteGenresCmd = new SqlCommand(deleteGenresSql, connection, transaction);
            deleteGenresCmd.Parameters.AddWithValue("@BookId", book.Id);
            await deleteGenresCmd.ExecuteNonQueryAsync(cancellationToken);

            if (book.Genres.Any())
            {
                var insertGenresSql = "INSERT INTO BookGenres (BookId, GenreId) VALUES (@BookId, @GenreId)";
                await using var insertGenresCmd = new SqlCommand(insertGenresSql, connection, transaction);
                var bookIdParam = insertGenresCmd.Parameters.Add("@BookId", SqlDbType.Int);
                var genreIdParam = insertGenresCmd.Parameters.Add("@GenreId", SqlDbType.Int);

                foreach (var genre in book.Genres)
                {
                    bookIdParam.Value = book.Id;
                    genreIdParam.Value = genre.Id;
                    await insertGenresCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "DELETE FROM Books WHERE Id = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = ConnDB.GetConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT COUNT(1) FROM Books";
        await using var command = new SqlCommand(sql, connection);
        
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }
}
