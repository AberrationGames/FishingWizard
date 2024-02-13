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

    public void LoadGame(bool a_isHost)
    {
        StartCoroutine(LoadGameAsync(a_isHost));
    }

    private IEnumerator LoadGameAsync(bool a_isHost)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("DEV_Thomas", LoadSceneMode.Single);
        while (!asyncOperation.isDone)
            yield return null;
        
        switch (m_currentLobbyType)
        {
            case GameLobbyType.SinglePlayer:
                break;
            case GameLobbyType.OnlineMultiplayer:
            case GameLobbyType.LocalAreaNetworkMultiplayer:
                if (a_isHost)
                    GameNetworkManager.Instance.StartHosting();
                else
                    GameNetworkManager.Instance.StartClient(0);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
