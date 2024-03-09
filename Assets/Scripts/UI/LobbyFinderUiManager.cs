using Steamworks.Data;
using UnityEngine;

public class LobbyFinderUiManager : MonoBehaviour
{
    [SerializeField] private GameObject m_lobbyItemPrefab;
    [SerializeField] private GameObject m_lobbyContainer;
    //Since lobbies might take a bit to find we just want a loading screen to let the player know something is still happening.
    [SerializeField] private GameObject m_loadingScreenObject;

    public void SetLobbyItems()
    {
        Lobby[] lobbySettings = GameNetworkManager.Instance.Lobbies.ToArray();
        //Clear all previous lobby items then create all new ones required. can fix later to have object pooling but since 10 lobbys will be shown at any given point I think it is fine.
        for (int i = 0; i < m_lobbyContainer.transform.childCount; i++)
        {
            Destroy(m_lobbyContainer.transform.GetChild(0).gameObject);
            i--;
        }
    }
}
