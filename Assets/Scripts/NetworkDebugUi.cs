using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkDebugUi : MonoBehaviour
{
    public static NetworkDebugUi Instance;

    //Reloads scene when pressed so users should be able to swap from lan to online and etc / restart lobbies without reloading the game.
    [SerializeField] private Button m_lanOptionButton;
    [SerializeField] private Button m_steamOptionButton;
    [SerializeField] private Button m_hostStartButton;
    [SerializeField] private Button m_clientStartButton;
    
    //for now probably just make it go back to a main menu scene and reload the network manager class / check that it unloads and leaves the lobby correctly for both host and client, 
    //dont forget to fix when host leaves for the client to be booted atm it softlocks them.
    //[SerializeField] private Button m_leaveLobbyButton;
    
    //[SerializeField] private TextMeshProUGUI m_isHostOrClientText;
    //[SerializeField] private TextMeshProUGUI m_amountOfPlayersText;
    //[SerializeField] private TextMeshProUGUI m_pingText;
    //[SerializeField] private TextMeshProUGUI m_steamUsersConnected;
    //[SerializeField] private TextMeshProUGUI m_fpsText;
    //[SerializeField] private TextMeshProUGUI m_averagePingOfClients;
    //amount of clients, ishost or isclient, display each steam user in steam mode, ping/latency, fps.

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        m_hostStartButton.onClick.AddListener(() =>
        {
            if (GameNetworkManager.Instance.m_isUsingSteamNetworking)
                GameNetworkManager.Instance.StartHosting();
            else
                NetworkManager.Singleton.StartHost();
        });
        m_clientStartButton.onClick.AddListener(() =>
        {
            if (GameNetworkManager.Instance.m_isUsingSteamNetworking)
                return;
            
            NetworkManager.Singleton.StartClient();
        });
        
        m_lanOptionButton.onClick.AddListener(SwapToLan);
        m_steamOptionButton.onClick.AddListener(SwapToSteam);
    }

    private void SwapToSteam()
    {
        m_clientStartButton.gameObject.SetActive(false);
        GameNetworkManager.Instance.SwapToSteamTransport();
    }

    private void SwapToLan()
    {
        m_clientStartButton.gameObject.SetActive(true);
        GameNetworkManager.Instance.SwapToLanTransport();
    }

    private void SetActiveDebugWindow()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
