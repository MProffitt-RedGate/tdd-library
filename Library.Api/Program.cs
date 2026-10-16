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

public partial class Program
{
    
}

public class Book(string Title, string Reference)
{
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

    public class BookUnavailableException : Exception
    {
    }
}