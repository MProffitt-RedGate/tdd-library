namespace Library.Api.Services;

public interface IBookRepository
{
    ICollection<Book> Books { get; }
    
    void Add(Book book);
    bool Exists(Predicate<Book> predicate);
    Book? GetByIdOrDefault(Guid id);
}

public class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = new();
    public ICollection<Book> Books =>  _books;

    public void Add(Book book)
    {
        _books.Add(book);
    }

    public bool Exists(Predicate<Book> predicate)
    {
        return _books.Any(b => predicate(b));
    }

    public Book? GetByIdOrDefault(Guid id)
    {
        return _books.FirstOrDefault(b => b.Id == id);
    }
}