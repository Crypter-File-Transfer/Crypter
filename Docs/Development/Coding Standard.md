# Coding Standard

## 1. Be Explicit

### Use constructors to instantiate models

*As opposed using using factories or object initializers.*

Constructors exist to make sure we never create an invalid instance of an object.

Multiple constructors and constructors with optional parameters are fine, as long as they give us back valid objects.

We should not be writing code to check whether a given object is valid. Rather, invalid objects should not exist in the first place.

```
public class Student()
{
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }

    public Student(string firstName, string lastName, string middleName = "")
    {
        FirstName = firstName;
        MiddleName = middleName
        LastName = lastName;
    }
}
```

### Label a method `async` if it runs asynchronously

This makes it obvious the method can run asynchronously and should be awaited.

`public async Task<foo> GetFooAsync(Guid id)`

### Use enum types

*As opposed to using strings or numbers*

Enum types are great for creating categories of something. You can inspect an enum to see everywhere it is being used,
you can easily `switch` over an enum, and you can always be sure you are considering every value in the enum because every value is defined right in the enum declaration.

```
public enum FailureReason
{
    None,
    MissingArgument,
    InvalidArgument,
    SomeOtherConditionNotMet
}
```

### Use Monads and Value objects

#### Monads

Become acquainted with the couple of monads that exist under [Crypter.Common/Monads](<./../../Crypter.Common/Monads>):

* Either
* Maybe

These Monads offer a lot in terms of flexibility and making it clear what types of data a method expects or may return.
It is a lot easier to understand that a method returning `Maybe<Foo>` may return an instance of `Foo`, or it may not.

#### Primitives

And have a look at the "Primitive" objects that exist under [Crypter.Common/Primitives](<./../../Crypter.Common/Primitives>).

Thanks to the built-in validation of these classes, we can trust that every instance of one of these classes is already valid.
We do not need to check the validity of something every time it gets passed around.

## 2. Use `async` Methods

### Database queries

Always use asynchronous methods when querying the database.

`var myFoo = await db.GetFooAsync(Guid id);`


### File IO

When performing file IO.
```
var fileBytes = await File.ReadAllBytesAsync(file);

await File.WriteAllBytesAsync(newFile, data);
```

### Network calls

`var response = await _httpClient.SendAsync(request);`

## 3. Naming Conventions

### Convention for C#

Identifiers in C# code should follow the formats provided below.

| Type             | Format              |
|------------------|---------------------|
| Interface        | **IUpperCamelCase** |
| Private field    | **_lowerCamelCase** |
| Public field     | UpperCamelCase      |
| Protected field  | UpperCamelCase      |
| Internal field   | UpperCamelCase      |
| Property         | UpperCamelCase      |
| Method           | UpperCamelCase      |
| Class            | UpperCamelCase      |
| Local Variable   | UpperCamelCase      |
| Parameter        | UpperCamelCase      |
| Enum             | UpperCamelCase      |

### Enumerations

An enumeration should have a singular name, unless it represents a bitwise flag.

```
// Singular
public enum Day
{
    Sunday,    // = 0
    Monday,    // = 1
    Tuesday,   // = 2
    Wednesday, // = 3
    Thursday,  // = 4
    Friday,    // = 5
    Saturday   // = 6
}

// Bitwise
public enum Days
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32
}
```
