# Crypter

End-to-end encrypted file and message transfer. ASP.NET Core API + Blazor WebAssembly client, PostgreSQL, libsodium.

See [CONTRIBUTING.md](CONTRIBUTING.md) and [Coding Standard](<Documentation/Development/Coding Standard.md>) for the human-facing versions of these rules.

## Layout

| Project | Purpose |
|---|---|
| `Crypter.API` | RESTful API |
| `Crypter.Core` | Back-end business logic |
| `Crypter.DataAccess` | EF Core / PostgreSQL, entities and migrations |
| `Crypter.Common` | Domain models, monads, primitives shared by everything |
| `Crypter.Common.Client` | Client-side interfaces and implementations; `*Repository` interfaces are implemented per-platform |
| `Crypter.Crypto.Common` | Cryptographic primitive interfaces |
| `Crypter.Crypto.Providers.Browser` | libsodium via BlazorSodium |
| `Crypter.Crypto.Providers.Default` | libsodium via Geralt, for non-browser platforms |
| `Crypter.Web` | Blazor WebAssembly client |
| `Crypter.Test` | NUnit tests for everything except the web client |
| `Crypter.Test.Web` | NUnit tests for browser-specific code |

## Commands

```bash
dotnet build Crypter.sln
dotnet test Crypter.Test                      # requires Docker (Testcontainers spins up PostgreSQL)
dotnet test Crypter.Test --filter FullyQualifiedName~SomeTest
dotnet test Crypter.Test.Web                  # requires the wasm-tools workload
docker compose up                             # local stack
```

`Crypter.Web` runs `pnpm install` and several `vite build` scripts in a PreBuild target, so building it (or the solution) requires pnpm. CI pins pnpm 11.18.0 and .NET 10; keep the Dockerfiles and workflows on the same versions when either moves.

## Code style

- Follow the [Coding Standard](<Documentation/Development/Coding Standard.md>): constructors over object initializers, `Async` suffix on async methods, enums over magic strings.
- Use `Maybe<T>` and `Either<TLeft, TRight>` from `Crypter.Common/Monads` rather than nulls or exceptions for expected failures, and the validated types in `Crypter.Common/Primitives` rather than raw strings.
- Async all the way for database, file, and network IO.
- `.editorconfig` governs formatting and naming. Private fields are `_camelCase`, everything else follows normal C# conventions.
- Do not write comments that narrate history — no "bumped from X to Y", "was previously Z", "new in .NET 10". Comments explain the code as it stands.
- Schema changes mean editing the entities under `Crypter.DataAccess/Entities` and adding an EF Core migration in `Crypter.DataAccess/Migrations`. Some migrations need a companion script in `Crypter.DataAccess/Scripts` to be run first.

## Commits

- Base new branches on `stable` and target `stable` in pull requests. `main` and `stable` must always be releasable.
- Work in a git worktree. Create it from `origin/stable`, not from the default branch, then enter it by path:
  ```bash
  git worktree add .claude/worktrees/{name} -b {branch} origin/stable
  ```
- Subject line: imperative mood, capitalized, no trailing period, under ~72 characters. Existing history reads `Add basic tests for getting transfer settings`, `Fix Docker image builds for .NET 10 and pnpm 11`, `Use SemaphoreSlim to limit access to UserTransferSettings memory cache`.
- No prefixes or tags — this repo does not use Conventional Commits.
- Body is optional for small, self-explanatory changes. When a change is non-obvious, write a body wrapped at ~80 characters explaining *why*: what broke, what constraint forced the approach, what was ruled out. Describe consequences, not a file-by-file list of the diff.
- Keep commits clean before opening a pull request; multiple commits per pull request are fine.

## Pull requests

- Title reads like a commit subject.
- Description is a few sentences of plain English saying what changed and why. Short is good.
- Do not argue the case. No justifying the approach, pre-empting objections, listing rejected alternatives, or citing evidence that the change is sound. Reviewers are on the same side.
- Call out what a reviewer would otherwise have to discover: breaking API changes, migrations, deployment steps, dependencies deliberately held back. That is information, not argument.
