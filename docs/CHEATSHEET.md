# NSubstitute and FluentAssertions cheat sheet

Examples use this repo's types. Versions: NSubstitute 6.2.0, FluentAssertions 8.11.0.

## NSubstitute

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;   // Throws / ThrowsAsync
```

### Create

```csharp
var repo = Substitute.For<IBookRepository>();
```

Only interfaces and classes with `virtual`/`abstract` members can be substituted. `Book` and `Member` can't (non-virtual), so use real instances.

### Stub return values

```csharp
repo.GetByIdOrDefault(id).Returns(book);                      // specific argument
repo.GetByIdOrDefault(Arg.Any<Guid>()).Returns(book);         // any argument
repo.GetByIdOrDefault(id).Returns(book, null);                // first call book, then null
repo.GetByIdOrDefault(Arg.Any<Guid>())
    .Returns(call => new Book("t", call.Arg<Guid>().ToString()));   // computed
repo.GetByIdOrDefault(id).Returns(_ => throw new InvalidOperationException());
repo.GetByIdOrDefault(id).Throws(new InvalidOperationException());  // needs ExceptionExtensions
```

Unconfigured members return defaults: `0`, `false`, `null`, an empty string, or an empty array. For interfaces, delegates and purely virtual classes they return a recursive substitute instead.

### Async

```csharp
repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(book);   // Task<Book?> wrapped for you
repo.GetByIdAsync(id, default).ThrowsAsync(new TimeoutException());  // needs ExceptionExtensions
```

### Verify calls

```csharp
repo.Received(1).Add(book);                  // exactly once
repo.Received().Add(Arg.Any<Book>());        // at least once
repo.DidNotReceive().Add(Arg.Any<Book>());   // never
repo.DidNotReceiveWithAnyArgs().Add(default!);
```

### Argument matchers

```csharp
Arg.Any<Book>()
Arg.Is<Book>(b => b.Isbn == "123")            // match by predicate
Arg.Is(book)                                  // equality
Arg.Do<Book>(b => captured = b)               // capture an argument
Arg.Invoke(...)                               // invoke a callback argument
```

Rule: in one call, **either all arguments are matchers or none are**. `Add(Arg.Any<Book>(), 5)` mixes styles and will throw. Use `Arg.Is(5)` instead of `5`.

### Void methods

```csharp
repo.When(r => r.Add(Arg.Any<Book>()))
    .Do(_ => throw new IOException());        // make a void method throw
repo.When(r => r.Add(Arg.Any<Book>()))
    .Do(call => captured = call.Arg<Book>()); // run a side effect
```

### Ordering and inspection

```csharp
Received.InOrder(() =>
{
    repo.Add(book);
    notifier.Send(Arg.Any<Notification>());
});

repo.ReceivedCalls();                         // everything that was called
repo.ClearReceivedCalls();
```

### Properties and events

```csharp
userContext.Member.Returns(member);           // property getter
userContext.Member = member;                  // setters are remembered on a substitute
sender.Sent += Raise.Event<EventHandler>(this, EventArgs.Empty);
```

### Common NSubstitute pitfalls

- Calling a substitute inside a `Returns(...)` argument (for example `a.Foo().Returns(b.Bar())`) confuses the "last call" tracking. Evaluate the inner value first, into a variable.
- Non-virtual member on a substituted class? The real code runs rather than being intercepted, and `Returns` fails with a confusing error such as `CouldNotSetReturnDueToNoLastCallException`.
- Verifying too much couples tests to implementation. Verify *outgoing* side effects that matter (a notification was sent, a book was saved). Don't verify every query.
- Don't substitute simple data (`Book`, `Member`, DTOs) or pure logic (a fee calculator). Use the real thing.

## FluentAssertions

```csharp
using FluentAssertions;
using FluentAssertions.Execution;   // AssertionScope
```

### Values

```csharp
book.IsCheckedOut.Should().BeTrue();
member.AvailableCheckoutCount.Should().Be(1);
book.Title.Should().StartWith("Clean").And.EndWith("Code");
book.Id.Should().NotBeEmpty();                  // Guid
fee.Should().BeApproximately(1.50m, 0.001m);
due.Should().BeCloseTo(now.AddDays(14), TimeSpan.FromSeconds(1));
due.Should().BeAfter(now);
```

### Exceptions

```csharp
Action act = () => service.AddBook(duplicate);
act.Should().Throw<Book.DuplicateIsbnException>();            // subclasses allowed
act.Should().ThrowExactly<Book.DuplicateIsbnException>();     // exact type
act.Should().Throw<ArgumentException>()
   .WithMessage("*isbn*")                                     // * is a wildcard, case-insensitive
   .WithParameterName("book");
act.Should().NotThrow();

service.Invoking(s => s.Return(id)).Should().Throw<Book.BookNotFoundException>();

Func<Task> actAsync = () => service.AddBookAsync(book);
await actAsync.Should().ThrowAsync<Book.DuplicateIsbnException>();
await actAsync.Should().NotThrowAsync();
await actAsync.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
```

### Collections

```csharp
books.Should().HaveCount(3);
books.Should().ContainSingle(b => b.Isbn == "123");
books.Should().Contain(b => b.Title.Contains("Test"));
books.Should().OnlyContain(b => !b.IsCheckedOut);
books.Should().BeEmpty();
books.Should().BeInAscendingOrder(b => b.Title);
books.Should().Equal(first, second);                        // same items, same order
books.Should().BeEquivalentTo(new[] { first, second });     // same items, any order, compared by value
books.Should().SatisfyRespectively(
    b => b.Title.Should().Be("A"),
    b => b.Title.Should().Be("B"));
books.Select(b => b.Title).Should().ContainInOrder("A", "B");
```

### Object comparison

```csharp
actual.Should().BeEquivalentTo(expected);                              // structural, not reference
actual.Should().BeEquivalentTo(expected, o => o.Excluding(b => b.Id));
actual.Should().BeEquivalentTo(new { Title = "T", Isbn = "1" });       // anonymous subset
actual.Should().BeEquivalentTo(expected, o => o.ExcludingMissingMembers());
book.Should().BeSameAs(sameInstance);                                  // reference equality
```

### Multiple assertions in one go

```csharp
using (new AssertionScope())
{
    result.Title.Should().Be("T");
    result.IsCheckedOut.Should().BeTrue();
    member.CheckedOutBooks.Should().HaveCount(1);
}   // all failures reported together
```

### Better failure messages

```csharp
book.IsCheckedOut.Should().BeTrue("the service should mark {0} as checked out", book.Title);
```

### Custom assertions

Derive from `ReferenceTypeAssertions<TSubject, TAssertions>`, add an extension method returning your assertions class, and use `Execute.Assertion.ForCondition(...).FailWith(...)`. Task 13 walks you through it.

## xUnit pieces you'll need

```csharp
[Theory]
[InlineData(0, 0.00)]
[InlineData(3, 0.75)]
public void Fee_ForDaysLate(int days, decimal expected) { ... }

public class MyTests : IAsyncLifetime { ... }       // async setup and teardown
```

Prefer a `BuildService(...)` helper or a small builder class over constructor/field setup: it keeps each test's arrangement visible.
