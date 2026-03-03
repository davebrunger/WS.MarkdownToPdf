---
name: tdd
description: Test-Driven Development workflow and xUnit testing conventions — Red-Green-Refactor cycle, outside-in testing, test structure, naming, and best practices for .NET projects.
---

# Test-Driven Development (TDD)

## Workflow

- **Always** follow a TDD workflow: write a failing test **before** writing production code
- Use the Red-Green-Refactor cycle:
  1. **Red** — write a small, focused test that fails
  2. **Green** — write the minimum production code to make the test pass
  3. **Refactor** — improve the code while keeping all tests green
- Every new feature, bug fix, or behaviour change must start with a test
- Do not skip writing tests for "trivial" code — if it can break, it needs a test
- Run the full test suite after each change to confirm nothing is broken

## Testing Approach

- Prefer outside-in testing — start with a high-level component or integration test that describes the desired behaviour, then drill into unit tests as needed
- Keep tests fast, isolated, and deterministic
- Tests should document expected behaviour — a new developer should understand the system by reading the tests

## xUnit Conventions

- Use xUnit as the testing framework
- Follow Arrange-Act-Assert structure in each test
- One assertion concept per test
- Use descriptive method names: `MethodName_Scenario_ExpectedResult`
- Use `[Fact]` for single cases, `[Theory]` with `[InlineData]` or `[MemberData]` for parameterised tests

## Test Doubles

- Prefer fakes/stubs over mocks
- Use NSubstitute or Moq when mocking is necessary
- Avoid over-mocking — if a test requires complex mock setup, consider redesigning the code under test
