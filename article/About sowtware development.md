# Making Software
## Problems we face
While developing software we face a lot of difficulties along the way: unclear requirements, miscommunication, poor development process and so on.
We also face some technical difficulties: legacy code slows us down, scaling is tricky, some bad decisions of the past kick us in the teeth today.
All of them can be if not eliminated then significantly reduced, but there's one fundamental problem you can do nothing about: the complexity of your system.
The idea of a system you are developing itself is always complex, whether you understand it or not.
Even when you're making _yet another CRUD application_, there're always some edge cases, some tricky things, and from time to time someone asks "Hey, what's gonna happen if I do this and this under these circumstances?" and you say "Hm, that's a very good question.".
Those tricky cases, shady logic, validation and access managing -- all that adds up to your big idea.
Quite often that idea is so big that it doesn't fit in one head, and that fact alone brings problems like miscommunication.
But let's be generous and assume that this team of domain experts and business analysts communicates clearly and produces fine consistent requirements.
Now we have to implement them, to express that complex idea in our code. Now that code is another system, way more complicated than original idea we had in mind(s).
How so? It faces reality: technical limitations force you to deal with highload, data consistency and availability on top of implementing actual business logic.
As you can see the task is pretty challenging, and now we need proper tools to deal with it.
A programming language is just another tool, and like with every other tool, it's not just about the quality of it, it's probably even more about the tool fitting the job. You might have the best screwdriver there is, but if you need to put some nails into wood, a crappy hammer would be better, right?

## Technical aspects

Most popular languages today a object oriented. When someone makes an introduction to OOP they usually use examples:
Consider a car, which is an object from the real world. It has various properties like brand, weight, color, max speed, current speed and so on.
To reflect this object in our program we gather those properties in one class. Properties can be permanent or mutable, which together form both current state of this object and some boundaries in which it may vary. However combining those properties isn't enough, since we have to check that current state makes sense, e.g. current speed doesn't exceed max speed. To make sure of that we attach some logic to this class, mark properties as private to prevent anyone from creating illegal state.
As you can see objects are about their internal state and life cycle.
So those three pillars of OOP make perfect sense in this context: we use inheritance to reuse certain state manipulations, encapsulation for state protection and polymorphism for treating similar objects the same way. Mutability as a default also makes sense, since in this context immutable object can't have a life cycle and has always one state, which isn't the most common case.

Thing is when you look at a typical web application of these days, it doesn't deal with objects. Almost everything in our code has either eternal lifetime or no proper lifetime at all. Two most common kinds of "objects" are some sort of services like `UserService`, `EmployeeRepository` or some models/entities/DTOs or whatever you call them. Services have no logical state inside them, they die and born again exactly the same, we just recreate the dependency graph with a new database connection.
Entities and models don't have any behavior attached to them, they are merely bundles of data, their mutability doesn't help but quite the opposite.
Therefore key features of OOP aren't really useful for developing this kind of applications.

What happens in a typical web app is data flowing: validation, transformation, evaluation and so on. And there's a paradigm that fits perfectly for that kind of job: functional programming. And there's a proof for that: all the modern features in popular languages today come from there: `async/await`, lambdas and delegates, reactive programming, discriminated unions (enums in swift or rust, not to be confused with enums in java or .net), tuples - all that is from FP.
However those are just crumbles, it's very nice to have them, but there's more, way more.

Before I go any deeper, there's a point to be made. Switching to a new language, especially a new paradigm, is an investment for developers and therefore for business. Doing foolish investments won't give you anything but troubles, but reasonable investments may be the very thing that'll keep you afloat.

## Tools we have and what they give us

A lot of us prefer languages with static typing. The reason for that is simple: compiler takes care of tedious checks like passing proper parameters to functions, constructing our entities correctly and so on. These checks come for free. Now, as for the stuff that compiler can't check, we have a choice: hope for the best or make some tests. Writing tests means money, and you don't pay just once per test, you have to maintain them. Besides, people get sloppy, so every once in a while we get false positive and false negative results. The more tests you have to write the lower is the average quality of those tests. There's another problem: in order to test something, you have to know and remember that that thing should be tested, but the bigger your system is the easier it is to miss something.

However compiler is only as good as the type system of the language. If it doesn't allow you to express something in static ways, you have to do that in runtime. Which means tests, yes. It's not only about type system though, syntax and small sugar features are very important too, because at the end of the day we want to write as little code as possible, so if some approach requires you to write ten times more lines, well, no one is gonna use it. That's why it's important that language you choose has the fitting set of features and tricks - well, right focus overall. If it doesn't - instead of using its features to fight original challenges like complexity of your system and changing requirements, you gonna be fighting the language as well. And it all comes down to money, since you pay developers for their time. The more problem they have to solve, the more time they gonna need and the more developers you are gonna need.

Finally we are about to see some code to prove all that. I'm happen to be a .NET developer, so code samples are gonna be in C# and F#, but the general picture would look more or less the same in other popular OOP and FP languages.

