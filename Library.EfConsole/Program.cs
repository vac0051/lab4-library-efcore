using Library.Data;
using Library.Data.Entities;

Console.WriteLine("ЛР4: Примеры запросов через ADO.NET (ConnDB)\n");

var bookRepo = new BookRepository();
var genreRepo = new GenreRepository();

var allBooks = await bookRepo.GetAllAsync();
var allGenres = await genreRepo.GetAllAsync();

var booksByGenre = allGenres
    .Select(genre => new
    {
        GenreName = genre.Name,
        BookTitles = allBooks
            .Where(b => b.Genres.Any(g => g.Id == genre.Id))
            .Select(b => b.Title)
            .OrderBy(t => t)
            .ToList()
    })
    .OrderBy(item => item.GenreName)
    .ToList();

Console.WriteLine("1) Группировка книг по жанрам:");
foreach (var genreInfo in booksByGenre)
{
    Console.WriteLine($"- {genreInfo.GenreName}: {string.Join(", ", genreInfo.BookTitles)}");
}

const int minPublicationYear = 1950;
var filteredBooks = allBooks
    .Where(book => book.PublicationYear >= minPublicationYear)
    .OrderBy(book => book.PublicationYear)
    .ThenBy(book => book.Title)
    .ToList();

Console.WriteLine($"\n2) Фильтрация книг по году издания >= {minPublicationYear}:");
foreach (var book in filteredBooks)
{
    Console.WriteLine($"- {book.PublicationYear}: {book.Title} ({book.Author?.Name})");
}

Console.WriteLine("\nГотово. База данных: LibraryDB на SQL Server");
