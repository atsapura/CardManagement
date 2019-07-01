# Fighting complexity in software development
## Problems we face
While developing software we face a lot of difficulties along the way: unclear requirements, miscommunication, poor development process and so on.
We also face some technical difficulties: legacy code slows us down, scaling is tricky, some bad decisions of the past kick us in the teeth today.
All of them can be if not eliminated then significantly reduced, [many of them и в конструкции if not-then на английском я не уверен]  but there's one fundamental problem you can do nothing about: the complexity of your system.
The idea of a system you are developing itself is always complex, whether you understand it or not.
Even when you're making _yet another CRUD application_, there're always some edge cases, some tricky things, and from time to time someone asks "Hey, what's gonna happen if I do this and this under these circumstances?" and you say "Hm, that's a very good question.".
Those tricky cases, shady logic, validation and access managing [https://en.wikipedia.org/wiki/Ninety-ninety_rule]  - all that adds up to your big idea.
Quite often that idea is so big that it doesn't fit in one head, and that fact alone brings problems like miscommunication.
But let's be generous and assume that this team of domain experts and business analysts communicates clearly and produces fine consistent requirements.
Now we have to implement them, to express that complex idea in our code. Now that code is another system, way more complicated than original idea we had in mind(s).
 Why so? It faces reality: technical limitations force you to deal with highload, data consistency and availability on top of implementing actual business logic.
As you can see the task is pretty challenging, and now we need proper tools to deal with it.
A programming language is just another tool, and like with every other tool, it's not just about the quality of it, it's probably even more about the tool fitting the job. You might have the best screwdriver there, but if you need to put some nails into wood, a crappy hammer would be better, right?

## Technical aspects

Most popular languages today a object oriented. When someone makes an introduction to OOP they usually use examples:
Consider a car, which is an object from the real world. It has various properties like brand, weight, color, max speed, current speed and so on.
To reflect this object in our program we gather those properties in one class. Properties can be permanent or mutable, which together form both current state of this object and some boundaries in which it may vary. However combining those properties isn't enough, since we have to check that current state makes sense, e.g. current speed doesn't exceed max speed. To make sure of that we attach some logic to this class, mark properties as private to prevent anyone from creating illegal state.
As you can see objects are about their internal state and life cycle.
So those three pillars of OOP make perfect sense in this context: we use inheritance to reuse certain state manipulations, encapsulation for state protection and polymorphism for treating similar objects the same way. Mutability as a default also makes sense, since in this context immutable object can't have a life cycle and has always one state, which isn't the most common case.

Thing is when you look at a typical web application of these days, it doesn't deal with objects. Almost everything in our code has either eternal lifetime or no proper lifetime at all. Two most common kinds of "objects" are some sort of services like `UserService`, `EmployeeRepository` or some models/entities/DTOs or whatever you call them. [at this point I started to wonder, why not to use stateful/ stateless terms. Maybe you need 'this article is aimed at' disclaimer. ] Services have no logical state inside them, they die and born again exactly the same, we just recreate the dependency graph with a new database connection.
Entities and models don't have any behavior attached to them, they are merely bundles of data, their mutability doesn't help but quite the opposite.
Therefore key features of OOP aren't really useful for developing this kind of applications. [There could be expectation, that you've read classic Kingdom of noun](https://steve-yegge.blogspot.com/2006/03/execution-in-kingdom-of-nouns.html)

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
// and public factory which receives string and returns `Result<CardNumber, string>`.
// Normally we would use `ValidationError` instead, but string is good enough for example
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
      Name: LetterString // LetterString is another type with built-in validation
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
 
Meanwhile language feature comparison is nice, however it's not what this article about. If you're interested in it, I covered that topic in my [previous article](https://medium.com/@liman.rom/f-spoiled-me-or-why-i-dont-enjoy-c-anymore-39e025035a98). But language features themselves shouldn't be a reason to switch technology.

So that brings us to these questions:
1. Why do we really need to switch from modern OOP?
2. Why should we switch to FP?

Answer to first question is using common OOP languages for modern applications gives you a lot of troubles, because they were designed for a different purposes. It results in time and money you spend to fight their design along with fighting complexity of your application.

And the second answer is FP languages give you an easy way to design your features so they work like a clock, and if a new feature breaks existing logic, it breaks the code, hence you know that immediately.

***
However those answers aren't enough. As my friend pointed out during one of our discussions, switching to FP would be useless when you don't know best practices. Our big industry produced tons of articles, books and tutorials about designing OOP applications, and we have production experience with OOP, so we know what to expect from different approaches. Unfortunately, it's not the case for functional programming, so even if you switch to FP, your first attempts most likely would be awkward and certainly wouldn't bring you the desired result: fast and painless developing of complex systems.

Well, that's precisely what this article is about. As I said, we're gonna build production-like application to see the difference.

## How do we design application?

A lot of this ideas I used in design process I borrowed from the great book [Domain Modeling Made Functional](https://www.amazon.com/Domain-Modeling-Made-Functional-Domain-Driven/dp/1680502549), so I strongly encourage you to read it.

Full source code with comments is [here](https://github.com/atsapura/CardManagement). Naturally, I'm not going to put all of it in here, so I'll just walk through key points.

We'll have 4 main projects: business layer, data access layer, infrastructure and, of course, common. Every solution has it, right?
We begin with modeling our domain. At this point we don't know and don't care about database. It's done on purpose, because having specific database in mind we tend to design our domain according to it, we bring this entity-table relation in business layer, which later brings problems. You only need implement mapping `domain -> DAL` once, while wrong design will trouble us constantly until the point we fix it. So here's what we do: we create a project named `CardManagement` (very creative, I know), and immediately turn on the setting `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in project file. Why do we need this? Well, we're gonna use discriminated unions heavily, and when you do pattern matching, compiler gives us a warning, if we didn't cover all the possible cases:
```fsharp
let fail result =
    match result with
    | Ok v -> printfn "%A" v
    // warning: Incomplete pattern matches on this expression. For example, the value 'Error' may indicate a case not covered by the pattern(s).
```
With this setting on, this code just won't compile, which is exactly what we need, when we extend existing functionality and want it to be adjusted everywhere. Next thing we do is creating module (it compiles in a static class) `CardDomain`. In this file we describe domain types and nothing more. Keep in mind that in F#, code and file order matters: by default you can use only what you declared earlier.

### Domain types
We begin defining our types with `CardNumber` I showed before, although we're gonna need more practical `Error` than just a string, so we'll use `ValidationError`.
```fsharp
type ValidationError =
    { FieldPath: string
      Message: string }

let validationError field message = { FieldPath = field; Message = message }

// Actually we should use here Luhn's algorithm, but I leave it to you as an exercise,
// so you can see for yourself how easy is updating code to new requirements.
let private cardNumberRegex = new Regex("^[0-9]{16}$", RegexOptions.Compiled)

type CardNumber = private CardNumber of string
    with
    member this.Value = match this with CardNumber s -> s
    static member create fieldName str =
        match str with
        | (null|"") -> validationError fieldName "card number can't be empty"
        | str ->
            if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
            else validationError fieldName "Card number must be a 16 digits string"
```
Then we of course define `Card` which is the heart of our domain. We know that card has some permanent attributes like number, expiration date and name on card, and some changeable information like balance and daily limit, so we encapsulate that changeable info in other type:
```fsharp
type AccountInfo =
    { HolderId: UserId
      Balance: Money
      DailyLimit: DailyLimit }

type Card =
    { CardNumber: CardNumber
      Name: LetterString
      HolderId: UserId
      Expiration: (Month * Year)
      AccountDetails: CardAccountInfo }
```
Now, there're several types here, which we haven't declared yet:
1. **Money**

    We could use `decimal` (and we will, but no directly), but `decimal` is less descriptive. Besides, it can be used for representation of other things than money, and we don't want it to be mixed up. So we use custom type `type [<Struct>] Money = Money of decimal `.
2. **DailyLimit**

   Daily limit can be either set to a specific amount or to be absent at all. If it's present, it must be positive. Instead of using `decimal` or `Money` we define this type:
   ```fsharp
    [<Struct>]
    type DailyLimit =
        private // private constructor so it can't be created directly outside of module
        | Limit of Money
        | Unlimited
        with
        static member ofDecimal dec =
            if dec > 0m then Money dec |> Limit
            else Unlimited
        member this.ToDecimalOption() =
            match this with
            | Unlimited -> None
            | Limit limit -> Some limit.Value
   ```
   It is more descriptive than just implying that `0M` means that there's no limit, since it also could mean that you can't spend money on this card. The only problem is since we've hidden the constructor, we can't do pattern matching. But no worries, we can use [Active Patterns](https://fsharpforfunandprofit.com/posts/convenience-active-patterns/):
   ```fsharp
    let (|Limit|Unlimited|) limit =
        match limit with
        | Limit dec -> Limit dec
        | Unlimited -> Unlimited
   ```
   Now we can pattern match `DailyLimit` everywhere as a regular DU.
3. **LetterString**

   That one is simple. We use same technique as in `CardNumber`. One little thing though: `LetterString` is hardly about credit cards, it's a rather thing and we should move it in `Common` project in `CommonTypes` module. Time comes we move `ValidationError` into separate place as well.
4. **UserId**

   That one is just an alias `type UserId = System.Guid`. We use it for descriptiveness only.

5. **Month and Year**

   Those have to go to `Common` too. `Month` is gonna be a discriminated union with methods to convert it to and from `unsigned int16`, `Year` is going to be like `CardNumber` but for `uint16` instead of string.

Now let's finish our domain types declaration. We need `User` with some user information and card collection, we need balance operations for top-ups and payments.
```fsharp
    type UserInfo =
        { Name: LetterString
          Id: UserId
          Address: Address }

    type User =
        { UserInfo : UserInfo
          Cards: Card list }

    [<Struct>]
    type BalanceChange =
        | Increase of increase: MoneyTransaction // another common type with validation for positive amount
        | Decrease of decrease: MoneyTransaction
        with
        member this.ToDecimal() =
            match this with
            | Increase i -> i.Value
            | Decrease d -> -d.Value

    [<Struct>]
    type BalanceOperation =
        { CardNumber: CardNumber
          Timestamp: DateTimeOffset
          BalanceChange: BalanceChange
          NewBalance: Money }
```
Good, we designed our types in a way that invalid state is unrepresentable. Now whenever we deal with instance of any of these types we are sure that data in there is valid and we don't have to validate it again. Now we can proceed to business logic!

### Business logic

We'll have an unbreakable rule here: all business logic is gonna be coded in **pure functions**. A pure function is a function which satisfies following criteria:

- The only thing it does is computes output value. It has no side effects at all.
- It always produces same output for the same input.

Hence pure functions don't throw exceptions, don't produce random values, don't interact with outside world at any form, be it database or a simple `DateTime.Now`. Of course interacting with impure function automatically renders calling function impure. So what shall we implement?

Here's a list of requirements we have:

- **Activate/deactivate card**
- **Process payments**

   We can process payment if:
     1. Card isn't expired
     2. Card is active
     3. There's enough money for the payment
     4. Spendings for today haven't exceeded daily limit.

- **Top up balance**

   We can top up balance for active and not expired card.

- **Set daily limit**

   User can set daily limit if card isn't expired and is active.

When operation can't be completed we have to return an error, so we need to define `OperationNotAllowedError`:
```fsharp
    type OperationNotAllowedError =
        { Operation: string
          Reason: string }

    // and a helper function to wrap it in `Error` which is a case for `Result<'ok,'error> type
    let operationNotAllowed operation reason = { Operation = operation; Reason = reason } |> Error
```
In this module with business logic that would be _the only_ type of error we return. We don't do validation in here, don't interact with database - just executing operations if we can otherwise return `OperationNotAllowedError`.

Full module can be found [here](https://github.com/atsapura/CardManagement/blob/master/CardManagement/CardActions.fs). I'll list here the trickiest case here: `processPayment`. We have to check for expiration, active/deactivated status, money spent today and current balance. Since we can't interact with outer world, we have to pass all the necessary information as parameters. That way this _logic_ would be very easy to test, and allows you to do [property based testing](https://github.com/fscheck/FsCheck).
```fsharp
    let processPayment (currentDate: DateTimeOffset) (spentToday: Money) card (paymentAmount: MoneyTransaction) =
        // first check for expiration
        if isCardExpired currentDate card then
            cardExpiredMessage card.CardNumber |> processPaymentNotAllowed
        else
        // then active/deactivated
        match card.AccountDetails with
        | Deactivated -> cardDeactivatedMessage card.CardNumber |> processPaymentNotAllowed
        | Active accInfo ->
            // if active then check balance
            if paymentAmount.Value > accInfo.Balance.Value then
                sprintf "Insufficent funds on card %s" card.CardNumber.Value
                |> processPaymentNotAllowed
            else
            // if balance is ok check limit and money spent today
            match accInfo.DailyLimit with
            | Limit limit when limit < spentToday + paymentAmount ->
                sprintf "Daily limit is exceeded for card %s with daily limit %M. Today was spent %M"
                    card.CardNumber.Value limit.Value spentToday.Value
                |> processPaymentNotAllowed
            (*
            We could use here the ultimate wild card case like this:
            | _ ->
            but it's dangerous because if a new case appears in `DailyLimit` type,
            we won't get a compile error here, which would remind us to process this
            new case in here. So this is a safe way to do the same thing.
            *)
            | Limit _ | Unlimited ->
                let newBalance = accInfo.Balance - paymentAmount
                let updatedCard = { card with AccountDetails = Active { accInfo with Balance = newBalance } }
                // note that we have to return balance operation, so it can be stored to DB later.
                let balanceOperation =
                    { Timestamp = currentDate
                      CardNumber = card.CardNumber
                      NewBalance = newBalance
                      BalanceChange = Decrease paymentAmount }
                Ok (updatedCard, balanceOperation)
```
This `spentToday` - we'll have to calculate it from `BalanceOperation` collection we'll keep in database. So we'll need module for that, which will basically have 1 public function:
```fsharp
    let private isDecrease change =
        match change with
        | Increase _ -> false
        | Decrease _ -> true

    let spentAtDate (date: DateTimeOffset) cardNumber operations =
        let date = date.Date
        let operationFilter { CardNumber = number; BalanceChange = change; Timestamp = timestamp } =
            isDecrease change && number = cardNumber && timestamp.Date = date
        let spendings = List.filter operationFilter operations
        List.sumBy (fun s -> -s.BalanceChange.ToDecimal()) spendings |> Money
```
Good. Now that we're done with all the business logic implementation, time to think about mapping. A lot of our types use discriminated unions, some of our types have no public constructor, so we can't expose them as is to the outside world. We'll need to deal with (de)serialization. Besides that, right now we have only one bounded context in our application, but later on in real life you would want to build a bigger system with multiple bounded contexts, and they have to interact with each other through public contracts, which should be comprehensible for everyone, including other programming languages.

We have to do both way mapping: from public models to domain and vise versa. While mapping from domain to models is pretty strait forward, the other direction has a bit of a pickle: models can have invalid data, after all we use plain types that can be serialized to json. Don't worry, we'll have to build our validation in that mapping. The very fact that we use different types for possibly invalid data and data, that's **always** valid means, that compiler won't let us forget to execute validation.

Here's what it looks like:
```fsharp
    // You can use type aliases to annotate your functions. This is just an example, but sometimes it makes code more readable
    type ValidateCreateCardCommand = CreateCardCommandModel -> ValidationResult<Card>
    let validateCreateCardCommand : ValidateCreateCardCommand =
        fun cmd ->
        // that's a computation expression for `Result<>` type.
        // Thanks to this we don't have to chose between short code and strait forward one,
        // like we have to do in C#
        result {
            let! name = LetterString.create "name" cmd.Name
            let! number = CardNumber.create "cardNumber" cmd.CardNumber
            let! month = Month.create "expirationMonth" cmd.ExpirationMonth
            let! year = Year.create "expirationYear" cmd.ExpirationYear
            return
                { Card.CardNumber = number
                  Name = name
                  HolderId = cmd.UserId
                  Expiration = month,year
                  AccountDetails =
                     AccountInfo.Default cmd.UserId
                     |> Active }
        }
```
Full module for mappings and validations is [here](https://github.com/atsapura/CardManagement/blob/master/CardManagement/CardDomainCommandModels.fs) and module for mapping to models is [here](https://github.com/atsapura/CardManagement/blob/master/CardManagement/CardDomainQueryModels.fs).

At this point we have implementation for all the business logic, mappings, validation and so on, and so far all of this is completely isolated from real world: it's written in pure functions entirely. Now you're maybe wondering, how exactly are we gonna make use of this? Because we do have to interact with outside world. More than that, during a workflow execution we have to make some decisions based on outcome of those real-world interactions. So the question is how do we assemble all of this? In OOP they use IoC containers to take care of that, but here we can't do that, since we don't even have objects, we have static functions.

We are gonna use `Interpreter pattern` for that! It's a bit tricky, mostly because it's unfamiliar, but I'll do my best to explain this pattern. First, let's talk about function composition. For instance we have a function `int -> string`. This means that function expects `int` as a parameter and returns string. Now let's say we have another function `string -> char`. At this point we can chain them, i.e. execute first one, take it's output and feed it to the second function, and there's even an operator for that: `>>`. Here's how it works:

```fsharp
let intToString (i: int) = i.ToString()
let firstCharOrSpace (s: string) =
    match s with
    | (null| "") -> ' '
    | s -> s.[0]

let firstDigitAsChar = intToString >> firstCharOrSpace

// And you can chain as many functions as you like
let alwaysTrue = intToString >> firstCharOrSpace >> Char.IsDigit
```

However we can't use simple chaining in some scenarios, e.g. activating card. Here's a sequence of actions:
- validate input card number. If it's valid, then
- try to get card by this number. If there's one
- activate it.
- save results. If it's ok then
- map to model and return.

The first two steps have that `If it's ok then...`. That's the reason why direct chaining is not working.

We could simply inject as parameters those functions, like this:
```fsharp
let activateCard getCardAsync saveCardAsync cardNumber = ...
```
But there're certain problems with that. First, number of dependencies can grow big and function signature will look ugly. Second, we are tied to specific effects in here: we have to choose if it's a `Task` or `Async` or just plain sync calls. Third, it's easy to mess things up when you have that many functions to pass: e.g. `createUserAsync` and `replaceUserAsync` have same signature but different effects, so when you have to pass them hundreds of times you can make a mistake with really weird symptoms. Because of those reasons we go for interpreter. 

The idea is that we divide our composition code in 2 parts: execution tree and interpreter for that tree. Every node in this tree is a place for a function with effect we want to inject, like `getUserFromDatabase`. Those nodes are defined by name, e.g. `getCard`, input parameter type, e.g. `CardNumber` and return type, e.g. `Card option`. We don't specify here `Task` or `Async`, that's not the part of the tree, _it's a part of interpreter_. Every edge of this tree is some series of pure transformations, like validation or business logic function execution. The edges also have some input, e.g. raw string card number, then there's validation, which can give us an error or a valid card number. If there's an error, we are gonna interrupt that edge, if not, it leads us to the next node: `getCard`. If this node will return `Some card`, we can continue to the next edge, which would be activation, and so on.

For every scenario like `activateCard` or `processPayment` or `topUp` we are gonna build a separate tree. When those trees are built, their nodes are kinda blank, they don't have real functions in them, _they have a place_ for those functions. The goal of interpreter is to fill up those nodes, simple as that. Interpreter knows effects we use, e.g. `Task`, and it knows which real function to put in a given node. When it visits a node, it executes corresponding real function, awaits it in case of `Task` or `Async`, and passes the result to the next edge. That edge may lead to another node, and then it's a work for interpreter again, until this interpreter reaches the stop node, the bottom of our recursion, where we just return the result of the whole execution of our tree.

The whole tree would be represented with discriminated union, and a node would look like this:

```fsharp
    type Program<'a> =
        | GetCard of CardNumber * (Card option -> Program<'a>) // <- THE NODE
        | ... // ANOTHER NODE
```

It's always gonna be a tuple, where the first element is an input for your dependency, and the last element is a _function_, which receives the result of that dependency. That "space" between those elements of tuple is where your dependency will fit in, like in those composition examples, where you have function `'a -> 'b`, `'c -> 'd` and you need to put another one `'b -> 'c` in between to connect them.

Since we are inside of our bounded context, we shouldn't have too many dependencies, and if we do - it's probably a time to split our context into smaller ones.

Here's what it looks like, full source is [here](https://github.com/atsapura/CardManagement/blob/master/CardManagement/CardProgramBuilder.fs):
```fsharp
    type Program<'a> =
        | GetCard of CardNumber * (Card option -> Program<'a>)
        | GetCardWithAccountInfo of CardNumber * ((Card*AccountInfo) option -> Program<'a>)
        | CreateCard of (Card*AccountInfo) * (Result<unit, DataRelatedError> -> Program<'a>)
        | ReplaceCard of Card * (Result<unit, DataRelatedError> -> Program<'a>)
        | GetUser of UserId * (User option -> Program<'a>)
        | CreateUser of UserInfo * (Result<unit, DataRelatedError> -> Program<'a>)
        | GetBalanceOperations of (CardNumber * DateTimeOffset * DateTimeOffset) * (BalanceOperation list -> Program<'a>)
        | SaveBalanceOperation of BalanceOperation * (Result<unit, DataRelatedError> -> Program<'a>)
        | Stop of 'a

    // This bind function allows you to pass a continuation for current node of your expression tree
    // the code is basically a boiler plate, as you can see.
    let rec bind f instruction =
        match instruction with
        | GetCard (x, next) -> GetCard (x, (next >> bind f))
        | GetCardWithAccountInfo (x, next) -> GetCardWithAccountInfo (x, (next >> bind f))
        | CreateCard (x, next) -> CreateCard (x, (next >> bind f))
        | ReplaceCard (x, next) -> ReplaceCard (x, (next >> bind f))
        | GetUser (x, next) -> GetUser (x,(next >> bind f))
        | CreateUser (x, next) -> CreateUser (x,(next >> bind f))
        | GetBalanceOperations (x, next) -> GetBalanceOperations (x,(next >> bind f))
        | SaveBalanceOperation (x, next) -> SaveBalanceOperation (x,(next >> bind f))
        | Stop x -> f x


    // this is a set of basic functions. Use them in your expression tree builder to represent dependency call
    let stop x = Stop x
    let getCardByNumber number = GetCard (number, stop)
    let getCardWithAccountInfo number = GetCardWithAccountInfo (number, stop)
    let createNewCard (card, acc) = CreateCard ((card, acc), stop)
    let replaceCard card = ReplaceCard (card, stop)
    let getUserById id = GetUser (id, stop)
    let createNewUser user = CreateUser (user, stop)
    let getBalanceOperations (number, fromDate, toDate) = GetBalanceOperations ((number, fromDate, toDate), stop)
    let saveBalanceOperation op = SaveBalanceOperation (op, stop)
```

With a help of [computation expressions](https://fsharpforfunandprofit.com/series/computation-expressions.html), we now have a very easy way to build our workflows without having to care about implementation of real-world interactions. We do that in [CardWorkflow module](https://github.com/atsapura/CardManagement/blob/master/CardManagement/CardWorkflow.fs):

```fsharp
    // `program` is the name of our computation expression.
    // In every `let!` binding we unwrap the result of operation, which can be
    // either `Program<'a>` or `Program<Result<'a, Error>>`. What we unwrap would be of type 'a.
    // If, however, an operation returns `Error`, we stop the execution at this very step and return it.
    // The only thing we have to take care of is making sure that type of error is the same in every operation we call
    let processPayment (currentDate: DateTimeOffset, payment) =
        program {
            (* You can see these `expectValidationError` and `expectDataRelatedErrors` functions here.
               What they do is map different errors into `Error` type, since every execution branch
               must return the same type, in this case `Result<'a, Error>`.
               They also help you quickly understand what's going on in every line of code:
               validation, logic or calling external storage. *)
            let! cmd = validateProcessPaymentCommand payment |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let today = currentDate.Date |> DateTimeOffset
            let tomorrow = currentDate.Date.AddDays 1. |> DateTimeOffset
            let! operations = getBalanceOperations (cmd.CardNumber, today, tomorrow)
            let spentToday = BalanceOperation.spentAtDate currentDate cmd.CardNumber operations
            let! (card, op) =
                CardActions.processPayment currentDate spentToday card cmd.PaymentAmount
                |> expectOperationNotAllowedError
            do! saveBalanceOperation op |> expectDataRelatedErrorProgram
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }
```

This module is the last thing we need to implement in business layer. Also, I've done some refactoring: I moved errors and common types to [Common project](https://github.com/atsapura/CardManagement/tree/master/CardManagement.Common). About time we moved on to implementing data access layer.

### Data access layer

The design of entities in this layer may depend on our database or framework we use to interact with it. Therefore domain layer doesn't know anything about these entities, which means we have to take care of mapping to and from domain models in here. Which is quite convenient for consumers of our DAL API. For this application I've chosen MongoDB, not because it's a best choice for this kind of task, but because there're many examples of using SQL DBs already and I wanted to add something different. We are gonna use C# driver.
For the most part it's gonna be pretty strait forward, the only tricky moment is with `Card`. When it's active it has an `AccountInfo` inside, when it's not it doesn't. So we have to split it in two documents: `CardEntity` and `CardAccountInfoEntity`, so that deactivating card doesn't erase information about balance and daily limit.
Other than that we just gonna use primitive types instead of discriminated unions and types with built-in validation.

There're also few things we need to take care of, since we are using C# library:
- Convert `null`s to `Option<'a>`
- Catch expected exceptions and convert them to our errors and wrap it in `Result<_,_>`

We start with [CardDomainEntities module](https://github.com/atsapura/CardManagement/blob/master/CardManagement.Data/CardDomainEntities.fs), where we define our entities:
```fsharp
    [<CLIMutable>]
    type CardEntity =
        { [<BsonId>]
          CardNumber: string
          Name: string
          IsActive: bool
          ExpirationMonth: uint16
          ExpirationYear: uint16
          UserId: UserId }
        with
        // we're gonna need this in every entity for error messages
        member this.EntityId = this.CardNumber.ToString()
        // we use this Id comparer quotation (F# alternative to C# Expression) for updating entity by id,
        // since for different entities identifier has different name and type
        member this.IdComparer = <@ System.Func<_,_> (fun c -> c.CardNumber = this.CardNumber) @>
```

Those fields `EntityId` and `IdComparer` we are gonna use with a help of [SRTP](https://gist.github.com/atsapura/fd9d7aa26e337eaa2f7f04d6cbb58ef6). We'll define functions that will retrieve them from any type that has those fields define, without forcing every entity to implement some interface:

```fsharp
    let inline (|HasEntityId|) x =
        fun () -> (^a : (member EntityId: string) x)

    let inline entityId (HasEntityId f) = f()

    let inline (|HasIdComparer|) x =
        fun () -> (^a : (member IdComparer: Quotations.Expr<Func< ^a, bool>>) x)

    // We need to convert F# quotations to C# expressions which C# mongo db driver understands.
    let inline idComparer (HasIdComparer id) =
        id()
        |> LeafExpressionConverter.QuotationToExpression 
        |> unbox<Expression<Func<_,_>>>
```

As for `null` and `Option` thing, since we use record types, F# compiler doesn't allow using `null` value, neither for assigning nor for comparison. At the same time record types are just another CLR types, so technically we can and will get a `null` value, thanks to C# and design of this library. We can solve this in 2 ways: use `AllowNullLiteral` attribute, or use `Unchecked.defaultof<'a>`. I went for the second choice since this `null` situation should be localized as much as possible:

```fsharp
    let isNullUnsafe (arg: 'a when 'a: not struct) =
        arg = Unchecked.defaultof<'a>

    // then we have this function to convert nulls to option, therefore we limited this
    // toxic null thing in here.
    let unsafeNullToOption a =
        if isNullUnsafe a then None else Some a
```

In order to deal with expected exception for duplicate key, we use Active Patterns again:

```fsharp
    // First we define a function which checks, whether exception is about duplicate key
    let private isDuplicateKeyException (ex: Exception) =
        ex :? MongoWriteException && (ex :?> MongoWriteException).WriteError.Category = ServerErrorCategory.DuplicateKey

    // Then we have to check wrapping exceptions for this
    let rec private (|DuplicateKey|_|) (ex: Exception) =
        match ex with
        | :? MongoWriteException as ex when isDuplicateKeyException ex ->
            Some ex
        | :? MongoBulkWriteException as bex when bex.InnerException |> isDuplicateKeyException ->
            Some (bex.InnerException :?> MongoWriteException)
        | :? AggregateException as aex when aex.InnerException |> isDuplicateKeyException ->
            Some (aex.InnerException :?> MongoWriteException)
        | _ -> None

    // And here's the usage:
    let inline private executeInsertAsync (func: 'a -> Async<unit>) arg =
        async {
            try
                do! func(arg)
                return Ok ()
            with
            | DuplicateKey ex ->
                    return EntityAlreadyExists (arg.GetType().Name, (entityId arg)) |> Error
        }
```

After mapping is implemented we have everything we need to assemble [API for our data access layer](https://github.com/atsapura/CardManagement/blob/master/CardManagement.Data/CardDataPipeline.fs), which looks like this:

```fsharp
    // `MongoDb` is a type alias for `IMongoDatabase`
    let replaceUserAsync (mongoDb: MongoDb) : ReplaceUserAsync =
        fun user ->
        user |> DomainToEntityMapping.mapUserToEntity
        |> CommandRepository.replaceUserAsync mongoDb

    let getUserInfoAsync (mongoDb: MongoDb) : GetUserInfoAsync =
        fun userId ->
        async {
            let! userInfo = QueryRepository.getUserInfoAsync mongoDb userId
            return userInfo |> Option.map EntityToDomainMapping.mapUserInfoEntity
        }
```

The last moment I mention is when we do mapping `Entity -> Domain`, we have to instantiate types with built-in validation, so there can be validation errors. In this case we won't use `Result<_,_>` because if we've got invalid data in DB, it's a bug, not something we expect. So we just throw an exception. Other than that nothing really interesting is happening in here. The full source code of data access layer you'll find [here](https://github.com/atsapura/CardManagement/tree/master/CardManagement.Data).

### Composition, logging and all the rest

As you remember, we're not gonna use DI framework, we went for interpreter pattern. If you want to know why, here's some reasons:
- IoC container operates in runtime. So until you run your program you can't know that all the dependencies are satisfied.
- It's a powerful tool which is very easy to abuse: you can do property injection, use lazy dependencies, and sometimes even some business logic can find it's way in dependency registering/resolving (yeah, I've witnessed it). All of that makes code maintaining extremely hard.

That means we need a place for that functionality. We could place it on a top level in our Web Api, but in my opinion it's not a best choice: right now we are dealing with only 1 bounded context, but if there's more, this global place with all the interpreters for each context will become cumbersome. Besides, there's single responsibility rule, and web api project should be responsible for web, right? So we create [CardManagement.Infrastructure project](https://github.com/atsapura/CardManagement/tree/master/CardManagement.Infrastructure).

Here we will do several things:
- Composing our functionality
- App configuration
- Logging

If we had more than 1 context, app configuration and log configuration should be moved to global infrastructure project, and the only thing happening in this project would be assembling API for our bounded context, but in our case this separation is not necessary.

Let's get down to composition. We've built execution trees in our domain layer, now we have to interpret them. Every node in that tree represents some dependency call, in our case a call to database. If we had a need to interact with 3rd party api, that would be in here also. So our interpreter has to know how to handle every node in that tree, which is verified in compile time, thanks to `<TreatWarningsAsErrors>` setting. Here's what it looks like:

```fsharp
    // Those `bindAsync (next >> interpretCardProgram mongoDb)` work pretty simple:
    // we execute async function to the left of this expression, await that operation
    // and pass the result to the next node, after which we interpret that node as well,
    // until we reach the bottom of this recursion: `Stop a` node.
    let rec private interpretCardProgram mongoDb prog =
        match prog with
        | GetCard (cardNumber, next) ->
            cardNumber |> getCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetCardWithAccountInfo (number, next) ->
            number |> getCardWithAccInfoAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | CreateCard ((card,acc), next) ->
            (card, acc) |> createCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | ReplaceCard (card, next) ->
            card |> replaceCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetUser (id, next) ->
            getUserAsync mongoDb id |> bindAsync (next >> interpretCardProgram mongoDb)
        | CreateUser (user, next) ->
            user |> createUserAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetBalanceOperations (request, next) ->
            getBalanceOperationsAsync mongoDb request |> bindAsync (next >> interpretCardProgram mongoDb)
        | SaveBalanceOperation (op, next) ->
             saveBalanceOperationAsync mongoDb op |> bindAsync (next >> interpretCardProgram mongoDb)
        | Stop a -> async.Return a

    let interpret prog =
        try
            let interpret = interpretCardProgram (getMongoDb())
            interpret prog
        with
        | failure -> Bug failure |> Error |> async.Return
```

Note that this interpreter is the place where we have this `async` thing. We can do another interpreter with `Task` or just a plain sync version of it. Now you're probably wondering, how we can cover this with unit-test, since familiar mock libraries ain't gonna help us. Well, it's easy: you have to make another interpreter. Here's what it can look like:

```fsharp
    type SaveResult = Result<unit, DataRelatedError>

    type TestInterpreterConfig =
        { GetCard: Card option
          GetCardWithAccountInfo: (Card*AccountInfo) option
          CreateCard: SaveResult
          ReplaceCard: SaveResult
          GetUser: User option
          CreateUser: SaveResult
          GetBalanceOperations: BalanceOperation list
          SaveBalanceOperation: SaveResult }

    let defaultConfig =
        { GetCard = Some card
          GetUser = Some user
          GetCardWithAccountInfo = (card, accountInfo) |> Some
          CreateCard = Ok()
          GetBalanceOperations = balanceOperations
          SaveBalanceOperation = Ok()
          ReplaceCard = Ok()
          CreateUser = Ok() }

    let testInject a = fun _ -> a

    let rec interpretCardProgram config (prog: Program<'a>) =
        match prog with
        | GetCard (cardNumber, next) ->
            cardNumber |> testInject config.GetCard |> (next >> interpretCardProgram config)
        | GetCardWithAccountInfo (number, next) ->
            number |> testInject config.GetCardWithAccountInfo |> (next >> interpretCardProgram config)
        | CreateCard ((card,acc), next) ->
            (card, acc) |> testInject config.CreateCard |> (next >> interpretCardProgram config)
        | ReplaceCard (card, next) ->
            card |> testInject config.ReplaceCard |> (next >> interpretCardProgram config)
        | GetUser (id, next) ->
            id |> testInject config.GetUser |> (next >> interpretCardProgram config)
        | CreateUser (user, next) ->
            user |> testInject config.CreateUser |> (next >> interpretCardProgram config)
        | GetBalanceOperations (request, next) ->
            testInject config.GetBalanceOperations request |> (next >> interpretCardProgram config)
        | SaveBalanceOperation (op, next) ->
             testInject config.SaveBalanceOperation op |> (next >> interpretCardProgram config)
        | Stop a -> a
```

We've created `TestInterpreterConfig` which holds desired results of every operation we want to inject. You can easily change that config for every given test and then just run interpreter. This interpreter is sync, since there's no reason to bother with `Task` or `Async`.

There's nothing really tricky about the logging, but you can find it in [this module](https://github.com/atsapura/CardManagement/blob/master/CardManagement.Infrastructure/Logging.fs). The approach is that we wrap the function in logging: we log function name, parameters and log result. If result is ok, it's info, if error it's a warning and if it's a `Bug` then it's an error. That's pretty much it.

One last thing is to make a facade, since we don't want to expose raw interpreter calls. Here's the whole thing:

```fsharp
    let createUser arg =
        arg |> (CardWorkflow.createUser >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.createUser")
    let createCard arg =
        arg |> (CardWorkflow.createCard >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.createCard")
    let activateCard arg =
        arg |> (CardWorkflow.activateCard >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.activateCard")
    let deactivateCard arg =
        arg |> (CardWorkflow.deactivateCard >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.deactivateCard")
    let processPayment arg =
        arg |> (CardWorkflow.processPayment >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.processPayment")
    let topUp arg =
        arg |> (CardWorkflow.topUp >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.topUp")
    let setDailyLimit arg =
        arg |> (CardWorkflow.setDailyLimit >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.setDailyLimit")
    let getCard arg =
        arg |> (CardWorkflow.getCard >> CardProgramInterpreter.interpret |> logifyResultAsync "CardApi.getCard")
    let getUser arg =
        arg |> (CardWorkflow.getUser >> CardProgramInterpreter.interpretSimple |> logifyResultAsync "CardApi.getUser")
```

All the dependencies here are injected, logging is taken care of, no exceptions is thrown - that's it. For web api I used [Giraffe](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md) framework. Web project is [here](https://github.com/atsapura/CardManagement/tree/master/CardManagement.Api/CardManagement.Api).


## Conclusion

We have built an application with validation, error handling, logging, business logic - all those things you usually have in your application. The difference is this code is way more durable and easy to refactor. Note that we haven't used reflection or code generation, no exceptions, but still our code isn't verbose. It's easy to read, easy to understand and hard to break. As soon as you add another field in your model, or another case in one of our union types, the code won't compile until you update every usage. Sure it doesn't mean you're totally safe or that you don't need any kind of testing at all, it just means that you're gonna have fewer problems when you develope new features or do some refactoring. The development process will be both cheaper and more interesting, because this tool allows you to focus on your domain and business tasks, instead of drugging focus on keeping an eye out that nothing is broken.

Another thing: I don't claim that OOP is completely useless and we don't need it, that's not true. I'm saying that we don't need it for solving _every single task_ we have, and that a big portion of our tasks can be better solved with FP. And truth is, as always, in balance: we can't solve everything efficiently with only one tool, so a good programming language should have a decent support of both FP and OOP. And, unfortunately, a lot of most popular languages today have only lambdas and async programming from functional world.
