using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TypingSurvivor.Features.Core.App; // AppManagerを参照するために追加

public class ServerStartup : MonoBehaviour
{
    private void Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        bool isDedicatedServer = false;
        ushort serverPort = 7777;
        string externalServerIP = "127.0.0.1";
        string gameMode = "MultiPlayer"; // Default game mode

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-dedicatedServer")
            {
                isDedicatedServer = true;
            }
            else if (args[i] == "-port" && i + 1 < args.Length)
            {
                serverPort = ushort.Parse(args[i + 1]);
            }
            else if (args[i] == "-ip" && i + 1 < args.Length)
            {
                externalServerIP = args[i + 1];
            }
            else if (args[i] == "-gameMode" && i + 1 < args.Length)
            {
                gameMode = args[i + 1];
            }
        }

        if (isDedicatedServer)
        {
            // Set the game mode for the server
            AppManager.GameMode = gameMode;
            
            // Start the server
            StartDedicatedServer(externalServerIP, serverPort);

            // Load the game scene directly
            SceneManager.LoadScene("Game");
        }
    }

    private void StartDedicatedServer(string ip, ushort port)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
        NetworkManager.Singleton.StartServer();
    }
}