using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class NetworkDebugUi : MonoBehaviour
{
    public static NetworkDebugUi Instance;
    //Reloads scene when pressed so users should be able to swap from lan to online and etc / restart lobbies without reloading the game.
    [SerializeField] private Button m_restartSceneButton;
    [SerializeField] private Button m_lanOptionButton;
    [SerializeField] private Button m_steamOptionButton;
    [SerializeField] private Button m_hostStartButton;
    [SerializeField] private Button m_clientStartButton;

    private bool m_isSteam = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        m_hostStartButton.onClick.AddListener(() =>
        {
            if (m_isSteam)
                GameNetworkManager.Instance.StartHost(8);
            else
                NetworkManager.Singleton.StartHost();
        });
        m_clientStartButton.onClick.AddListener(() =>
        {
            if (m_isSteam)
            {
                //GameNetworkManager.Instance.StartClient();
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
        });
        m_lanOptionButton.onClick.AddListener(SwapToLan);
    }

    private void SwapToLan()
    {
        m_isSteam = false;
    }

    private void SetActiveDebugWindow()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
