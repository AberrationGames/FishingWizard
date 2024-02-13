using System;
using System.Collections;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;

public class GameNetworkManager : MonoBehaviour
{
	//This is to have a saveable data struct so the users lobby settings can be saved and loaded each time so changing it isnt annoying and required every time.
	[Serializable]
	public struct LocalLobbySettings
	{
		public bool m_isPublicLobby;
		public string m_lobbyDescription;
		//current player count is only used for ui, every other check for player count limits will be done through steamworks lobby system
		public int m_currentPlayerCount;
		public int m_lobbyMaxPlayers;
	}
	public static GameNetworkManager Instance { get; private set; }

	public LocalLobbySettings m_localLobbySettings;

	private FacepunchTransport m_facepunchTransport;
	private UnityTransport m_unityTransport;
	public Lobby? CurrentLobby { get; private set; }

	public List<Lobby> Lobbies { get; private set; } = new List<Lobby>(capacity: 4);

	public bool m_isUsingSteamNetworking;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else
		{
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);
	}

	public void SwapToLanTransport()
	{
		NetworkManager.Singleton.NetworkConfig.NetworkTransport = m_unityTransport;
		m_isUsingSteamNetworking = false;
	}

	public void SwapToSteamTransport()
	{
		NetworkManager.Singleton.NetworkConfig.NetworkTransport = m_facepunchTransport;
		m_isUsingSteamNetworking = true;
	}

	public void Update()
	{
		SteamClient.RunCallbacks();
	}

	public void OnDisable()
	{
		SteamClient.Shutdown();
	}

	private void Start()
    {
#if UNITY_EDITOR
		Debug.unityLogger.logEnabled = true;
#else
		Debug.unityLogger.logEnabled = Debug.isDebugBuild;
#endif
		m_facepunchTransport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
		m_unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
		SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

		//Find users ipv4 so we can set that for the lan networking without getting the user to try and find it.
		foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				m_unityTransport.ConnectionData.Address = address.ToString();
				break;
			}
		}

		m_localLobbySettings.m_isPublicLobby = false;
		m_localLobbySettings.m_lobbyMaxPlayers = 4;
		m_localLobbySettings.m_lobbyDescription = "Lobby Description.";
		m_localLobbySettings.m_currentPlayerCount = 1;
		
		FindLobbies();
    }
	private void OnDestroy()
	{
		if (NetworkManager.Singleton == null)
			return;

		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
		
		SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
		SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
		SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
		SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
		SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
	}

	private void OnApplicationQuit() => Disconnect();

	[ContextMenu("Start Host")]
	public async void StartHosting()
	{
		Debug.Log("Started hosting, isSteamHost=" + m_isUsingSteamNetworking);
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStarted += OnServerStarted;

		NetworkManager.Singleton.StartHost();

		if (m_isUsingSteamNetworking)
			CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(m_localLobbySettings.m_lobbyMaxPlayers);
    }
	
	public void FindLobbies()
	{
		IEnumerable<Friend> steamFriends = SteamFriends.GetFriends();
		foreach (Friend friend in steamFriends)
		{
			if (!friend.IsPlayingThisGame || friend.GameInfo == null || friend.GameInfo.Value.Lobby == null)
				continue;
            Lobbies.Add(friend.GameInfo.Value.Lobby.Value);
		}
	}


	public void StartClient(SteamId a_id)
	{
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

		if (m_isUsingSteamNetworking)
		{
			m_facepunchTransport.targetSteamId = a_id;
			Debug.Log($"Joining room hosted by {m_facepunchTransport.targetSteamId}", this);
		}

		if (NetworkManager.Singleton.StartClient())
			Debug.Log("Client has joined!", this);
	}

	public void Disconnect()
	{
		CurrentLobby?.Leave();

		if (NetworkManager.Singleton == null)
			return;

		NetworkManager.Singleton.Shutdown();
	}

	#region Steam Callbacks

	//This is where the invite is accepted and the client is now joining/joined the lobby.
	private void OnGameLobbyJoinRequested(Lobby a_lobby, SteamId a_id)
	{
		bool isSame = a_lobby.Owner.Id.Equals(a_id);

		Debug.Log($"Owner: {a_lobby.Owner}");
		Debug.Log($"Id: {a_id}");
		Debug.Log($"IsSame: {isSame}", this);

		StartClient(a_id);
	}

	private void OnLobbyInvite(Friend a_friend, Lobby a_lobby) { /* Received an invite from .... */ }
	private void OnLobbyMemberLeave(Lobby a_lobby, Friend a_friend) { /* Manage lobby ui */ }
	private void OnLobbyMemberJoined(Lobby a_lobby, Friend a_friend) { /* Manage lobby ui */ }

	private void OnLobbyEntered(Lobby a_lobby)
    {
		Debug.Log($"You have entered in lobby, clientId={NetworkManager.Singleton.LocalClientId}", this);

		if (NetworkManager.Singleton.IsHost)
			return;

		StartClient(a_lobby.Owner.Id);
	}

    private void OnLobbyCreated(Result a_result, Lobby a_lobby)
	{
		if (a_result != Result.OK)
        {
			Debug.LogError($"Lobby couldn't be created!, {a_result}", this);
			return;
		}

		//I believe this is for the use of filters in the lobby search system leaving here so we know to add these when we are doing online lobbies for people without friends to play with.
		a_lobby.SetFriendsOnly(); // Set to friends only!
		a_lobby.SetData("TestingLobby", "Lobby for testing fishing wizard");
		a_lobby.SetJoinable(true);
		Debug.Log("Lobby has been created!");
	}

	#endregion

	#region Network Callbacks

	private void ClientConnected(ulong a_clientId) => Debug.Log($"I'm connected, clientId={a_clientId}");

    private void ClientDisconnected(ulong a_clientId)
	{
		Debug.Log($"I'm disconnected, clientId={a_clientId}");

		NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
		NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
	}

	private void OnServerStarted() { }

    private void OnClientConnectedCallback(ulong a_clientId) => Debug.Log($"Client connected, clientId={a_clientId}", this);

    private void OnClientDisconnectCallback(ulong a_clientId) => Debug.Log($"Client disconnected, clientId={a_clientId}", this);

    #endregion

    public void JoinLobbyAtIndex(int a_lobbyListIndex)
    {
    }
}
