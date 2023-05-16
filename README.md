# Crypter

## Licensing

Crypter is open-source software with a dual license.

### Personal and Non-Commercial Use

For free, non-commercial use, Crypter is licensed under the AGPLv3.

A copy of the AGLPv3 is provided in [LICENSE.md](LICENSE.md).

### Commercial Use

Contact the current copyright holder in order to purchase a commercial license.

Email: <jackedwards@protonmail.com>

## Getting Started

Check out these documents to get started working on Crypter:

* [Contribution Guide](./CONTRIBUTING.md)
* [Coding Standard](<./Documentation/Development/Coding Standard.md>)
* [Development Environment Setup](<./Documentation/Development/Development Environment Setup.md>)

Also take a look at some of the articles that have come in handy while working on the project:

* [Learning Material](<./Documentation/Learning%20Material.md>)

If you have any questions, please add an issue or send an email to <jackedwards@protonmail.com>.
I would love to hear from you.

## Projects

### Crypter.API

A RESTful API written using ASP.NET.

### Crypter.Benchmarks

A sandbox for benchmarking various things.

### Crypter.Common

A small class library containing domain models and data types that may be used in any project.

### Crypter.Common.Client

A class library containing various interfaces and most of their implementations for use in client applications.

The "Repository" interfaces must be implemented per-environment, since these implementations must decide where and how to store data on the client device.
These decisions and locations are device-specific.  For example, storing data in a browser is likely to be different than storing data on a mobile phone.

### Crypter.Core

A class library containing code to interact with the PostgreSQL database.
Most of the back-end business logic should be located here.

### Crypter.Crypto.Common

A class library containing interfaces and portable implementations of various cryptographic primitives.

### Crypter.Crypto.Providers.Browser

An implementation of the `Crypter.Crypto.Common` interfaces for use in browsers.
This uses libsodium through the [BlazorSodium](https://github.com/Jack-Edwards/BlazorSodium) nuget package.

### Crypter.Crypto.Providers.Default

An implementation of the `Crypter.Crypto.Common` interfaces for use in non-browser platforms.
This uses libsodium through the [Geralt](https://github.com/samuel-lucas6/Geralt) nuget package, as well as `System.Security.Cryptography` for random number generation.

### Crypter.Test

A project containing NUnit unit tests.

### Crypter.Web

A web application written using ASP.NET Blazor web-assembly.

## Acknowledgements

Thank you to the following people and organizations for helping make this project possible:

* [Santiago Escalante](https://github.com/saescalante) and [Steve Peters-Luciani](https://github.com/spetersluciani) for helping create Crypter as a final project for school.
* [Legion of the Bouncy Castle](https://bouncycastle.org/) for writing the [BouncyCastle](https://github.com/bcgit/bc-csharp) cryptographic library.
* [Frank Denis](https://github.com/jedisct1) et al. for writing the [libsodium](https://doc.libsodium.org/) cryptographic library.
* [Samuel Lucas](https://github.com/samuel-lucas6) for writing the [Geralt](https://github.com/samuel-lucas6/Geralt) C# language binding for libsodium.
* [Marek Fisera](https://github.com/maraf) and [Pavel Savara](https://github.com/pavelsavara) for all the help with BlazorSodium and Blazor in general.

And special thanks to the **C#** and **Web Dev Buddies** Discord servers for answering all my questions and providing motivation to continue working on this project.
