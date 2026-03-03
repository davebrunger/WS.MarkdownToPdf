---
name: csharp
description: C# coding guidance covering language features, code style, naming conventions, async patterns, error handling, dependency injection, LINQ, testing with xUnit, project structure, and documentation standards for .NET 8+ projects.
---

# C# Guidance

## Language & Framework

- Target .NET 10+ and use the latest C# language features
- Use top-level statements for simple console apps; use `Program.cs` with minimal hosting for services
- Enable nullable reference types (`<Nullable>enable</Nullable>`) and treat warnings as errors

## Code Style

- Use file-scoped namespaces (`namespace X;`)
- Prefer `var` when the type is obvious from the right-hand side
- Do not use primary constructors
- Prefer expression-bodied members for single-line methods and properties
- Use pattern matching (`is`, `switch` expressions) over type-checking with casts
- Prefer collection expressions (`[1, 2, 3]`) over explicit `new List<int> { ... }` where supported
- Use immutable types and avoid mutable state where possible
- Use `record` types for immutable DTOs and value objects
- Prefer `readonly` fields and `init` properties to communicate immutability

## Naming Conventions

- **PascalCase** for public members, types, namespaces, methods and properties of anonymous types
- **camelCase** for local variables and parameters
- **camelCase** (no underscore prefix) for private fields
- **I** prefix for interfaces (`IRepository`, `IService`)
- **Async** suffix for async methods (`GetOrderAsync`)
- Avoid Hungarian notation and abbreviations

## Async / Await

- Prefer `async`/`await` over `Task.ContinueWith` or `Task.Result`
- Never use `.Result` or `.Wait()` on tasks — it risks deadlocks
- Use `ValueTask` when the result is frequently available synchronously
- Pass and honour `CancellationToken` wherever possible
- Suffix async methods with `Async`

## Error Handling

- Throw specific exception types; avoid throwing `System.Exception` directly
- Use guard clauses (`ArgumentNullException.ThrowIfNull`) for parameter validation
- Prefer result/outcome objects or `OneOf` over exceptions for expected failures
- Never swallow exceptions silently — at minimum, log them
- Use `finally` or `using`/`await using` for resource cleanup

## Dependency Injection

- Register services in `Program.cs` or dedicated extension methods (`AddXxxServices`)
- Prefer constructor injection; avoid service locator patterns
- Use `IOptions<T>` / `IOptionsSnapshot<T>` for configuration binding
- Keep service lifetimes (Singleton, Scoped, Transient) intentional and documented

## LINQ & Collections

- Prefer LINQ method syntax for queries;
- Avoid multiple enumerations — materialise with `.ToList()` or `.ToArray()` when needed
- Use `IReadOnlyList<T>` or `IReadOnlyCollection<T>` for return types that shouldn't be mutated
- Prefer `Span<T>` / `Memory<T>` for performance-sensitive buffer work

## Documentation

- Use XML doc comments (`///`) on all public APIs
- Keep comments focused on *why*, not *what* — the code should be self-explanatory
- Avoid commented-out code in source control
