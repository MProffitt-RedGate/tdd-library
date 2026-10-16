using FluentAssertions;
using Library.Api.Services;
using NSubstitute;

namespace Library.Tests;

public class LibraryServiceTests
{
    private LibraryService BuildService(Member? member = null)
    {
        var userContext = Substitute.For<IUserContext>();

        if (member != null)
        {
            userContext.Member.Returns(member);
        }
        return new LibraryService(new InMemoryBookRepository(), userContext);
    }
    
    [Fact]
    public void AddBook_ShouldReturnBookId()
    {
        var book = new Book("Test Book", "1234");

        var service = BuildService();
        service.AddBook(book);
        
        book.Id.Should().NotBeEmpty();
    }
    
    [Fact]
    public void AddBookWithDuplicateIsbn_ShouldThrowDuplicateIsbnException()
    {
        var book = new Book("Test Book", "1234");
        var duplicateBook = new Book("Test Book 2", "1234");

        var service = BuildService();
        service.AddBook(book);
        
        Assert.Throws<Book.DuplicateIsbnException>(() => service.AddBook(duplicateBook));
    }
    
    [Fact]
    public void CheckoutOnUnknownId_ShouldThrowBookNotFoundException()
    {
        var bookId = Guid.NewGuid();
        var member = new Member("John Smith");
        
        var service = BuildService(member);
        
        Assert.Throws<Book.BookNotFoundException>(() => service.Checkout(bookId));
    }
    
    [Fact]
    public void CheckoutForMemberWith2OrLessBooks_ShouldAddToMembersBooks()
    {
        var newBook = new Book("New Book", "new-book");
        var member = new Member("John Smith")
        {
            CheckedOutBooks = new List<Book>()
            {
                new("Test Book 1", "1"),
                new("Test Book 2", "2")
            }
        };

        var service = BuildService(member);

        service.AddBook(newBook);
        service.Checkout(newBook.Id);
        
        member.AvailableCheckoutCount.Should().Be(0);
        member.CheckedOutBooks.Count.Should().Be(3);
    }
    
    [Fact]
    public void CheckoutForMemberWith3OrMoreBooks_ShouldThrowBookLimitExceededException()
    {
        var newBook = new Book("New Book", "new-book")
        {
            Id = Guid.NewGuid()
        };

        var member = new Member("John Smith")
        {
            CheckedOutBooks = new List<Book>()
            {
                new("Test Book 1", "1"),
                new("Test Book 2", "2"),
                new("Test Book 3", "3")
            }
        };
        
        var service = BuildService(member);

        member.AvailableCheckoutCount.Should().Be(0);
        Assert.Throws<Book.BookLimitExceededException>(() => service.Checkout(newBook.Id));
    }
    
    [Fact]
    public void ReturningABook_ShouldIncrementTheMembersAvailableBookCount()
    {
        var bookToReturn = new Book("New Book", "new-book")
        {
            Id = Guid.NewGuid()
        };

        var member = new Member("John Smith")
        {
            CheckedOutBooks = new List<Book>()
            {
                new("Test Book 1", "1"),
                new("Test Book 2", "2"),
                bookToReturn
            }
        };
        
        var service = BuildService(member);
        service.Return(bookToReturn.Id);
        
        member.AvailableCheckoutCount.Should().Be(1);
    }
    
    [Fact]
    public void ReturningABookTheUserDoesNotHave_ShouldThrowBookNotFoundException()
    {
        var otherBook = new Book("New Book", "new-book")
        {
            Id = Guid.NewGuid()
        };

        var member = new Member("John Smith")
        {
            CheckedOutBooks = new List<Book>()
            {
                new("Test Book 1", "1"),
                new("Test Book 2", "2")
            }
        };
        
        var service = BuildService(member);
        
        Assert.Throws<Book.BookNotFoundException>(() => service.Return(otherBook.Id));
    }
}