using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Data;
using Library.Data.Entities;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== ЛР4: Управление библиотекой через ADO.NET ===\n");

var bookRepo = new BookRepository();
var genreRepo = new GenreRepository();
var authorRepo = new AuthorRepository();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n--- МЕНЮ УПРАВЛЕНИЯ БИБЛИОТЕКОЙ ---");
    Console.ResetColor();
    Console.WriteLine("1. Вывести список всех книг");
    Console.WriteLine("2. Группировка книг по жанрам (запрос 1)");
    Console.WriteLine("3. Фильтрация книг по году издания (запрос 2)");
    Console.WriteLine("4. Добавить новую книгу");
    Console.WriteLine("5. Добавить автора");
    Console.WriteLine("6. Добавить жанр");
    Console.WriteLine("7. Удалить книгу по ID");
    Console.WriteLine("0. Выход");
    Console.Write("Выберите действие: ");

    var choice = Console.ReadLine();
    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1":
                await ListAllBooksAsync(bookRepo);
                break;
            case "2":
                await GroupBooksByGenreAsync(bookRepo, genreRepo);
                break;
            case "3":
                await FilterBooksByYearAsync(bookRepo);
                break;
            case "4":
                await AddNewBookAsync(bookRepo, authorRepo, genreRepo);
                break;
            case "5":
                await AddNewAuthorAsync(authorRepo);
                break;
            case "6":
                await AddNewGenreAsync(genreRepo);
                break;
            case "7":
                await DeleteBookAsync(bookRepo);
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Ошибка] Неизвестный пункт меню.");
                Console.ResetColor();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Ошибка] {ex.Message}");
        Console.ResetColor();
    }
}

async Task ListAllBooksAsync(BookRepository bookRepository)
{
    Console.WriteLine("\n--- СПИСОК ВСЕХ КНИГ ---");
    var books = await bookRepository.GetAllAsync();
    if (!books.Any())
    {
        Console.WriteLine("В библиотеке пока нет книг.");
        return;
    }

    foreach (var b in books)
    {
        var genres = b.Genres.Any() ? string.Join(", ", b.Genres.Select(g => g.Name)) : "нет жанров";
        Console.WriteLine($"ID: {b.Id} | \"{b.Title}\" | Автор: {b.Author?.Name} | Год: {b.PublicationYear} | Жанры: {genres}");
    }
}

async Task GroupBooksByGenreAsync(BookRepository bookRepository, GenreRepository genreRepository)
{
    Console.WriteLine("\n--- ГРУППИРОВКА КНИГ ПО ЖАНРАМ ---");
    var allBooks = await bookRepository.GetAllAsync();
    var allGenres = await genreRepository.GetAllAsync();

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

    foreach (var genreInfo in booksByGenre)
    {
        var bookList = genreInfo.BookTitles.Any() ? string.Join(", ", genreInfo.BookTitles) : "[Нет книг]";
        Console.WriteLine($"- {genreInfo.GenreName}: {bookList}");
    }
}

async Task FilterBooksByYearAsync(BookRepository bookRepository)
{
    Console.Write("\nВведите минимальный год публикации для фильтрации: ");
    if (!int.TryParse(Console.ReadLine(), out int year))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Некорректный формат года.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"\n--- КНИГИ, ВЫПУЩЕННЫЕ В {year} ГОДУ И ПОЗЖЕ ---");
    var allBooks = await bookRepository.GetAllAsync();
    var filteredBooks = allBooks
        .Where(book => book.PublicationYear >= year)
        .OrderBy(book => book.PublicationYear)
        .ThenBy(book => book.Title)
        .ToList();

    if (!filteredBooks.Any())
    {
        Console.WriteLine("Книг, подходящих под критерий, не найдено.");
        return;
    }

    foreach (var book in filteredBooks)
    {
        Console.WriteLine($"- {book.PublicationYear}: \"{book.Title}\" (Автор: {book.Author?.Name})");
    }
}

