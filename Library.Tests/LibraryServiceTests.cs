using FluentAssertions;
using FluentAssertions.Execution;
using Library.Api.Services;
using NSubstitute;

namespace Library.Tests;

public class LibraryServiceTests
{
    private LibraryService BuildService(Member? member = null)
    {
        var userContext = Substitute.For<IUserContext>();
        var bookRepository = Substitute.For<IBookRepository>();

        if (member != null)
        {
            userContext.Member.Returns(member);
        }
        return new LibraryService(bookRepository, userContext);
    }
    
    [Fact]
    public void AddBook_ShouldUpdateBookId()
    {
        var book = new Book("Test Book", "1234");

        var service = BuildService();
        service.AddBook(book);
        
        book.Id.Should().NotBeEmpty();
    }
    
    [Fact]
    public void AddBookWithDuplicateIsbn_ShouldThrowDuplicateIsbnException()
    {
        var duplicateBook = new Book("Test Book 2", "1234");

        var userContext = Substitute.For<IUserContext>();
        var bookRepository = Substitute.For<IBookRepository>();
        bookRepository.Exists(Arg.Any<Predicate<Book>>()).Returns(true);
        
        var service = new LibraryService(bookRepository, userContext);
        
        Action act = () => service.AddBook(duplicateBook);
        act.Should().ThrowExactly<Book.DuplicateIsbnException>();
        
        bookRepository.Received(1).Exists(Arg.Any<Predicate<Book>>());
        bookRepository.DidNotReceive().Add(Arg.Any<Book>());
        duplicateBook.Id.Should().BeEmpty();
    }
    
    [Fact]
    public void CheckoutOnUnknownId_ShouldThrowBookNotFoundException()
    {
        var bookId = Guid.NewGuid();
        var member = new Member("John Smith");
        
        var service = BuildService(member);
        
        Action act = () => service.Checkout(bookId);
        act.Should().ThrowExactly<Book.BookNotFoundException>();
    }
    
    [Fact]
    public void CheckoutForMemberWith2OrLessBooks_ShouldAddToMembersBooks()
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
                new("Test Book 2", "2")
            }
        };
        
        var userContext = Substitute.For<IUserContext>();
        userContext.Member.Returns(member);
        
        var bookRepository = Substitute.For<IBookRepository>();
        bookRepository.GetByIdOrDefault(newBook.Id).Returns(newBook);
        
        var service = new LibraryService(bookRepository, userContext);

        service.Checkout(newBook.Id);

        // Useful, because it shows all failures, not just the one in sequence
        using (new AssertionScope())
        {
            member.AvailableCheckoutCount.Should().Be(0);
            member.CheckedOutBooks.Count.Should().Be(3);
        }
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
        
        var userContext = Substitute.For<IUserContext>();
        userContext.Member.Returns(member);
        
        var bookRepository = Substitute.For<IBookRepository>();
        bookRepository.GetByIdOrDefault(newBook.Id).Returns(newBook);
        
        var service = new LibraryService(bookRepository, userContext);

        member.AvailableCheckoutCount.Should().Be(0);
        
        Action act = () => service.Checkout(newBook.Id);
        act.Should().ThrowExactly<Book.BookLimitExceededException>();
    }
    
    [Fact]
    public void CheckoutForMemberWithSameBook_ShouldThrowBookUnavailableException()
    {
        var bookToCheckout = new Book("New Book", "new-book")
        {
            Id = Guid.NewGuid()
        };

        var member = new Member("John Smith")
        {
            CheckedOutBooks = new List<Book>()
            {
                new("Test Book 1", "1"),
                bookToCheckout
            }
        };
        
        
        var userContext = Substitute.For<IUserContext>();
        userContext.Member.Returns(member);
        
        var bookRepository = Substitute.For<IBookRepository>();
        bookRepository.GetByIdOrDefault(bookToCheckout.Id).Returns(bookToCheckout);
        
        var service = new LibraryService(bookRepository, userContext);
        
        Action act = () => service.Checkout(bookToCheckout.Id);
        act.Should().ThrowExactly<Book.BookUnavailableException>();
        
        member.CheckedOutBooks.Should().Contain(bookToCheckout);
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
        
        Action act = () => service.Return(otherBook.Id);
        act.Should().ThrowExactly<Book.BookNotFoundException>();
    }
}