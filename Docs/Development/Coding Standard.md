# Coding Standard

## 1. Be Explicit

### Use constructors to instantiate models

*As opposed using using factories or object initializers.*

This helps make sure we never have object instances with missing properties.
For example, we may add a new property to a class down the road. 
Chances are we would forget to go back and update all the existing object initializers.

On the other hand, with constructors, we can be explicit as to which properties are required and which are not.

Any line of code that fails to adhere to the constructor will cause a build failure until every reference is updated to adhere to the constructor.

```
public class Student()
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public Student(string firstName, string LastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
```

### Label a method `async` if it runs asynchronously

This makes it obvious to everyone the method runs asynchronously, which helps make sure we actually `await` the method.

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
