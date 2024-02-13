using UnityEngine;

public class GameManagerLoaderComponent : MonoBehaviour
{
    [SerializeField] private GameObject m_eventSystem;
    [SerializeField] private GameObject m_gameManager;
    [SerializeField] private GameObject m_networkManager;
    
    private void Awake()
    {
        //We just dont load this again if its already there as for dev purposes being able to play specific scenes without going through the menu will be useful.
        if (FindObjectOfType<GameNetworkManager>() != null)
            return;
        
        //Network manager cannot be nested so we use that as the manager parent object
        GameObject networkManager = Instantiate(m_networkManager);
        networkManager.transform.position = Vector3.zero;
        networkManager.transform.rotation = Quaternion.Euler(Vector3.zero);
        networkManager.transform.localScale = Vector3.one;
        
        Instantiate(m_eventSystem, networkManager.transform);
        Instantiate(m_gameManager, networkManager.transform);
        
        //Since the other managers are parented under network manager only network needs to be put into the dont destroy on load.
        DontDestroyOnLoad(networkManager);
    }
}
