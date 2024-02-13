using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameLobbyType
{
    SinglePlayer = 0,
    OnlineMultiplayer = 1,
    LocalAreaNetworkMultiplayer= 2,
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameLobbyType m_currentLobbyType = GameLobbyType.OnlineMultiplayer;

    private void Awake()
    {
        if (!Instance)
            Instance = this;
        else
        {
            Debug.LogError("There is already one game manager in the scene, Remove / Fix duplicate spawning.");
        }
    }

    public void LoadGame()
    {
        StartCoroutine(LoadScenesAsync());
    }

    
    /// <summary>
    /// Unloads main menu scene into main scene for testing at the moment 
    /// </summary>
    IEnumerator LoadScenesAsync()
    {
        //0 Should always be the main menu.
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(0);
        while (!unloadOperation.isDone)
        {
            yield return null;
        }
        
        SceneManager.LoadScene("DEV_Thomas");

        switch (m_currentLobbyType)
        {
            case GameLobbyType.SinglePlayer:
                break;
            case GameLobbyType.OnlineMultiplayer:
            case GameLobbyType.LocalAreaNetworkMultiplayer:
                GameNetworkManager.Instance.StartHosting();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
