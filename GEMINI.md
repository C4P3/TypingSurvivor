# GEMINI Development Guidelines

This document outlines the core principles and architectural patterns that I, Gemini, will adhere to when assisting with the development of the "Typing Survivor" project. My purpose is to generate and modify code in a way that is consistent with the existing design philosophy, ensuring the project remains robust, scalable, and easy to maintain.

## 1. Core Principles

I will strictly follow these two foundational principles in all my contributions:

-   **Loose Coupling**: I will design components so that they have minimal knowledge of each other. Communication will be handled through established interfaces (e.g., `IItemService`, `IGameStateReader`) and events, never through direct class references across feature boundaries.
-   **Separation of Concerns**: I will ensure that each class has a single, well-defined responsibility. For example, player input, state management, and network synchronization will be handled by separate, specialized classes within the `Player` feature.

## 2. Architecture: Server-Authoritative Model

I will operate under the assumption that the server has ultimate authority over the game state.

-   **Client's Role**: Client-side code I generate will focus on capturing user input and reflecting the server's state. It will not contain gameplay logic that could lead to desynchronization.
-   **Server's Role**: All critical game logic (e.g., destroying a block, applying an item effect, calculating score) will be executed on the server.
-   **Communication**: I will use `[ServerRpc]` to send requests from client to server and rely on `NetworkVariable` synchronization and `[ClientRpc]` calls to update clients.

## 3. Design Patterns

I will actively use the established design patterns to promote consistency and extensibility:

-   **Strategy Pattern**: For systems with interchangeable algorithms (e.g., item effects, map generation), I will create new functionality by implementing the appropriate strategy interface (`IItemEffect`, `IMapGenerator`).
-   **Facade Pattern**: I will use the Facade pattern (e.g., `PlayerFacade`) as the primary entry point for inter-feature communication, hiding the internal complexity of a feature.
-   **Dependency Injection (DI)**: I will write classes that declare their dependencies via interfaces, assuming they will be injected from an external source, rather than using singletons like `GameManager.Instance`.

## 4. Folder Structure

I will respect the feature-based folder structure. When adding new functionality for an existing feature (e.g., a new "Bomb" item), I will place all related assets (scripts, prefabs, ScriptableObjects) within the corresponding feature folder (`Features/Game/Items/Bomb/`).

## 5. Data Management

-   **Static Data**: For game balance and configuration, I will use `ScriptableObject` assets and register them in a central hub like `GameConfig` or `ItemRegistry`.
-   **Dynamic Data**: For player-specific data that needs to be saved, I will use serializable classes like `PlayerSaveData` intended for use with Unity Gaming Services.

## 6. Operational Protocol: Staying Up-to-Date

-   **Source of Truth**: The project's official documentation (e.g., `Architecture-Overview.md`, feature-specific designs) is the single source of truth. This `GEMINI.md` is my sworn constitution to follow it.
-   **Pre-Task Review**: Before beginning any new development task, I will re-read the relevant design documents to ensure my understanding is current.
-   **Adaptation**: If I detect any conflict between these guidelines and the project's documentation, I will prioritize the project's documentation and, if necessary, propose an update to this `GEMINI.md` file to reflect the latest design decisions.

By following these guidelines, I will ensure that my contributions seamlessly integrate with your project's architecture. I am now ready to assist with development.