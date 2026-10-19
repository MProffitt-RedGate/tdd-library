using FluentAssertions;

namespace Library.Tests;

public class BookTests
{
    [Fact]
    public void NewBook_IsAvailable()
    {
        var book = new Book("Refactoring", "123-456789");
        book.IsCheckedOut.Should().BeFalse();
    }
    
    [Fact]
    public void CheckOut_MarksBookAsCheckedOut()
    {
        var book = new Book("Refactoring", "123-456789");
        book.CheckOut();
        book.IsCheckedOut.Should().BeTrue();
    }
    
    [Fact]
    public void CheckOutAlreadyCheckedOutBook_ThrowsBookUnavailableException()
    {
        var book = new Book("Refactoring", "123-456789");
        book.CheckOut();

        Action act = book.CheckOut;
        act.Should().ThrowExactly<Book.BookUnavailableException>();
    }
    
    [Fact]
    public void ReturningCheckedOutBook_MakesBookAvailable()
    {
        var book = new Book("Refactoring", "123-456789");
        book.CheckOut();
        book.Return();
        
        book.IsCheckedOut.Should().BeFalse();
    }
    
    [Fact]
    public void ReturningAvailableBook_ThrowsInvalidOperationException()
    {
        var book = new Book("Refactoring", "123-456789");

        Action act = book.Return;
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}