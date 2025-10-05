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
* **管理方法**: **ScriptableObject** を全面的に採用します。これらのアセットは、それが関連する機能のフォルダ（例: `GameConfig`は`Features/Game/Settings/`、`PlayerDefaultStats`は`Features/Core/PlayerStatus/`）に配置され、`PlayerStatusSystem`や`GameSceneBootstrapper`などを通じて各システムに注入されます。

### **2.2. 動的データ / 永続化データ (Dynamic / Persistent Data)**

* **定義**: プレイヤーの行動によって変化し、ゲームセッションをまたいで保持されるべきデータ。
* **管理方法**: データの性質に応じて、以下のUnity Gaming Services (UGS)を使い分けます。
    *   **Unity Leaderboards**: プレイヤーのスコアやランキングなど、競争力のあるデータ。サーバーから権威をもって送信されます。
    *   **Cloud Save**: キーコンフィグ、音量設定、チュートリアルの進捗など、プレイヤー個人の設定項目。これらは主にクライアントから直接保存・読み込みされます。

## **3. 詳細設計：静的データの管理戦略**

静的データの管理は、Unityエディタとの親和性が高い **ScriptableObject** を基本としますが、データの性質に応じて外部のテキストファイル（CSV, JSON）を併用するハイブリッドアプローチを採用します。

#### **3.1. 基本方針: ScriptableObject**
アイテムの性能やゲームルールなど、主にUnityエディタ内で完結して設定できるデータは、ScriptableObjectとして作成します。

#### **3.2. 大規模データ: 外部ファイル (CSV/JSON) + ScriptableObjectラッパー**
タイピングの単語リストや、言語ごとの変換ルールのように、データ量が膨大になる可能性があり、かつ非プログラマーが表計算ソフトなどで編集する方が効率的なデータは、CSVやJSONファイルとして管理します。

これらの外部ファイルを直接コードからパスで読み込むのではなく、**ScriptableObjectをラッパーとして利用**します。

*   **`TypingConversionTableSO.cs`の例**:
    *   このScriptableObjectは、`TextAsset`としてJSONファイルへの参照をインスペクターから設定できます。
    *   実行時にJSONの内容を読み込み、C#オブジェクト（`TypingConversionTable`）としてキャッシュします。
*   **`GameConfig.cs`の例**:
    *   `GameConfig`は、`TextAsset`として単語リストのCSVファイルへの参照や、上記`TypingConversionTableSO`アセットへの参照を保持します。

この「**外部ファイル + SOラッパー**」パターンにより、外部ファイルの編集のしやすさと、ScriptableObjectによるUnityワークフローへの統合（ドラッグ＆ドロップでの設定、依存性注入の容易さ）という両方の利点を享受できます。

#### **3.3. 設定のハブ: GameConfig.cs**
`GameConfig`アセットは、これらの各種設定アセット（ScriptableObjectやTextAsset）への参照を集約する「ハブ」として機能し、`GameSceneBootstrapper`を通じて各システムに注入されます。

[CreateAssetMenu(fileName = "GameConfig", menuName = "Settings/Game Configuration")]  
public class GameConfig : ScriptableObject  
{  
    public GameRuleSettings RuleSettings;  
    public PlayerDefaultStats PlayerStats;  
    public ItemRegistry ItemRegistry;
    public AudioRegistry AudioRegistry;
    public VFXRegistry VFXRegistry;
    public TextAsset WordListCsv;
    public List<LanguageTableMapping> LanguageTables;
    // ... 他の全体設定アセット ...  
}

## **4. 詳細設計：動的データの永続化戦略**

### **4.1. PlayerSaveData.cs (データ構造)**

UGSのCloud Saveで保存・読み込みするプレイヤーデータの具体的な「形」を定義する、シリアライズ可能なクラスです。
```csharp
[System.Serializable]  
public class PlayerSaveData  
{  
    public string PlayerName; // For UI display
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