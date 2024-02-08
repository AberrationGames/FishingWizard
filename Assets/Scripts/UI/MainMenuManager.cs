using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class is the only point of interaction in the main menu and should manage everything in the Main Menu Scene.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject m_startScreenObject;
    
    [SerializeField] private Button m_startGameSinglePlayerButton;
    [SerializeField] private Button m_hostLobbyButton;
    [SerializeField] private Button m_joinLobbyButton;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private Button m_quitGameButton;
    
    [SerializeField] private GameObject m_networkSelectionObject;
    [SerializeField] private Button m_onlineOptionButton;
    [SerializeField] private Button m_lanOptionButton;
    
    [SerializeField] private GameObject m_lobbyScreenObject;
    [SerializeField] private Button m_lobbyStartGameButton;
    [SerializeField] private Button m_lobbyReadyUpButton;
    [SerializeField] private Button m_lobbyBackButton;
    //Decide later how to display the players individually in the lobby. should be able to fetch the users steam id and icon. in lan just do player one two three etc with a default icon.
    
    [SerializeField] private GameObject m_settingsScreenObject;
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
