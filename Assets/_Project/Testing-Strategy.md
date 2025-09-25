# **Testing Strategy**

This document outlines the strategy and conventions for writing automated tests in the Typing Survivor project.

## 1. Core Principles

-   **Test for Confidence**: We write tests to be confident in our code's correctness and to prevent future regressions. A good test should fail if the logic is broken and pass if it's correct.
-   **Clarity and Readability**: Tests should be easy to understand. They serve as living documentation for the features they cover.
-   **Focus on Business Logic**: Prioritize testing the core game logic (e.g., typing conversion, item effects, game rules) over testing Unity's engine features.

## 2. Framework

We will use the official **Unity Test Framework** package for all automated testing.

## 3. Test Types & Folder Structure

Tests are categorized into two types, located in the `Assets/_Project/Tests` directory.

### 3.1. Editor Tests

-   **Purpose**: For testing pure C# classes and logic that do not depend on `MonoBehaviour` or the game loop (e.g., services, data structures, utility classes).
-   **Location**: `Assets/_Project/Tests/Editor/`
-   **Characteristics**:
    -   Run directly in the Unity Editor.
    -   Extremely fast execution.
    -   Use the NUnit framework's attributes like `[Test]`, `[TestCase]`, and `Assert`.
-   **Example**: Testing the `TrieBuilder` to ensure it correctly converts "きゃ" to both "kya" and "kixya".

### 3.2. Play Mode Tests

-   **Purpose**: For testing `MonoBehaviour` components, component interactions, and anything that requires the game to be running.
-   **Location**: `Assets/_Project/Tests/PlayMode/`
-   **Characteristics**:
    -   Run in a separate scene in Play Mode.
    -   Slower than Editor tests.
    -   Use `[UnityTest]` attribute and can run as coroutines (`yield return null`).
-   **Example**: Testing if a player `MonoBehaviour` correctly moves from one grid cell to another over time.

## 4. Naming Conventions

-   **Test Files**: `[ClassName]Tests.cs` (e.g., `TrieBuilderTests.cs`)
-   **Test Methods**: `[MethodName]_[Condition]_[ExpectedResult]`
    -   **Example**: `Build_WithSokuonAndNextConsonant_DuplicatesConsonant`

## 5. Assembly Definitions (.asmdef)

-   Each test folder (`Editor`, `PlayMode`) will have its own Assembly Definition file.
-   These test assemblies will reference the main game code assemblies (e.g., `TypingSurvivor.Features.Game.Typing`) to access the code they need to test.
-   To test `internal` classes or methods, the game code's `.asmdef` file will be modified to include an `[InternalsVisibleTo]` attribute pointing to the test assembly.

## 6. Getting Started

1.  Open the `Test Runner` window (`Window > General > Test Runner`).
2.  Select either the `PlayMode` or `EditMode` tab.
3.  Click `Run All` to execute all tests in that category.
