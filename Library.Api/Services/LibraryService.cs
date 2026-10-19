namespace Library.Api.Services;

public class LibraryService(IBookRepository bookRepository, IUserContext userContext)
{
    public void AddBook(Book book)
    {
        var isbnInUse = bookRepository.Exists(b => 
            b.Isbn.ToUpperInvariant() == book.Isbn.ToUpperInvariant());
        if (isbnInUse)
        {
            throw new Book.DuplicateIsbnException();
        }
        
        book.Id = Guid.NewGuid();
        bookRepository.Add(book);
    }

    public Book Checkout(Guid bookId)
    {
        if (userContext.Member.AvailableCheckoutCount <= 0)
        {
            throw new Book.BookLimitExceededException();
        }
        
        var book = bookRepository.GetByIdOrDefault(bookId);
        if (book is null)
        {
            throw new Book.BookNotFoundException();
        }
        
        var alreadyHasBook = userContext.Member.CheckedOutBooks.Any(b => b.Id == bookId);
        if (alreadyHasBook)
        {
            throw new Book.BookUnavailableException();
        }
        
        userContext.Member.CheckedOutBooks.Add(book);
        return book;
    }

    public void Return(Guid bookId)
    {
        var book = userContext.Member.CheckedOutBooks.FirstOrDefault(b => b.Id == bookId);
        if (book is null)
        {
            throw new Book.BookNotFoundException();
        }
        userContext.Member.CheckedOutBooks.Remove(book);
    }
}