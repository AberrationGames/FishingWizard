using FishingWizard;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
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
