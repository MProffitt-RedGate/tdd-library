# TDD learning tasks

Thirteen tasks that each add to the library application, in an order that builds skills gradually. Every task is meant to be done **test-first** with NSubstitute and FluentAssertions.

Read [APPLICATION.md](APPLICATION.md) first for the current design and known gaps. Keep [CHEATSHEET.md](CHEATSHEET.md) open for syntax.

## How to work through a task

1. **Red.** Write one small failing test. Run it. Confirm it fails *for the reason you expect* (read the message, don't just look for red).
2. **Green.** Write the least code that passes. It's fine to be naive.
3. **Refactor.** Tidy production code *and* test code while everything is green.
4. Commit. Repeat with the next behaviour.

Ground rules:

- No production code without a failing test that demands it. (Pure refactors and the Task 1 test migration are the exceptions.)
- One behaviour per test, one reason to fail.
- Run the whole suite after every step.
- When a test is awkward to write, treat that as design feedback. Several tasks are built around exactly that.
- Each task lists **Learning goals**. Before moving on, check you can honestly tick them off.

## Progress

- [x] Migrate assertions to FluentAssertions
- [ ] Close the checkout gaps
- [x] Substitute the repository
- [ ] Make the repository seam mockable
- [ ] Search the catalogue
- [ ] Inject the loan policy
- [ ] Due dates and a clock
- [ ] Late fees (and when not to mock)
- [ ] Overdue notifications
- [ ] Async repository
- [ ] Reservations
- [ ] HTTP endpoints
- [ ] Custom FluentAssertions assertions

| Part | Tasks | Theme |
|---|---|---|
| A. Foundations | 1-4 | Core syntax, state vs interaction testing, designing for testability |
| B. Growing behaviour | 5-8 | Collections, policies, time, pure logic |
| C. Collaboration | 9-11 | Side effects, async, multi-step workflows |
| D. Outside-in | 12-13 | HTTP tests with substitutes, extending FluentAssertions |

---

## Part A: Foundations

### 1. Migrate assertions to FluentAssertions

**Why:** Get fluent with the exception and value assertion syntax before adding behaviour.

**Do:**

- Replace every `Assert.Throws<T>(...)` in `BookTests.cs` and `LibraryServiceTests.cs` with FluentAssertions (`Should().Throw<T>()`).
- Rename `AddBook_ShouldReturnBookId` so the name matches what it verifies.
- Make one failing assertion on purpose (for example expect the wrong exception type), and read how the xUnit message compares with the FluentAssertions one. Then undo it.

**TDD note:** This is a refactor, so you don't write new tests. The safety net is the existing suite staying green after each individual replacement.

**Learning goals**

- [x] Use `Action act = ...; act.Should().Throw<T>()` and `Invoking`
- [x] Explain `Throw<T>` vs `ThrowExactly<T>`
- [x] Add `.WithMessage(...)` to an assertion (you'll need messages in Task 2)

---

### 2. Close the checkout gaps

**Why:** Gaps 1 and 2 in APPLICATION.md are real bugs that no test catches. Writing the test that exposes a bug is the core TDD habit.

**Behaviours to add (one test each, in this order):**

1. After `Checkout`, the returned book has `IsCheckedOut == true`.
2. A book that is already checked out cannot be checked out again by *another* member: `Book.BookUnavailableException`.
3. The same member checking out the same book twice also throws `BookUnavailableException`, and their list still has one copy.
4. After `Return`, the book's `IsCheckedOut` is `false` and a different member can check it out.
5. A failed checkout (unknown book, or at the limit) doesn't change the member's list.
6. Give each exception a helpful message that includes the book id or title. Assert it with `WithMessage`.

**Hints:**

- For tests 2 and 4 you need two members. `LibraryService` takes a single `IUserContext`. Either build two services sharing one repository, or switch the substitute's `Member` between calls. Notice what is easier, and why.
- Start test 1 by writing only the assertion, then work backwards.

**Learning goals**

- [x] Write a test that fails because of a *missing* behaviour, not a compile error
- [x] Assert state with `Should().BeTrue()`, `HaveCount`, `Contain`, `NotContain`
- [x] Use an `AssertionScope` to check several properties of the result at once
- [x] Re-point a substituted property: `userContext.Member.Returns(otherMember)`

---

### 3. Substitute the repository

**Why:** So far the repository is real and nothing verifies interactions. Learn the difference between *state-based* and *interaction-based* tests, and when each fits.

**Do:** Convert the `AddBook` tests in `LibraryServiceTests` to use `Substitute.For<IBookRepository>()`.

**Behaviours:**

1. `AddBook` with a new ISBN calls `repository.Add` exactly once with that book (`Received(1)`).
2. `AddBook` with a duplicate ISBN does **not** call `Add` (`DidNotReceive`).
3. `AddBook` stores the book with a non-empty `Id`. Use `Arg.Do<Book>(...)` or `Arg.Is<Book>(b => b.Id != Guid.Empty)` to prove the id was assigned *before* it was stored.
4. `Checkout` for an unknown id throws `BookNotFoundException`. Stub `GetByIdOrDefault(id).Returns((Book?)null)` explicitly, then compare with leaving it unconfigured.

**Watch out:** `Exists(Predicate<Book>)` is awkward to stub. For behaviours 1-2 you'll probably write `repo.Exists(Arg.Any<Predicate<Book>>()).Returns(true)`. Notice that this test can't tell *which* ISBN was checked. Task 4 fixes that.

**Learning goals**

- [x] `Substitute.For`, `Returns`, `Received(n)`, `DidNotReceive`
- [x] `Arg.Any<T>()` vs `Arg.Is<T>(predicate)` vs `Arg.Do<T>(action)`
- [x] Say when a state-based test (real in-memory repo) beats an interaction test, and when it doesn't
- [x] Explain why you can't substitute `Book` or `Member`

---

### 4. Make the repository seam mockable

**Why:** Hard-to-test code is often hard-to-use code. Let the awkward test from Task 3 drive a better interface.

**Do (driven by tests, not by the refactor):**

1. Write a test that *demands* the ISBN being checked: with the substitute returning `true` only for `"ABC-1"`, adding a book with ISBN `"abc-1"` throws, and adding `"XYZ"` doesn't. It can't be written against `Exists(Predicate<Book>)`, so that is your red.
2. Introduce `bool IsbnExists(string isbn)` (or similar) on `IBookRepository` and make both tests pass.
3. Move the case-insensitive comparison to where it belongs. Decide whether the service or the repository owns it, and write the tests for that owner. Add a test on `InMemoryBookRepository` itself that proves `"abc-1"` and `"ABC-1"` collide.
4. Remove `Exists(Predicate<Book>)` and the exposed `Books` collection if nothing needs them. Add `IReadOnlyCollection<Book> GetAll()` only if a test needs it.

**Learning goals**

- [ ] Use a test's awkwardness as design feedback
- [ ] Test a real class (`InMemoryBookRepository`) directly with FluentAssertions, separately from the service tests that substitute it
- [ ] Explain why contract-style behaviour (case-insensitive ISBNs) belongs in one tested place

---

## Part B: Growing behaviour

### 5. Search the catalogue

**Why:** Collection assertions are where FluentAssertions shines. Search also needs the repository to return lists.

**Behaviours:**

1. `Search("clean")` returns books whose title contains the term, ignoring case.
2. The results are ordered by title.
3. A blank term returns an empty list (decide, and let the test document the decision).
4. Search results mark which books are currently available. Return a `BookSummary` DTO (`Title`, `Isbn`, `IsAvailable`) instead of `Book`.
5. Optional filter: `availableOnly`.

**Hints:**

- Decide whether the repository does the filtering (`Search(term)`) or the service does (`GetAll()` then LINQ). Write the test for your choice with a substitute that returns a fixed list.
- Compare these assertion styles on the same result and decide which gives the best failure message: `Count.Should().Be(2)`, `Should().HaveCount(2)`, `Should().BeEquivalentTo(...)`, `Should().SatisfyRespectively(...)`.

**Learning goals**

- [ ] `HaveCount`, `ContainSingle`, `OnlyContain`, `BeInAscendingOrder`, `BeEmpty`
- [ ] `BeEquivalentTo` with `Excluding(...)` (compare the DTO but ignore the generated `Id`)
- [ ] `SatisfyRespectively` for per-item checks
- [ ] Return a collection from a substitute: `repo.GetAll().Returns(new[] { a, b, c })`

---

### 6. Inject the loan policy

**Why:** `Member.MaxConcurrentBooks` is static, mutable, global state. Change it in one test and any parallel test can fail. Fixing it teaches isolation and substitutes for tiny interfaces.

**Do:**

1. **First, prove the problem.** Write two tests in different classes that set `Member.MaxConcurrentBooks` to different values. Run them repeatedly and see whether they interfere. Delete these once you've seen it.
2. Introduce `ILoanPolicy` with `int MaxConcurrentBooks`. Inject it into `LibraryService`.
3. Test: with a policy substitute returning `1`, a member with one book gets `BookLimitExceededException`. With `5`, a member with four books can still check out.
4. Remove the static field. Replace `Member.AvailableCheckoutCount` with a method that takes the limit, or move the calculation into the service. Update tests accordingly.
5. Add a real `DefaultLoanPolicy` (limit 3) with its own small test.

**Learning goals**

- [ ] Replace global state with an injected dependency
- [ ] Stub a property (`policy.MaxConcurrentBooks.Returns(1)`)
- [ ] Drive a constructor-signature change through all tests, and notice how a `BuildService` helper limits the damage
- [ ] Use `[Theory]` + `[InlineData]` for the boundary: 0, limit-1, limit

---

### 7. Due dates and a clock

**Why:** Anything using `DateTime.Now` is untestable. Learn to make time a dependency.

**Behaviours:**

1. Checking out a book records a **due date** 14 days from now (the loan period comes from `ILoanPolicy`).
2. `Member` (or a new `Loan` type) keeps `Book`, `CheckedOutAt` and `DueDate`.
3. `GetOverdueLoans()` returns the current member's loans where the due date has passed.
4. A loan due *exactly now* is **not** overdue. Pin down that boundary with a test.

**Hints:**

- Inject `TimeProvider` (built into .NET) and substitute it: `var time = Substitute.For<TimeProvider>(); time.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));` `GetUtcNow` is virtual, so NSubstitute can intercept it. You can use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) as an alternative. Try both and note the trade-offs.
- Use `Returns(a, b, c)` to simulate time moving forward across calls.
- If you introduce `Loan`, ask whether the due date belongs on `Loan` or `Book`.

**Learning goals**

- [ ] Make a non-deterministic input (time) a substitutable dependency
- [ ] `BeCloseTo`, `BeAfter`, `BeBefore`, `BeOnOrAfter` on `DateTimeOffset`
- [ ] Test an exact boundary
- [ ] Sequenced returns: `Returns(first, second)`

---

### 8. Late fees (and when not to mock)

**Why:** Not everything needs a substitute. A pure calculation should be tested with real objects and many examples. This task is about **restraint**.

**Behaviours** (`FeeCalculator`, no dependencies, no NSubstitute at all):

1. Returned on or before the due date: fee is `0.00`.
2. 0.25 per full day late.
3. Fees are capped at 10.00 per book.
4. Partial days don't count. 23 hours late is 0 days.
5. The daily rate and cap come from `ILoanPolicy` (here it's reasonable to substitute the policy, or to use a small real `LoanPolicy` record. Try both and pick).

Then wire it in: `Return` returns the fee. Write the service-level test with a **substitute `IFeeCalculator`** and check it was called with the right loan and return time, using `Received(1)` and `Arg.Is`. Compare it with a service test that uses the real calculator. Which would you rather maintain?

**Learning goals**

- [ ] Write a `[Theory]` with `[InlineData]` or `[MemberData]` covering boundaries and the cap
- [ ] Use `BeApproximately` / `Be` for decimals
- [ ] Articulate when *not* to substitute: pure logic and simple data
- [ ] Notice that a test with a real collaborator can be both simpler and stronger

---

## Part C: Collaboration

### 9. Overdue notifications

**Why:** Side effects are what interaction testing is *for*. A notification going out is an outcome you can't observe in state.

**Behaviours:**

1. `INotificationSender.Send(Notification)` where `Notification` has `MemberId`, `Subject`, `Body`.
2. `NotifyOverdueMembers()` sends one notification per overdue member (not one per book: group their books into one message).
3. A member with no overdue loans gets nothing (`DidNotReceive`).
4. The body lists the titles of the overdue books.
5. If sending to one member fails (substitute throws), the others are still notified, and the failure is reported (decide how; e.g. returns a result listing failures).

This needs a way to enumerate members, so you'll add an `IMemberRepository`. Let the test tell you the shape.

**Hints:**

- Check message contents without writing a giant `Arg.Is` lambda: capture with `Arg.Do<Notification>(n => sent.Add(n))` into a list, then assert on the list with FluentAssertions.
- For a throwing void method: `sender.When(s => s.Send(Arg.Is<Notification>(n => n.MemberId == bad))).Do(_ => throw new IOException());`
- Use `Received.InOrder` only if order genuinely matters. Does it here?

**Learning goals**

- [ ] `Arg.Do` capture, then `BeEquivalentTo(expected, o => o.Excluding(n => n.Body))`
- [ ] `When(...).Do(...)` to simulate failure
- [ ] `DidNotReceive` and `Received(n)` with matchers
- [ ] `Received.InOrder` (and know when it's over-specifying)
- [ ] `AssertionScope` for multi-field notification checks
- [ ] Spot an over-specified test: if refactoring without changing behaviour breaks it, it is too coupled

---

### 10. Async repository

**Why:** Real repositories do I/O. Async changes how you stub, verify and assert.

**Do:**

1. Convert `IBookRepository` and `LibraryService` to async: `Task<Book?> GetByIdAsync(Guid id, CancellationToken ct)`, `Task AddAsync(Book book, CancellationToken ct)`, and so on. Change the tests first, so they fail to compile, then the code.
2. New test: when the repository throws (`ThrowsAsync(new TimeoutException())`), `CheckoutAsync` lets it propagate and doesn't modify the member's list.
3. New test: the `CancellationToken` passed to the service reaches the repository. Verify with `Received(1).GetByIdAsync(id, token)` using a real `CancellationTokenSource`'s token, not `Arg.Any`.
4. New test: a cancelled token causes `OperationCanceledException`.

**Watch out:** Don't forget `await` in tests. An un-awaited `Should().ThrowAsync` silently passes.

**Learning goals**

- [ ] `repo.GetByIdAsync(...).Returns(book)` for `Task<T>` (and `ValueTask<T>`)
- [ ] `ThrowsAsync` from `NSubstitute.ExceptionExtensions`
- [ ] `await act.Should().ThrowAsync<T>()`, `NotThrowAsync`, `CompleteWithinAsync`
- [ ] `async Task` test methods, never `async void`
- [ ] Verify argument passing precisely instead of reaching for `Arg.Any`

---

### 11. Reservations

**Why:** A multi-step workflow. This task combines state assertions, interaction verification, ordering and several collaborators.

**Behaviours:**

1. A member can reserve a book that is currently checked out. Reserving an *available* book throws (they should just check it out).
2. Reservations queue first-in, first-out.
3. A member can't reserve the same book twice or reserve a book they have checked out.
4. When a book with reservations is returned, the **first** reserver is notified (`INotificationSender`) and the book is held for them.
5. While held, only that member can check it out. Others get `BookUnavailableException`.
6. A hold expires after 3 days. After that, the next reserver is notified. (Uses the clock from Task 7.)

**Hints:**

- Decide where the queue lives (`Book`? a `ReservationRepository`?) and justify it in a sentence in the test class's comments.
- Write the happy path end-to-end first with all substitutes, then add edge cases.
- For behaviour 4, verify both *what* was sent and *to whom*, and that the book was saved **before** the notification (`Received.InOrder`) if that matters to your design.

**Learning goals**

- [ ] Arrange a complex scenario with a builder or helper, and keep each test readable
- [ ] Mix state assertions (queue contents) and interaction assertions (notification) sensibly
- [ ] `SatisfyRespectively` / `ContainInOrder` on a queue
- [ ] Decide for yourself whether a particular check should be a state or an interaction check, and say why

---

## Part D: Outside-in

### 12. HTTP endpoints

**Why:** `Program.cs` has no endpoints yet, and `Microsoft.AspNetCore.Mvc.Testing` plus `public partial class Program;` are already in place. Testing at the HTTP boundary shows how substitutes drop into a real DI container.

**Do:**

1. Register services in DI and add minimal-API endpoints, test-first:
   - `POST /books` returns `201` with the new book (or `409` on duplicate ISBN)
   - `GET /books?search=...` returns `200` with the summaries from Task 5
   - `POST /books/{id}/checkout` returns `200`, `404` for unknown, `409` for unavailable, `422` for limit exceeded
   - `POST /books/{id}/return` returns `200` (and the fee) or `404`
2. Resolve the current member per request (for example from an `X-Member-Id` header) via `IUserContext`.
3. Map domain exceptions to status codes with `ProblemDetails`, using one place (middleware or `IExceptionHandler`), not a `try/catch` per endpoint.

**Test approach:**

- Use `WebApplicationFactory<Program>` and swap services in `ConfigureTestServices`: `services.AddSingleton(substituteService)`.
- **Two layers.** For routing and status-code tests, substitute `IBookRepository` (or the service) so each test controls the outcome. For one or two "walking skeleton" tests, use the real in-memory implementations end to end.
- Assert with `response.StatusCode.Should().Be(HttpStatusCode.Created)` and `await response.Content.ReadFromJsonAsync<BookDto>()` then `BeEquivalentTo`.

**Hints:**

- A test using the service substitute can't reuse `LibraryService` as-is, since its methods aren't virtual. Either substitute the repository, make a small `ILibraryService` interface, or mark the methods `virtual`. Pick one, and write down why.
- Remember each `WebApplicationFactory` test class gets one factory. Use `IClassFixture<>` and avoid shared mutable state between tests, or tests will interfere.

**Learning goals**

- [ ] Replace production services with substitutes inside a real host
- [ ] Choose the boundary at which to substitute, and justify it
- [ ] Assert HTTP status and JSON bodies with FluentAssertions
- [ ] Keep unit tests numerous and cheap, integration tests few and valuable

---

### 13. Custom FluentAssertions assertions

**Why:** Once you repeat the same assertions everywhere, make the vocabulary part of your tests. This is also the clearest way to learn how FluentAssertions works inside.

**Do (test-first, yes, even for assertions):**

1. Create `BookAssertions` so that `book.Should().BeAvailable()` and `book.Should().BeCheckedOut()` work.
2. Write tests that your assertion **passes** on the right state and **fails with a good message** on the wrong one. Use `Action act = () => book.Should().BeAvailable(); act.Should().Throw<XunitException>().WithMessage("*available*")`.
3. Add `.Should().BeCheckedOutBy(member)` for `Member`, and use `AndConstraint` so `book.Should().BeCheckedOut().And.HaveTitle("X")` chains.
4. Replace repeated assertions throughout the suite with your new ones. Delete what no longer pulls its weight.

**Hints:**

- Derive from `ReferenceTypeAssertions<Book, BookAssertions>` and override `Identifier`.
- Use `Execute.Assertion.BecauseOf(because, becauseArgs).ForCondition(...).FailWith("Expected {context:book} to be available{reason}, but it is checked out.")`.
- Return `new AndConstraint<BookAssertions>(this)`.

**Learning goals**

- [ ] Understand `Execute.Assertion`, `{context}` and `{reason}`
- [ ] Build chainable assertions with `AndConstraint`
- [ ] Test the failure message of a custom assertion
- [ ] Judge when a custom assertion improves readability and when it hides detail

---

## After the tasks

- Go back to Task 1's `Assert.Throws` style. Notice how differently you now read test failures.
- Run `dotnet test --collect:"XPlat Code Coverage"` (coverlet is already referenced) and look for **untested decisions**, not just uncovered lines.
- Pick three tests at random. For each, answer: *If I refactored the production code without changing behaviour, would this test break?* If yes, it's over-coupled; fix it.
- Reread your substitutes. Count how many verify *interactions* vs. how many stub *queries*. Interactions should be the minority.

## Suggested commit rhythm

One commit per red-green-refactor cycle, or at least per behaviour. Message the *behaviour*, e.g. `Checkout marks the book as checked out`, not `fixed stuff`.
