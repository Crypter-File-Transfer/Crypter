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
* Do not merge incomplete or partial code. The `master` and `stable` branches should always be in a releasable state.

There are two different procedures for submitting pull requests.
The first procedure is for members of the `Crypter-File-Transfer` team in Github.
The second procedure is for contributors who are not team members.

All pull requests are treated equally.

### For team members

1. Create a new branch, based on `stable`.
2. Do whatever you want in this branch while developing.
3. Clean up your commits when you are done developing.
4. Rebase your branch with the latest commits from `stable`.
5. Submit a pull request to merge back into `stable`.
6. Verify your branch is deleted from Github after your pull request is accepted and merged.

### For contributors who are not team members

1. Fork the repository, thus creating your own version of the code.
2. Create a new branch, based on `stable`.
3. Do whatever you want in this branch while developing.
4. Clean up your commits when you are done developing.
5. Fetch and/or pull from the upstream repository, owned by `Crypter-File-Transfer`.
6. Rebase your branch with the latest code from the upstream `stable` branch.
7. Submit a pull request to merge back into the upstream `stable` branch.

## Schema Updates

The database schema is made up of the models located under `Crypter.Core/Entities`.
Each of these models correspond to a table in the Crypter database.
Every property corresponds to a column in that table.
If you need to update the database schema, these models must be updated.

## Migrations

Refer to the `Docs/Production/Deployment/PostgreSQL.md` document on how to create a schema migration.
