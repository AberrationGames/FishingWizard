using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
public class NetworkDebugUi : MonoBehaviour
{
    public static NetworkDebugUi Instance;

    [SerializeField] private NetworkTransport m_facePunchTransport;
    [SerializeField] private NetworkTransport m_unityLocalTransport;
    
    //Reloads scene when pressed so users should be able to swap from lan to online and etc / restart lobbies without reloading the game.
    [SerializeField] private Button m_lanOptionButton;
    [SerializeField] private Button m_steamOptionButton;
    [SerializeField] private Button m_hostStartButton;
    [SerializeField] private Button m_clientStartButton;
    //for now probably just make it go back to a main menu scene and reload the network manager class / check that it unloads and leaves the lobby correctly for both host and client, 
    //dont forget to fix when host leaves for the client to be booted atm it softlocks them.
    [SerializeField] private Button m_leaveLobbyButton;

    [SerializeField] private TextMeshProUGUI m_isHostOrClientText;
    [SerializeField] private TextMeshProUGUI m_amountOfPlayersText;
    [SerializeField] private TextMeshProUGUI m_pingText;
    [SerializeField] private TextMeshProUGUI m_steamUsersConnected;
    [SerializeField] private TextMeshProUGUI m_fpsText;
    [SerializeField] private TextMeshProUGUI m_averagePingOfClients;
    //amount of clients, ishost or isclient, display each steam user in steam mode, ping/latency, fps.

    private bool m_isSteam = true;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        m_isSteam = GameNetworkManager.Instance.m_isUsingSteamNetworking;
        
        m_hostStartButton.onClick.AddListener(() =>
        {
            if (m_isSteam)
                GameNetworkManager.Instance.StartHost(8);
            else
                NetworkManager.Singleton.StartHost();
        });
        m_clientStartButton.onClick.AddListener(() =>
        {
            if (m_isSteam)
                return;
            
            NetworkManager.Singleton.StartClient();
        });
        m_lanOptionButton.onClick.AddListener(SwapToLan);
        m_steamOptionButton.onClick.AddListener(SwapToSteam);
    }

    private void SwapToSteam()
    {
        m_isSteam = true;
        m_clientStartButton.gameObject.SetActive(false);
    }

    private void SwapToLan()
    {
        m_isSteam = false;
        m_clientStartButton.gameObject.SetActive(true);
    }

    private void SetActiveDebugWindow()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
