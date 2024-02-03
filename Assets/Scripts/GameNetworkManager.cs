using System;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
	//private string m_playerName = "";
	//private SteamId m_playerSteamID = 0;
	//private string m_playerSteamIDString = "";
	//private bool m_connectedToSteam = false;
	public static GameNetworkManager Instance { get; private set; }

	private FacepunchTransport m_facepunchTransport;
	private UnityTransport m_unityTransport;
	public Lobby? CurrentLobby { get; private set; }

	public List<Lobby> Lobbies { get; private set; } = new List<Lobby>(capacity: 20);

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

	public async void StartSteamHost(uint a_maxMembers)
	{
		Debug.Log("Started hosting, isSteamHost=" + m_isUsingSteamNetworking.ToString());
		if (m_isUsingSteamNetworking)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
			NetworkManager.Singleton.OnServerStarted += OnServerStarted;
		}

		NetworkManager.Singleton.StartHost();

		if (m_isUsingSteamNetworking)
			CurrentLobby = await SteamMatchmaking.CreateLobbyAsync((int)a_maxMembers);
    }
	public void StartLocalHost(uint a_maxMembers = 4)
	{
		NetworkManager.Singleton.StartHost();
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

	public async Task<bool> RefreshLobbies(int a_maxResults = 20)
	{
		try
		{
			Lobbies.Clear();
			var lobbies = await SteamMatchmaking.LobbyList.FilterDistanceClose().WithMaxResults(a_maxResults).RequestAsync();
			
			if (lobbies == null)
				return false;
			
            for (int i = 0; i < lobbies.Length; i++)
				Lobbies.Add(lobbies[i]);
			
            return true;
		}
		catch (Exception ex)
		{
			Debug.Log("Error fetching lobbies", this);
			Debug.LogException(ex, this);
			return false;
		}
	}

	private Steamworks.ServerList.Internet GetInternetRequest()
	{
		//We can add filters to this request with request.AddFilter("","")
		var request = new Steamworks.ServerList.Internet();
		return request;
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
}
