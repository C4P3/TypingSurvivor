using UnityEngine;
using Unity.Netcode; // Netcode for GameObjects を使用

public class QuickGUIConnect : MonoBehaviour
{
    // OnGUI は、GUIイベントが処理されるたびに呼び出されます
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Join as Client"))
        {
            NetworkManager.Singleton.StartClient();
        }

        if (GUI.Button(new Rect(10, 50, 100, 30), "Start Host"))
        {
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(10, 90, 100, 30), "Start Server"))
        {
            NetworkManager.Singleton.StartServer();
        }
    }
}