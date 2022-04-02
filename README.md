# Crypter

## Licensing

Crypter is open-source software that is free for non-commercial use under the AGPLv3. A copy of the AGLPv3 is provided in [LICENSE.md](LICENSE.md).

You and/or your organization may be released from the terms of the AGLPv3 by purchasing a commercial license from the copyright holder.

Please reach out to <jackedwards@protonmail.com> or submit an issue if you believe these licensing terms are a cause for concern.

## Getting Started

Check out these documents to get started working on Crypter:

* [Contribution Guide](./CONTRIBUTING.md)
* [Coding Standard](<./Docs/Development/Coding Standard.md>)
* [Development Environment Setup](<./Docs/Development/Development Environment Setup.md>)

Also take a look at some of the articles that have come in handy while working on the project:

* [Learning Material](<./Docs/Learning Material.md>)

If you have any questions, please add an issue or send an email to <jackedwards@protonmail.com>.
I would love to hear from you.

## Projects

### Crypter.API

A RESTful API written using ASP.NET.

### Crypter.ClientServices

A class library containing various interfaces and most of their implementations.

The "Repository" interfaces must be implemented per-environment, since these implementations must decide where and how to store data on the client device.
These decisions and locations are device-specific.  For example, storing data in a browser is likely to be different than storing data on a mobile phone.

### Crypter.Common

A small class library containing domain models and data types that may be used in any project.

### Crypter.Console

A command-line program that helps administer the Crypter servers.
For example, this program contains commands to create all the database tables from scratch and perform schema migrations.

### Crypter.Contracts

A class library containing classes and enumerations to help facilitate communcation between different projects.
For example, if Crypter.Web needs to POST some data to Crypter.API, there should be a class defining what that data looks like.

### Crypter.Core

A class library containing code to interact with the PostgreSQL database.
Most of the back-end business logic should be located here.

### Crypter.CryptoLib

A class library that mostly acts as a wrapper around the BouncyCastle cryptography library.
Most of the project's code related to cryptography is located here.

### Crypter.Test

A project containing NUnit unit tests.

### Crypter.Web

A web application written using ASP.NET Blazor web-assembly.
