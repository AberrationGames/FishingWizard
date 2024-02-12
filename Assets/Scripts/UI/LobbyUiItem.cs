using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyUiItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_lobbyOnlineTypeText;
    [SerializeField] private TextMeshProUGUI m_lobbyDescription;
    [SerializeField] private TextMeshProUGUI m_lobbyPlayerCount;
    [SerializeField] private Button m_joinLobbyButton;
    
    public void SetData(string a_onlineTypeText, string a_lobbyDescription, int a_playerCount, int a_maxPlayers, UnityAction a_callback)
    {
        m_lobbyOnlineTypeText.text = a_onlineTypeText;
        m_lobbyDescription.text = a_lobbyDescription;
        m_lobbyPlayerCount.text = $"{a_playerCount}/{a_maxPlayers}";
        m_joinLobbyButton.onClick.AddListener(a_callback);
    }
    [ContextMenu("Set Debug Lobby Data")]
    private void SetDebugData()
    {
        m_lobbyOnlineTypeText.text = "Friend";
        m_lobbyDescription.text = "This is a Description!";
        m_lobbyPlayerCount.text = $"2/5";
        m_joinLobbyButton.onClick.AddListener(() => {Debug.Log("You pressed the join button!");});
    }
}
