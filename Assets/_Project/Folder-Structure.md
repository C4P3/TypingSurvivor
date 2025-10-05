# **フォルダ構造**

このプロジェクトでは、**機能別（Feature-based）アプローチ**を全面的に採用します。これは、アセットの種類（Scripts, Prefabs, Materials...）で分けるのではなく、ゲームの機能（Player, Items, UI...）で関連ファイルをまとめるアプローチです。

## **1\. 設計思想**

* **高い凝集度**: ある機能に関連する全てのファイル（スクリプト、プレハブ、データアセット等）が、一つのフォルダに集約されます。これにより、機能の追加・修正・削除がそのフォルダ内で完結し、プロジェクトの見通しが格段に良くなります。  
* **低い結合度**: 各機能フォルダは、他の機能フォルダの内部構造を知ることなく、定義されたインターフェースを通じてのみ連携します。これにより、機能の独立性が保たれます。  
* **責務の明確化**: フォルダ構造そのものが、アプリケーションの設計思想を反映します。新しいファイルを追加する際、どこに置くべきかが直感的に判断できます。

## **2\. ルート構造**

Assets/  
├── 📂\_Project/      \# ✅ このゲーム固有のアセットは全てここに格納する  
├── 📂ExternalAssets/  \# Asset Storeなど、外部から導入したアセット  
└── 📂Editor/        \# Editor拡張用のスクリプト

## 3. _Projectフォルダ詳細構造

_Project/  
├── 📂Features/         # ⭐ プロジェクトの心臓部。全機能はこの中に定義される  
│  
├── 📂Scenes/           # シーンファイル (.unity)  
│   ├── App.unity
│   ├── MainMenu.unity  
│   └── Game.unity  
│  
└── 📂Settings/         # ゲーム全体で共有する設定用ScriptableObjectアセットを格納します。
    # 例: GameConfig.asset, AudioRegistry.asset, VFXRegistry.assetなど

## **4\. Featuresフォルダ詳細構造**

各機能フォルダには `.asmdef` ファイルが配置され、アセンブリとして分割されています。

Features/
├── 📂Core/                 # アプリケーションの生存期間全体に影響する、横断的な機能
│   ├── 📂App/
│   ├── 📂Audio/
│   ├── 📂Auth/
│   ├── 📂CloudSave/
│   ├── 📂Map/
│   ├── 📂Matchmaking/
│   ├── 📂PlayerStatus/
│   ├── 📂SceneManagement/
│   ├── 📂Sound/
│   └── 📂VFX/
│
├── 📂Game/                 # ゲームプレイシーンに特化した機能群
│   ├── 📂_Bootstrap/
│   ├── 📂Camera/
│   ├── 📂Gameplay/
│   ├── 📂Item/
│   ├── 📂Leaderboard/
│   ├── 📂Level/
│   ├── 📂Player/
│   ├── 📂Rating/
│   ├── 📂Scripts/
│   ├── 📂Settings/
│   └── 📂Typing/
│
└── 📂UI/                   # UI機能
    ├── 📂Common/             # 画面をまたいで使われる共通部品 (ボタン、ゲージなど)
    └── 📂Screens/            # 画面単位のプレハブと管理スクリプト
        ├── 📂InGameHUD/
        └── 📂MainMenu/

### **全体のドキュメント:**　
[README.md](./README.md)
### **次のドキュメント:**
[Data-Flow.md](./Data-Flow.md)