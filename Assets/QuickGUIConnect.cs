using UnityEngine;
using Unity.Netcode; // Netcode for GameObjects を使用

public class QuickGUIConnect : MonoBehaviour
{
    // OnGUI は、GUIイベントが処理されるたびに呼び出されます
    void OnGUI()
    {
        // 画面の左上から10ピクセル離れた位置に、幅100、高さ30のボタンを配置します
        if (GUI.Button(new Rect(10, 10, 100, 30), "Join as Client"))
        {
            // ボタンがクリックされたら、クライアントとして接続を開始します
            NetworkManager.Singleton.StartClient();
        }

        // もう一つ、ホストとして開始するボタンも作ってみましょう
        if (GUI.Button(new Rect(10, 50, 100, 30), "Start Host"))
        {
            // ホスト（サーバー兼クライアント）として接続を開始します
            NetworkManager.Singleton.StartServer();
        }
    }
}