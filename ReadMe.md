# 設計
## クラス図
```mermaid
classDiagram
    class GameManager {
        - IGameModeStrategy gameMode
        - GameState gameState
        + OnGameOver event
    }
    class GameState {
        + float Oxygen
        + int Score
    }
    class IGameModeStrategy {
        <<Interface>>
        + IsGameOver(GameState)
    }
    class SinglePlayGameMode {
        + IsGameOver(GameState)
    }
    class MultiPlayGameMode {
        + IsGameOver(GameState)
    }

    class PlayerFacade {
        - PlayerInput input
        - PlayerStateMachine stateMachine
        + CmdInteract(Vector3Int)
    }
    class PlayerInput {
        + OnInteract event
    }
    class PlayerStateMachine {
        - IPlayerState currentState
        + ChangeState(IPlayerState)
    }
    class IPlayerState {
        <<Interface>>
        + Enter()
        + Execute()
        + Exit()
    }
    class RoamingState { ... }
    class TypingState { ... }

    class LevelGenerator {
        + RpcDestroyBlock(Vector3Int)
    }
    class ItemService {
        + CmdAcquireItem(ItemData)
    }
    
    class OxygenView {
        + OnGameStateChanged(GameState)
    }
    
    GameManager o-- GameState
    GameManager o-- IGameModeStrategy
    IGameModeStrategy <|.. SinglePlayGameMode
    IGameModeStrategy <|.. MultiPlayGameMode
    
    PlayerFacade o-- PlayerInput
    PlayerFacade o-- PlayerStateMachine
    PlayerStateMachine o-- IPlayerState
    IPlayerState <|.. RoamingState
    IPlayerState <|.. TypingState

    PlayerFacade --> LevelGenerator : "破壊依頼(Cmd)"
    PlayerFacade --> ItemService : "取得報告(Cmd)"
    
    GameManager --> OxygenView : "イベント通知"
```
## 全体設計
```mermaid
graph TD
    subgraph "Application Scope(DontDestroyOnLoad)"
        AppManager("AppManager<br/>(シーン管理, サウンド管理)")
    end
    
    subgraph "MainMenu Scene"
        MainMenuManager("MainMenuManager")
    end

    subgraph "Game Scene"
        GameManager("GameManager")
    end
    
    subgraph "Lobby Scene"
        LobbyManager("LobbyManager")
    end

    AppManager --> MainMenuManager
    AppManager --> GameManager
    AppManager --> LobbyManager

    MainMenuManager -- "ゲーム開始" --> AppManager
    GameManager -- "メニューへ戻る" --> AppManager
```

# フォルダ戦略
```
Assets
├── 📂_Project                  # ✅ プロジェクト固有のアセットは全てここに
│   ├── 📂Features              # ⭐ プロジェクトの心臓部。機能単位で管理
│   │   ├── 📂Core                # アプリケーション全体の基盤機能
│   │   │   ├── 📂SceneManagement # シーン遷移
│   │   │   ├── 📂Sound           # サウンド管理
│   │   │   └── 📂DI              # DIコンテナの設定など
│   │   │
│   │   ├── 📂Game                # ゲームプレイ中の機能
│   │   │   ├── 📂Player
│   │   │   │   ├── 📂Prefabs
│   │   │   │   ├── 📂Scripts
│   │   │   │   └── 📂Animations
│   │   │   ├── 📂Items           # 💎 アイテム関連はここに集約
│   │   │   │   ├── 📂Data          # ScriptableObjectのアセット置き場
│   │   │   │   ├── 📂Icons         # アイテムのアイコン
│   │   │   │   ├── 📂Scripts
│   │   │   │   │   ├── Data        # ItemData.cs など定義クラス
│   │   │   │   │   └── Effects     # IItemEffect.cs や具体的な効果クラス
│   │   │   │   └── 📂Tiles         # タイルマップ用のアイテムタイル
│   │   │   ├── 📂Level
│   │   │   └── 📂Typing
│   │   │
│   │   ├── 📂Networking          # PlayFab, Mirror関連のスクリプト
│   │   └── 📂UI                  # UI関連
│   │       ├── 📂Screens         # 画面ごとのUIプレハブやスクリプト
│   │       │   ├── 📂MainMenu
│   │       │   ├── 📂Lobby
│   │       │   └── 📂InGameHUD
│   │       └── 📂Common          # 画面をまたいで使う共通UI部品
│   │           ├── 📂Buttons
│   │           └── 📂Fonts
│   │
│   ├── 📂Scenes                  # シーンファイル
│   │   ├── Title.unity
│   │   └── Game.unity
│   │
│   └── 📂Settings                # ゲーム全体の設定用ScriptableObject
│       ├── ItemRegistry.asset      # アイテムDBの実体
│       └── GameDBSettings.asset
│
├── 📂ExternalAssets            # Asset Storeなど外部アセット
└── 📂Editor                    # Editor拡張スクリプト
```

# 参考文献
    - マルチプレイ
        - あのゲームの作り方Web版
            - NetCode for GameObject
            - Game Server Hosting(Unity)