## Let the coding begin

We are gonna build a web application for managing credit cards.
Basic requirements:
- Create/Read users
- Create/Read credit cards
- Activate/Deactivate credit cards
- Set daily limit for cards
- Top up balance
- Process payments (considering balance, card expiration date, active/deactivated state and daily limit)

For the sake of simplicity we are gonna use one card per account and we will skip authorization. But for the rest we're gonna build capable application with validation, error handling, database and web api. So let's get down to our first task: design credit cards.
First, let's see what it would look like in C#
```csharp
public class Card
{
    public string CardNumber {get;set;}
    public string Name {get;set;}
    public int ExpirationMonth {get;set;}
    public int ExpirationYear {get;set;}
    public bool IsActive {get;set;}
    public AccountInfo AccountInfo {get;set;}
}

public class AccountInfo
{
    public decimal Balance {get;set;}
    public string CardNumber {get;set;}
    public decimal DailyLimit {get;set;}
}
```

But that's not enough, we have to add validation, and commonly it's being done in some `Validator`, like the one from `FluentValidation`.
 The rules are simple:
- Card number is required and must be a 16-digit string.
- Name is required and must contain only letters and can contain spaces in the middle.
- Month and year have to satisfy boundaries.
- Account info must be present when the card is active and absent when the card is deactivated. If you are wondering why, it's simple: when card is deactivated, it shouldn't be possible to change balance or daily limit.

```csharp
public class CardValidator : IValidator
{
    internal static CardNumberRegex = new Regex("^[0-9]{16}$");
    internal static NameRegex = new Regex("^[\w]+[\w ]+[\w]+$");

    public CardValidator()
    {
        RuleFor(x => x.CardNumber)
            .Must(c => !string.IsNullOrEmpty(c) && CardNumberRegex.IsMatch(c))
            .WithMessage("oh my");

        RuleFor(x => x.Name)
            .Must(c => !string.IsNullOrEmpty(c) && NameRegex.IsMatch(c))
            .WithMessage("oh no");

        RuleFor(x => x.ExpirationMonth)
            .Must(x => x >= 1 && x <= 12)
            .WithMessage("oh boy");
            
        RuleFor(x => x.ExpirationYear)
            .Must(x => x >= 2019 && x <= 2023)
            .WithMessage("oh boy");
            
        RuleFor(x => x.AccountInfo)
            .Null()
            .When(x => !x.IsActive)
            .WithMessage("oh boy");

        RuleFor(x => x.AccountInfo)
            .NotNull()
            .When(x => x.IsActive)
            .WithMessage("oh boy");
    }
}
```

Now there're several problems with this approach:
- Validation is separated from type declaration, which means to see the full picture of _what card really is_ we have to navigate through code and recreate this image in our head. It's not a big problem when it happens only once, but when we have to do that for every single entity in a big project, well, it's very time consuming.
- This validation isn't forced, we have to keep in mind to use it everywhere. We can ensure this with tests, but then again, you have to remember about it when you write tests.
- When we want to validate card number in other places, we have to do same thing all over again. Sure, we can keep regex in a common place, but still we have to call it in every validator.

In F# we can do it in a different way:
```fsharp
// First we define a type for CardNumber with private constructor
// and public factory which receives string and returns `Result<CardNumber, string>`
type CardNumber = private CardNumber of string
    with
    member this.Value = match this with CardNumber s -> s
    static member create str =
        match str with
        | (null|"") -> Error "card number can't be empty"
        | str ->
            if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
            else Error "Card number must be a 16 digits string"

// Then in here we express this logic "when card is deactivated, balance and daily limit manipulations aren't available`.
// Note that this is way easier to grasp that reading `RuleFor()` in validators.
type CardAccountInfo =
    | Active of AccountInfo
    | Deactivated

// And then that's it. The whole set of rules is here, and it's described in a static way.
// We don't need tests for that, the compiler is our test. And we can't accidentally miss this validation.
type Card =
    { CardNumber: CardNumber
      Name: LetterString
      HolderId: UserId
      Expiration: (Month * Year)
      AccountDetails: CardAccountInfo }
```

Of course some things from here we can do in C#. We can create `CardNumber` class which will throw `ValidationException` in there too. But that trick with `CardAccountInfo` can't be done in C# in easy way.
Another thing - C# heavily relies on exceptions. There are several problems with that:
- Exceptions have "go to" semantics. One moment you're here in this method, another - you ended up in some global handler.
- They don't appear in method signature. Exceptions like `ValidationException` or `InvalidUserOperationException` are part of the contract, but you don't know that until you read _implementation_. And it's a major problem, because quite often you have to use code written by someone else, and instead of reading just signature, you have to navigate all the way to the bottom of the call stack, which takes a lot of time.

And this is what bothers me: whenever I implement some new feature, implementation process itself doesn't take much time, the majority of it goes to two things:
- Reading other people's code and figuring out business logic rules.
- Making sure nothing is broken.

It may sound like a symptom of a bad code design, but same thing what happens even on decently written projects.
Okay, but we can try use same `Result` thing in C#. The most obvious implementation would look like this:

```csharp
public class Result<TOk, TError>
{
    public TOk Ok {get;set;}
    public TError Error {get;set;}
}
```
and it's a pure garbage, it doesn't prevent us from setting both `Ok` and `Error` and allows error to be completely ignored. The proper version would be something like this:
```csharp
public abstract class Result<TOk, TError>
{
    public abstract bool IsOk { get; }

