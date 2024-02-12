using UnityEngine;

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
    
    
}
