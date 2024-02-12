using System.Collections.Generic;
using FishingWizard.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This class is the only point of interaction in the main menu and should manage everything in the Main Menu Scene.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
    //This is here so text can add themselves to this list and when any screen transition occurs they will be set back to normal.
    [HideInInspector] public List<ButtonTextOnHighlight> m_selectedTexts = new List<ButtonTextOnHighlight>();
    
    [SerializeField] private GameObject m_startScreenObject;
    
    [SerializeField] private Button m_startGameSinglePlayerButton;
    [SerializeField] private Button m_hostLobbyButton;
    [SerializeField] private Button m_joinLobbyButton;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private Button m_quitGameButton;
    
    [SerializeField] private GameObject m_networkSelectionObject;
    [SerializeField] private Button m_onlineOptionButton;
    [SerializeField] private Button m_lanOptionButton;
    
    [SerializeField] private GameObject m_lobbySearchScreenObject;
    [SerializeField] private GameObject m_lobbyHostSettingsScreenObject;
    
    //Decide later how to display the players individually in the lobby. should be able to fetch the users steam id and icon. in lan just do player one two three etc with a default icon.
    [SerializeField] private GameObject m_lobbyScreenObject;
    [SerializeField] private Button m_lobbyStartGameButton;
    [SerializeField] private Button m_lobbyReadyUpButton;
    [SerializeField] private Button m_lobbyBackButton;
    
    [SerializeField] private GameObject m_settingsScreenObject;
    
    
    private void Awake()
    {
        if (!Instance)
            Instance = this;
        else 
            Debug.LogError("There are two main menu managers. FIX IT");
        
        m_startGameSinglePlayerButton.onClick.AddListener(LoadGameSinglePlayer);
        m_hostLobbyButton.onClick.AddListener(GotoNetworkSelectionScreenHost);    
        m_joinLobbyButton.onClick.AddListener(GotoNetworkSelectionScreenClient);
        m_settingsButton.onClick.AddListener(GotoSettingsScreen);
        m_quitGameButton.onClick.AddListener(Application.Quit);
    }

    private void Start()
    {
        //We use find objects of type as the event system is in PROD_DONOTDESTROY and cannot be manually referenced outside the scene.
        FindObjectOfType<EventSystem>().SetSelectedGameObject(m_startGameSinglePlayerButton.gameObject);
    }

    private void LoadGameSinglePlayer()
    {
        
    }
    //Easier and cleaner to disable all then reenable.
    private void DisableAllScreenObjects()
    {
        for (int i = 0; i < m_selectedTexts.Count; i++)
            m_selectedTexts[i].SetButtonRegular();
        m_selectedTexts.Clear();
        
        m_lobbyScreenObject.SetActive(false);
        m_startScreenObject.SetActive(false);
        m_networkSelectionObject.SetActive(false);
        m_settingsScreenObject.SetActive(false);
        m_lobbyHostSettingsScreenObject.SetActive(false);
        m_lobbySearchScreenObject.SetActive(false);
    }

    [ContextMenu("Goto Settings Menu")]
    public void GotoSettingsScreen()
    {
        DisableAllScreenObjects();
        m_settingsScreenObject.SetActive(true);
    }

    [ContextMenu("Goto NetworkSelectionHost Menu")]
    public void GotoNetworkSelectionScreenHost()
    {
        DisableAllScreenObjects();
        m_networkSelectionObject.SetActive(true);
        
        m_lanOptionButton.onClick.RemoveAllListeners();
        m_onlineOptionButton.onClick.RemoveAllListeners();
        
        m_lanOptionButton.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.SwapToLanTransport();
            m_lobbyHostSettingsScreenObject.SetActive(true);
        });
        m_onlineOptionButton.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.SwapToSteamTransport();
            m_lobbyHostSettingsScreenObject.SetActive(true);
        });
    }
    
    [ContextMenu("Goto NetworkSelectionClient Menu")]
    public void GotoNetworkSelectionScreenClient()
    {
        DisableAllScreenObjects();
        m_networkSelectionObject.SetActive(true);
        
        m_lanOptionButton.onClick.RemoveAllListeners();
        m_onlineOptionButton.onClick.RemoveAllListeners();
        
        m_lanOptionButton.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.SwapToLanTransport();
            m_lobbySearchScreenObject.SetActive(true);
        });
        m_onlineOptionButton.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.SwapToSteamTransport();
            m_lobbySearchScreenObject.SetActive(true);
        });
    }
    
    [ContextMenu("Goto Lobby Menu")]
    public void GotoLobbyScreen()
    {
        DisableAllScreenObjects();
        m_lobbyScreenObject.SetActive(true);
    }
    
    [ContextMenu("Goto Start Menu")]
    public void GotoStartScreen()
    {
        DisableAllScreenObjects();
        m_startScreenObject.SetActive(true);
    }
}
