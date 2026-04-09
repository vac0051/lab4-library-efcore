using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<LibraryContext>()
    .UseSqlite("Data Source=library.db")
    .Options;

await using var context = new LibraryContext(options);
await context.Database.MigrateAsync();

await SeedDataIfRequiredAsync(context);

Console.WriteLine("ЛР4: Примеры LINQ-запросов\n");

var booksByGenre = await context.Genres
    .Select(genre => new
    {
        GenreName = genre.Name,
        BookTitles = genre.Books.Select(book => book.Title).OrderBy(title => title).ToList()
    })
    .OrderBy(item => item.GenreName)
    .ToListAsync();

Console.WriteLine("1) Группировка книг по жанрам:");
foreach (var genreInfo in booksByGenre)
{
    Console.WriteLine($"- {genreInfo.GenreName}: {string.Join(", ", genreInfo.BookTitles)}");
}

const int minPublicationYear = 1950;
var filteredBooks = await context.Books
    .Include(book => book.Author)
    .Where(book => book.PublicationYear >= minPublicationYear)
    .OrderBy(book => book.PublicationYear)
    .ThenBy(book => book.Title)
    .ToListAsync();

Console.WriteLine($"\n2) Фильтрация книг по году издания >= {minPublicationYear}:");
foreach (var book in filteredBooks)
{
    Console.WriteLine($"- {book.PublicationYear}: {book.Title} ({book.Author?.Name})");
}

Console.WriteLine("\nГотово. База данных: library.db");

static async Task SeedDataIfRequiredAsync(LibraryContext context)
{
    if (await context.Books.AnyAsync())
    {
        return;
    }

    var sciFi = new Genre { Name = "Sci-Fi" };
    var fantasy = new Genre { Name = "Fantasy" };
    var drama = new Genre { Name = "Drama" };

    var asimov = new Author { Name = "Isaac Asimov" };
    var tolkien = new Author { Name = "J.R.R. Tolkien" };
    var orwell = new Author { Name = "George Orwell" };

    var foundation = new Book
    {
        Title = "Foundation",
        PublicationYear = 1951,
        Author = asimov,
        Genres = new List<Genre> { sciFi }
    };

    var hobbit = new Book
    {
        Title = "The Hobbit",
        PublicationYear = 1937,
        Author = tolkien,
        Genres = new List<Genre> { fantasy }
    };

    var animalFarm = new Book
    {
        Title = "Animal Farm",
        PublicationYear = 1945,
        Author = orwell,
        Genres = new List<Genre> { drama }
    };

    var lotr = new Book
    {
        Title = "The Lord of the Rings",
        PublicationYear = 1954,
        Author = tolkien,
        Genres = new List<Genre> { fantasy, drama }
    };

    context.Books.AddRange(foundation, hobbit, animalFarm, lotr);
    await context.SaveChangesAsync();
}
