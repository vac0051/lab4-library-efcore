using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Data;

public sealed class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(author => author.Name)
                .HasMaxLength(150)
                .IsRequired();
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.Property(genre => genre.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(book => book.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(book => book.Author)
                .WithMany(author => author.Books)
                .HasForeignKey(book => book.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(book => book.Genres)
                .WithMany(genre => genre.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookGenres",
                    right => right
                        .HasOne<Genre>()
                        .WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Book>()
                        .WithMany()
                        .HasForeignKey("BookId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("BookId", "GenreId");
                        join.ToTable("BookGenres");
                    });
        });
    }
}
