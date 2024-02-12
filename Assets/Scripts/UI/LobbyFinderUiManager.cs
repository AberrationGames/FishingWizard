using System;
using Steamworks.Data;
using UnityEngine;

public class LobbyFinderUiManager : MonoBehaviour
{
    [SerializeField] private GameObject m_lobbyItemPrefab;
    [SerializeField] private GameObject m_lobbyContainer;

    private void OnEnable()
    {
        SetLobbyItems();
    }

    public void SetLobbyItems()
    {
        Lobby[] lobbySettings = GameNetworkManager.Instance.Lobbies.ToArray();
        //Clear all previous lobby items then create all new ones required. can fix later to have object pooling but since 10 lobbys will be shown at any given point I think it is fine.
        for (int i = 0; i < m_lobbyContainer.transform.childCount; i++)
        {
            Destroy(m_lobbyContainer.transform.GetChild(0).gameObject);
            i--;
        }
        for (int i = 0; i < lobbySettings.Length; i++)
        {
            GameObject lobbyItem = Instantiate(m_lobbyItemPrefab, m_lobbyContainer.transform);
            LobbyUiItem newLobbyItem = lobbyItem.GetComponent<LobbyUiItem>();
            int indexCopy = i;
            //newLobbyItem.SetData(
            //    lobbySettings[i].Value.SetPublic().m_isPublicLobby ? "Public" : "Private", 
            //    a_lobbyInformation[i].m_lobbyDescription, 
            //    a_lobbyInformation[i].m_currentPlayerCount, 
            //    a_lobbyInformation[i].m_lobbyMaxPlayers, 
            //    () => { GameNetworkManager.Instance.JoinLobbyAtIndex(indexCopy);});
        } 
    }
}
