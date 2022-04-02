# Contributing

Thank you for showing an interest in contributing to this project. Extra thanks for taking the time to read the documentation.

The purpose of this contribution guide is to help others get started with contributing to the project.
The intent is not to create a bunch of procedures or any level of bureaucracy that everyone must follow.

Please feel free to contribute in whatever way you wish.
If you have questions, send me an email at <jackedwards@protonmail.com>.

## Issue Tracking

Most of the tasks that need to be completed are introduced as *Issues*. These issues can be bug reports, feature requests, requests for documentation, or tasks that just need to be performed.

Feel free to begin working on any of the existing, unassigned issues. If you are not part of the team and are unable to assign yourself to an issue, you may comment on the issue to let others know you are working on it.

As a new contributor to the project, please create issues before working on something new.  It helps keep things transparent and gives any code owners time to consider and comment on the issue.

## Pull Requests

General guidelines for everyone to follow:
* Clean up your commits before submitting a pull request.
* Use concise, descriptive commit messages.
* Multiple commits per pull request are okay.
* It is highly encouraged to improve upon your open pull requests. If you see room for improvement, then make the necessary changes and push a new commit.
* All pull requests should have a description written by the author. Describe your change in plain English.

There are two different procedures for submitting pull requests.
The first procedure is for members of the `Crypter-File-Transfer` team in Github.
The second procedure is for contributors who are not team members.

All pull requests are treated equally.

### For team members

1. Create a new branch, based on `dev`.
2. Do whatever you want in this branch while developing.
3. Clean up your commits when you are done developing.
4. Rebase your branch with the latest commits from `dev`.
5. Submit a pull request to merge back into `dev`.
6. Verify your branch is deleted from Github after your pull request is accepted and merged.

### For contributors who are not team members

1. Fork the repository, thus creating your own version of the code.
2. Create a new branch, based on `dev`.
3. Do whatever you want in this branch while developing.
4. Clean up your commits when you are done developing.
5. Fetch and/or pull from the upstream repository, owned by `Crypter-File-Transfer`.
6. Rebase your branch with the latest code from the upstream `dev` branch.
7. Submit a pull request to merge back into the upstream `dev` branch.

## Schema Updates

The Crypter database schema is defined in two areas.

The first area is in `Crypter.Core/Models`. Each of these classes correspond to a table in the Crypter database. Every property corresponds to a column in that table. If you need to update the database schema, these models must be updated.

The second area is in `Crypter.Console/SqlScripts`. There are scripts to create tables, drop tables, and perform schema migrations. These scripts must also be updated and kept up-to-date as changes to the database schema are made.

### Add a new table

If you need to add a new table to the Crypter database schema, follow these instructions:

1. Add a new `Create_Foo.sql` script to `Crypter.Console`.
2. Add a new `Drop_Foo.sql` script to `Crypter.Console`.
3. Add a new `MigrationX.sql` script to `Crypter.Console`.
4. Add references to the `Create_Foo.sql` and `Drop_Foo.sql` scripts in the `Crypter.Console/Jobs/ManageSchema` class.

### Modify an existing table

If you need to modify an existing table, follow these instructions:

1. Modify the existing `Create_Foo.sql` script.

### Create the Migration

A migration always needs to be written when the database schema is modified.

In most cases, a new `MigrationX.sql` script will need to be added to `Crypter.Console`.
However, if latest migration has not been applied to the production database, then it may be more appropriate to modify the latest migration instead.
Either ask which way is best before making a change, or submit your change and we will hopefully catch it during code review.
