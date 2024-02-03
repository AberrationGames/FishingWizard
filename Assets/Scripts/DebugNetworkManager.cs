using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[ExecuteAlways]
public class DebugNetworkManager : MonoBehaviour
{
    [Header("Debug / Local area testing requires a different transport layer")]
    [Header("That needs to be changed before running the game")]
    [SerializeField] private bool m_isLocalBuild;
    private bool m_previousIsLocalBool = true;

    private NetworkManager m_networkManager;
    private FacepunchTransport m_facePunchTransport;
    private UnityTransport m_unityTransport;
    private GameNetworkManager m_gameNetworkManager;

    private void Start()
    {
        m_previousIsLocalBool = m_isLocalBuild;
        GetComponentsForDebug();
    }

    private void Update()
    {
        if (m_isLocalBuild == m_previousIsLocalBool)
            return;
        if (m_networkManager == null || m_facePunchTransport == null || m_unityTransport == null || m_gameNetworkManager == null)
            GetComponentsForDebug();
        
        if (m_isLocalBuild)
        {
            m_unityTransport.enabled = true;
            m_facePunchTransport.enabled = false;
            m_gameNetworkManager.m_isUsingSteamNetworking = false;
            m_networkManager.NetworkConfig.NetworkTransport = m_unityTransport;
        }
        if (!m_isLocalBuild)
        {
            m_unityTransport.enabled = false;
            m_facePunchTransport.enabled = true;
            m_gameNetworkManager.m_isUsingSteamNetworking = true;
            m_networkManager.NetworkConfig.NetworkTransport = m_facePunchTransport;
        }
        
        Debug.Log(m_isLocalBuild ? "Set To Local Build" : "Set To Steam Build");
        m_previousIsLocalBool = m_isLocalBuild;
    }

    //ExecuteAlways doesnt run start 
    private void GetComponentsForDebug()
    {
        m_networkManager = GetComponent<NetworkManager>();
        m_facePunchTransport = GetComponent<FacepunchTransport>();
        m_unityTransport = GetComponent<UnityTransport>();
        m_gameNetworkManager = GetComponent<GameNetworkManager>();
    }
}