    private sealed class OkResult : Result<TOk, TError>
    {
        public readonly TOk _ok;
        public OkResult(TOk ok) { _ok = ok; }

        public override bool IsOk => true;
    }
    private sealed class ErrorResult : Result<TOk, TError>
    {
        public readonly TError _error;
        public ErrorResult(TError error) { _error = error; }

        public override bool IsOk => false;
    }

    public static Result<TOk, TError> Ok(TOk ok) => new OkResult(ok);
    public static Result<TOk, TError> Error(TError error) => new ErrorResult(error);

    public Result<T, TError> Map<T>(Func<TOk, T> map)
    {
        if (this.IsOk)
        {
            var value = ((OkResult)this)._ok;
            return Result<T, TError>.Ok(map(value));
        }
        else
        {
            var value = ((ErrorResult)this)._error;
            return Result<T, TError>.Error(value);
        }
    }

    public Result<TOk, T> MapError<T>(Func<TError, T> mapError)
    {
        if (this.IsOk)
        {
            var value = ((OkResult)this)._ok;
            return Result<TOk, T>.Ok(value);
        }
        else
        {
            var value = ((ErrorResult)this)._error;
            return Result<TOk, T>.Error(mapError(value));
        }
    }
}
```
Pretty cumbersome, right? And I didn't even implement the `void` versions for `Map` and `MapError`. The usage would look like this:
```csharp
void Test(Result<int, string> result)
{
    var squareResult = result.Map(x => x * x);
}
```
Not so bad, uh? Well, now imagine you have three results and you want to do something with them when all of them are `Ok`. Nasty. So that's hardly an option.
F# version:
```fsharp
// this type is in standard library, but declaration looks like this:
type Result<'ok, 'error> =
    | Ok of 'ok
    | Error of 'error
// and usage:
let test res1 res2 res3 =
    match res1, res2, res3 with
    | Ok ok1, Ok ok2, Ok ok3 -> printfn "1: %A 2: %A 3: %A" ok1 ok2 ok3
    | _ -> printfn "fail"
```
 Basically, you have to choose whether you write reasonable amount of code, but the code is obscure, relies on exceptions, reflection, expressions and other "magic", or you write much more code, which is hard to read, but it's more durable and straight forward. When such a project gets big you just can't fight it, not in languages with C#-like type systems. Let's consider a simple scenario: you have some entity in your codebase for a while. Today you want to add a new required field. Naturally you need to initialize this field everywhere this entity is created, but compiler doesn't help you at all, since class is mutable and `null` is a valid value. And libraries like `AutoMapper` make it even harder. This mutability allows us to partially initialize objects in one place, then push it somewhere else and continue initialization there. That's another source of bugs.
 
Language feature comparison is nice, however it's not what this article about. If you're interested in it, I covered that topic in my [previous article](https://medium.com/@liman.rom/f-spoiled-me-or-why-i-dont-enjoy-c-anymore-39e025035a98). And language features themselves shouldn't be a reason to switch technology.

So that brings us to these questions:
1. Why do we really need to switch from modern OOP?
2. Why should we to switch to FP?

Answer to first question is using common OOP languages for modern applications gives you a lot of troubles, because they were designed for a different purposes. It results in time and money you spend to fight their design along with fighting complexity of your application.
And second answer is FP languages give you an opportunity to design your code in a way that you implement feature once and you forget about it. It works like a clock, and if a new feature breaks the logic of it, it breaks the code, hence you know that immediately.

***
However those answers aren't enough. As my friend pointed out during one of our discussions, switching to FP would be useless when you don't know best practices. Our big industry produced tons of articles, books and tutorials about designing OOP applications, and we have production experience with OOP, so we know what to expect from different approaches. Unfortunately, it's not the case for functional programming, so even if you switch to FP, your first attempts most likely would be awkward and certainly wouldn't bring you the desired result: fast and painless developing of complex systems.

Well, that's precisely what this article is about. As I said, we're gonna build production-like application to see the difference.

## How do we design application?

A lot of this ideas I used in design process I borrowed from the great book [Domain Modeling Made Functional](https://www.amazon.com/Domain-Modeling-Made-Functional-Domain-Driven/dp/1680502549), so I strongly encourage you to read it.