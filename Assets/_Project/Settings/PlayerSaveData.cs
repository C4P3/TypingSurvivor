// PlayerSaveData.cs
using System.Collections.Generic;

[System.Serializable] // JSONにシリアライズ可能にするため
public class PlayerSaveData
{
    public int SaveVersion = 1; // ★将来のデータ構造変更に対応するためのバージョン番号

    public PlayerSettingsData Settings;
    public PlayerProgressData Progress;

    public PlayerSaveData()
    {
        Settings = new PlayerSettingsData();
        Progress = new PlayerProgressData();
    }
}

[System.Serializable]
public class PlayerSettingsData
{
    public float MasterVolume = 1.0f;
    public float BgmVolume = 0.8f;
    public float SeVolume = 0.8f;
    public Dictionary<string, string> KeyBindings; // (例: "Interact" -> "LeftShift")
}

[System.Serializable]
public class PlayerProgressData
{
    public int SinglePlayHighScore = 0;
    public List<string> UnlockedItemIds;
}