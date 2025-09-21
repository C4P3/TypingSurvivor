using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;
using System;

using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        clientButton.interactable = false;
        clientButton.onClick.AddListener(() =>
        {
            // マッチングを開始する処理を記載する
            CreateTicket();
            clientButton.interactable = false;
        });
    }

    async private void Start()
    {
        bool isClient = true;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-dedicatedServer")
            {
                isClient = false;
            }
        }
        if (isClient)
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            clientButton.interactable = true;
        }
    }

    private async void CreateTicket()
    {
        var options = new CreateTicketOptions("FreeMatch-1vs1");

        var players = new List<Player>
        {
            new Player(AuthenticationService.Instance.PlayerId,
                new MatchmakingPlayerData
                {
                    skill = 100
                })
        };

        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        string ticketID = ticketResponse.Id;

        Debug.Log($"Created ticket {ticketID}");

        PollTicketStatus(ticketID);
    }

    private async void PollTicketStatus(string ticketID)
    {
        MultiplayAssignment multiplayAssignment = null;
        bool isAssigning = true;
        bool isFound = false;
        do
        {
            await Task.Delay(1000);
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketID);
            if (ticketStatus == null)
            {
                Debug.Log("Ticket not found");
                continue;
            }
            if (ticketStatus.Type == typeof(MultiplayAssignment))
            {
                multiplayAssignment = ticketStatus.Value as MultiplayAssignment;

                switch (multiplayAssignment.Status)
                {
                    case StatusOptions.Found:
                        Debug.Log("Found match");
                        isAssigning = false;
                        isFound = true;
                        TicketAssigned(multiplayAssignment);
                        break;
                    case StatusOptions.InProgress:
                        break;
                    case StatusOptions.Failed:
                        isAssigning = false;
                        Debug.Log($"Failed to get ticket status. Error:{multiplayAssignment.Message}");
                        break;
                    case StatusOptions.Timeout:
                        isAssigning = false;
                        Debug.Log($"Ticket timed out. Error:{multiplayAssignment.Message}");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
        while (isAssigning);

        clientButton.interactable = !isFound;
    }
    private void TicketAssigned(MultiplayAssignment multiplayAssignment)
    {
        Debug.Log($"Ticket assigned:{multiplayAssignment.Ip}:{multiplayAssignment.Port}");
        NetworkManager.Singleton.GetComponent<UnityTransport>().
        SetConnectionData(
            multiplayAssignment.Ip,
            (ushort)multiplayAssignment.Port);

        NetworkManager.Singleton.StartClient();
    }

    [Serializable]
    public class MatchmakingPlayerData
    {
        public int skill;
    }
}