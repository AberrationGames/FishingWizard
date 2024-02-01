using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class NetworkDebugUi : MonoBehaviour
{
    [SerializeField] private Button m_hostStartButton;
    [SerializeField] private Button m_serverStartButton;
    [SerializeField] private Button m_clientStartButton;

    private void Awake()
    {
        m_hostStartButton.onClick.AddListener(() =>
        {
            GameNetworkManager.Instance.StartHost(8);
        });
        m_serverStartButton.onClick.AddListener(() =>
        {
            
        });
        m_clientStartButton.onClick.AddListener(() =>
        {
            
        });
    }
}
