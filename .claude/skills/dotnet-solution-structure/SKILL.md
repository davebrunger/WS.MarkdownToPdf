---
name: dotnet-solution-structure
description: Rules for organising .NET solutions and projects — directory layout, naming conventions, project types, dependencies, shared code, configuration, and build structure for .NET 8+ solutions.
---

# .NET Solution & Project Structure

## Solution Layout

- Place the `.sln` file at the repository root
- Group projects into top-level folders by concern:

```
repo-root/
├── src/              # Production source projects
├── tests/            # Test projects
├── docs/             # Documentation (if applicable)
├── build/            # Build scripts, CI/CD definitions
└── Solution.sln
```

- Mirror the `src/` structure in `tests/` — each source project should have a corresponding test project (e.g., `src/Ordering.Api/` → `tests/Ordering.Api.Tests/`)

## Project Naming

- Use the pattern `<Product>.<Component>` for project names and namespaces (e.g., `WS.Ordering.Api`, `WS.Ordering.Domain`)
- Match the project folder name to the project name exactly
- Use `.Tests` suffix for unit/integration test projects (e.g., `WS.Ordering.Api.Tests`)
- Use `.IntegrationTests` when separating integration from unit tests

## Project Types & Responsibilities

Organise projects by architectural layer or bounded context. Common project types:

| Project suffix | Purpose |
|----------------|---------|
| `.Api` / `.Web` | HTTP host — controllers, middleware, `Program.cs` |
| `.Domain` | Domain models, entities, value objects, domain services |
| `.Application` | Use cases, commands, queries, handlers, DTOs |
| `.Infrastructure` | Data access, external services, messaging adapters |
| `.Contracts` / `.Abstractions` | Shared interfaces, DTOs, and events consumed by other projects |
| `.ServiceDefaults` | Shared Aspire or hosting defaults |

- Keep host/entry-point projects thin — delegate logic to lower layers
- Domain projects must have **zero** infrastructure dependencies

## Dependency Direction

- Dependencies flow **inward**: Api → Application → Domain
- Infrastructure implements interfaces defined in Domain or Application
- Never reference an `.Api` or `.Web` project from a library project
- Use `Contracts`/`Abstractions` projects to share types across service boundaries without coupling to implementations
- Avoid circular references — if two projects need each other, extract the shared contract

## Shared Code

- Place truly cross-cutting utilities (e.g., extension methods, guard helpers) in a `*.Common` or `*.Shared` project
- Keep shared projects small — resist the urge to dump unrelated code into a catch-all library
- Prefer NuGet packages for code shared across multiple solutions

## Project File (.csproj) Conventions

- Use the SDK-style project format (`<Project Sdk="Microsoft.NET.Sdk">`)
- Target a single framework unless multi-targeting is explicitly required
- Enable nullable reference types and treat warnings as errors:

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

- Use `Directory.Build.props` at the repo root to centralise shared properties (framework version, nullable, warnings, common package versions)
- Use `Directory.Packages.props` with Central Package Management to pin NuGet versions in one place
- Avoid `<ProjectReference>` to test projects from source projects

## Namespace Conventions

- Root namespace should match the project name
- Use file-scoped namespaces (`namespace X;`)
- Organise namespaces by feature or domain concept, not by technical pattern (avoid `Models/`, `Services/`, `Repositories/` folders in favour of feature folders)

## Configuration & Settings

- Store environment-specific configuration in `appsettings.{Environment}.json`
- Use the options pattern (`IOptions<T>`) for strongly-typed settings
- Never hard-code connection strings, secrets, or environment-specific values
- Use user secrets (`dotnet user-secrets`) for local development credentials

## Test Project Structure

- Name test classes `<ClassUnderTest>Tests` (e.g., `OrderServiceTests`)
- Group tests in the same namespace hierarchy as the code they exercise
- Separate unit and integration tests into distinct projects when build/run time matters
- Reference only the project under test — avoid referencing sibling source projects unless testing integration points

## Solution Filters

- Use `.slnf` solution filter files when the solution is large and developers routinely work on a subset
- Keep filters checked into source control alongside the `.sln`

## Build & CI

- Ensure `dotnet build` and `dotnet test` succeed from the repo root with no extra setup
- Place shared MSBuild props/targets in `build/` or the repo root (`Directory.Build.props`, `Directory.Build.targets`)
- Avoid solution-level NuGet packages — manage dependencies at the project level via Central Package Management