async Task AddNewBookAsync(BookRepository bookRepository, AuthorRepository authorRepository, GenreRepository genreRepository)
{
    Console.WriteLine("\n--- ДОБАВЛЕНИЕ НОВОЙ КНИГИ ---");
    
    // Ввод названия
    Console.Write("Введите название книги: ");
    var title = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(title))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Название не может быть пустым.");
        Console.ResetColor();
        return;
    }

    // Ввод года
    Console.Write("Введите год издания: ");
    if (!int.TryParse(Console.ReadLine(), out int year))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Некорректный год.");
        Console.ResetColor();
        return;
    }

    // Выбор автора
    var authors = await authorRepository.GetAllAsync();
    if (!authors.Any())
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[Внимание] В базе нет авторов. Сначала добавьте автора (пункт 5).");
        Console.ResetColor();
        return;
    }

    Console.WriteLine("Выберите автора из списка:");
    for (int i = 0; i < authors.Count; i++)
    {
        Console.WriteLine($"{i + 1}. {authors[i].Name} (ID: {authors[i].Id})");
    }
    Console.Write("Введите номер автора: ");
    if (!int.TryParse(Console.ReadLine(), out int authorIndex) || authorIndex < 1 || authorIndex > authors.Count)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Неверный выбор автора.");
        Console.ResetColor();
        return;
    }
    var selectedAuthor = authors[authorIndex - 1];

    // Выбор жанров (опционально)
    var genres = await genreRepository.GetAllAsync();
    var selectedGenres = new List<Genre>();
    if (genres.Any())
    {
        Console.WriteLine("Доступные жанры (введите ID жанров через запятую или оставьте пустым): ");
        foreach (var g in genres)
        {
            Console.WriteLine($"ID: {g.Id} | {g.Name}");
        }
        Console.Write("Ввод: ");
        var genresInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(genresInput))
        {
            var parts = genresInput.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int genreId))
                {
                    var genre = genres.FirstOrDefault(g => g.Id == genreId);
                    if (genre != null)
                    {
                        selectedGenres.Add(genre);
                    }
                }
            }
        }
    }

    var newBook = new Book
    {
        Title = title,
        PublicationYear = year,
        AuthorId = selectedAuthor.Id,
        Author = selectedAuthor,
        Genres = selectedGenres
    };

    await bookRepository.AddAsync(newBook);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[Успех] Книга \"{title}\" успешно добавлена с ID {newBook.Id}!");
    Console.ResetColor();
}

async Task AddNewAuthorAsync(AuthorRepository authorRepository)
{
    Console.WriteLine("\n--- ДОБАВЛЕНИЕ АВТОРА ---");
    Console.Write("Введите имя автора: ");
    var name = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(name))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Имя автора не может быть пустым.");
        Console.ResetColor();
        return;
    }

    var author = new Author { Name = name };
    await authorRepository.AddAsync(author);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[Успех] Автор \"{name}\" успешно добавлен с ID {author.Id}!");
    Console.ResetColor();
}

async Task AddNewGenreAsync(GenreRepository genreRepository)
{
    Console.WriteLine("\n--- ДОБАВЛЕНИЕ ЖАНРА ---");
    Console.Write("Введите название жанра: ");
    var name = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(name))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Название жанра не может быть пустым.");
        Console.ResetColor();
        return;
    }

    var genre = new Genre { Name = name };
    await genreRepository.AddAsync(genre);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[Успех] Жанр \"{name}\" успешно добавлен с ID {genre.Id}!");
    Console.ResetColor();
}

async Task DeleteBookAsync(BookRepository bookRepository)
{
    Console.WriteLine("\n--- УДАЛЕНИЕ КНИГИ ---");
    Console.Write("Введите ID книги для удаления: ");
    if (!int.TryParse(Console.ReadLine(), out int bookId))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] Некорректный ID.");
        Console.ResetColor();
        return;
    }

    var book = await bookRepository.GetByIdAsync(bookId);
    if (book == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Ошибка] Книга с ID {bookId} не найдена.");
        Console.ResetColor();
        return;
    }

    await bookRepository.DeleteAsync(bookId);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[Успех] Книга \"{book.Title}\" (ID: {bookId}) удалена!");
    Console.ResetColor();
}
