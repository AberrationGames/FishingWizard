using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHostSettingsMenu : MonoBehaviour
{
    [SerializeField] private Button m_publicLobbyToggle;
    [SerializeField] private Button m_hostLobbyButton;
    [SerializeField] private TMP_InputField m_lobbyDescriptionInput;
    [SerializeField] private TMP_InputField m_lobbyMaxSizeInput;

    [SerializeField] private TextMeshProUGUI m_publicLobbyToggleText;
    void Start()
    {
        m_publicLobbyToggleText.text = GameNetworkManager.Instance.m_localLobbySettings.m_isPublicLobby ? "Public Lobby" : "Private/Friends Lobby";
        m_publicLobbyToggle.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.m_localLobbySettings.m_isPublicLobby = !GameNetworkManager.Instance.m_localLobbySettings.m_isPublicLobby;
            m_publicLobbyToggleText.text = GameNetworkManager.Instance.m_localLobbySettings.m_isPublicLobby ? "Public Lobby" : "Private/Friends Lobby";
        });
        m_lobbyDescriptionInput.onValueChanged.AddListener((string a_newValue) =>
        {
            GameNetworkManager.Instance.m_localLobbySettings.m_lobbyDescription = a_newValue;
        });
        m_lobbyMaxSizeInput.onValueChanged.AddListener((string a_newValue) =>
        {
            GameNetworkManager.Instance.m_localLobbySettings.m_lobbyMaxPlayers = int.Parse(a_newValue);
        });
        
        m_hostLobbyButton.onClick.AddListener(() =>
        {
            GameManager.Instance.m_currentLobbyType = GameNetworkManager.Instance.m_isUsingSteamNetworking ? GameLobbyType.OnlineMultiplayer : GameLobbyType.LocalAreaNetworkMultiplayer;
            GameManager.Instance.LoadGame();
        });
    }
}
