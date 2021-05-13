# Crypter

## Coding Standard

### No object initializers for custom classes
Use a constructor instead.

This helps make sure we never have object instances with missing properties.  For example, we may add a new property to a class down the road.  Chances are we would forget to go back and update all the existing object initializers.

If we enforce all properties via constructors, the build will fail until we update all pre-existing calls to create an instance of that class.

#### Good

```
var student = new Student("Craig", "Playstead");
```

#### Bad

```
Student student = new Student
{
    FirstName = "Craig",
    LastName = "Playstead"
};
```

### No blocking calls in the API

Examples:

* Always query the database asynchronously
