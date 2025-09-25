# **データ管理と永続化 設計ドキュメント**

## **1. 責務と目的**

このドキュメントは、ゲーム内で使用される各種データ（ゲームバランス、設定、プレイヤーの進捗など）をどのように管理し、保存・読み込み（永続化）するかの戦略を定義します。

**目的:**

* **データとロジックの分離**: ゲームバランスなどの調整を、コードの変更なしに安全に行えるようにする。  
* **堅牢なデータ保存**: プレイヤーの設定や進捗を、Unity Gaming Services (UGS) を利用して安全にクラウド上に保存する。  
* **明確な管理方針**: どのデータがScriptableObjectで管理され、どのデータがCloud Saveの対象となるかを明確に定義する。

## **2. 管理するデータの分類**

データは、その性質に応じて以下の2種類に大別します。

### **2.1. 静的データ (Static Data)**

* **定義**: ゲームのバージョンが変わらない限り、基本的には変化しない設定値やデータベース。  
* **例**: アイテムの性能、マップ生成のパラメータ、ゲームの基本ルール（酸素減少率など）。  
* **管理方法**: **ScriptableObject** を全面的に採用します。これらのアセットは、それが関連する機能のフォルダ（例: `GameConfig`は`Features/Game/Settings/`、`PlayerDefaultStats`は`Features/Core/PlayerStatus/`）に配置され、`GameSceneBootstrapper`などを通じて各システムに注入されます。

### **2.2. 動的データ / 永続化データ (Dynamic / Persistent Data)**

* **定義**: プレイヤーの行動によって変化し、ゲームセッションをまたいで保持されるべき、プレイヤー固有のデータ。  
* **例**: キーコンフィグ、音量設定、シングルプレイのハイスコア、ローグライクモードの進行状況。  
* **管理方法**: **Unity Gaming Services (UGS)** の各種サービスを主軸とします。

## **3. 詳細設計：静的データの管理戦略**

ScriptableObjectで作成される多数の設定アセットへのアクセスを単純化するため、**設定のハブ**となるGameConfigアセットを導入します。

#### **3.1. GameConfig.cs (ScriptableObject)**

このScriptableObjectは、他の全ての設定ScriptableObjectへの参照を保持します。

[CreateAssetMenu(fileName = "GameConfig", menuName = "Settings/Game Configuration")]  
public class GameConfig : ScriptableObject  
{  
    public GameRuleSettings RuleSettings;  
    public PlayerDefaultStats PlayerStats;  
    public ItemRegistry ItemRegistry;  
    // ... 他の全体設定アセット ...  
}

#### **3.2. MapTheme.cs (ScriptableObject)**
マップ生成の静的データを管理するScriptableObjectです。`MapGenerator`が生成した抽象的な値（例: ノイズ値）と、実際にシーンに配置されるタイルPrefabとの対応関係を定義します。

これにより、レベルの「見た目」や「テーマ」（例: 洞窟、森、遺跡）をコードから完全に分離し、デザイナーがUnityエディタ上で直感的にレベルデザインを行えるようになります。

## **4. 詳細設計：動的データの永続化戦略**

### **4.1. PlayerSaveData.cs (データ構造)**

UGSのCloud Saveで保存・読み込みするプレイヤーデータの具体的な「形」を定義する、シリアライズ可能なクラスです。
```csharp
[System.Serializable]  
public class PlayerSaveData  
{  
    public int SaveVersion = 1; // データ構造の変更に対応するためのバージョン番号

    public PlayerSettingsData Settings;  
    public PlayerProgressData Progress;  
}

[System.Serializable]  
public class PlayerSettingsData { /* 音量、キーコンフィグなど */ }

[System.Serializable]  
public class PlayerProgressData { /* ハイスコア、アンロック状況など */ }
```

**関連ドキュメント:**
* [README.md](./README.md)
* [./Architecture-Overview.md](./Architecture-Overview.md)  
* [./Features/Typing/Typing-Design.md](./Features/Game/Typing/Typing-Design.md)