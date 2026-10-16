var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

public partial class Program;

public class Book(string title, string isbn)
{
    public Guid Id { get; set; }
    public string Title { get; set; } = title;
    public string Isbn { get; set; } = isbn;
    public bool IsCheckedOut { get; set; }
    
    public void CheckOut()
    {
        if (IsCheckedOut)
        {
            throw new BookUnavailableException();
        }
        IsCheckedOut = true;
    }

    public void Return()
    {
        if (!IsCheckedOut)
        {
            throw new InvalidOperationException();
        }
        IsCheckedOut = false;
    }

    public class BookUnavailableException : Exception;
    public class DuplicateIsbnException : Exception;
    public class BookNotFoundException : Exception;
    public class BookLimitExceededException : Exception;
}

public class Member(string name)
{
    public static int MaxConcurrentBooks = 3;
    
    public Guid Id { get; set; }
    public string Name { get; set; } = name;

    public int AvailableCheckoutCount => MaxConcurrentBooks - CheckedOutBooks.Count;
    public ICollection<Book> CheckedOutBooks { get; set; } = new List<Book>();
}