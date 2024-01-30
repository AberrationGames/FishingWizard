using System;
using System.Collections;
using System.Collections.Generic;
using FishingWizard;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// This class will manage the multiple players and everything else. the plan will be for this object to sync everything
/// and to be run on the hosts computer. possibly later it will be able to run on a server but p2p should be fine.
/// </summary>
public class ServerManager : NetworkBehaviour 
{
    const int MAX_CHARACTERS = 4;

    private FishermanController[] m_characters;

    private void Awake()
    {
        m_characters = new FishermanController[MAX_CHARACTERS];
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
