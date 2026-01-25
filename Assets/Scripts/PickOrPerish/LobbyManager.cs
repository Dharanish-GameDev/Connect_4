using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;
// using VeganVR.VoiceChat;
// using VeganVR.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Private Variables

    [SerializeField] private UnityTransport transport;
    [SerializeField] private GameObject netUICanvas;

    [Space(10)]
    [Header("Lobby")]
    [SerializeField] private Button createButton;
    [SerializeField] private Button searchLobbyButton;
    [SerializeField] private GameObject lobbyPrefab;
    [SerializeField] private Transform lobbyPrefabParent;
    [SerializeField] private TextMeshProUGUI lobbyLable;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;


    // Hidden in Inspector
    private int maxConnections = 2;
    private readonly string lobbyName = "PickOrPerish";
    private readonly string startGameDataKey = "START_GAME";
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartBeatTimer;
    private float lobbyUpdateTimer;
    private readonly float heartBeatTimerMax = 15;
    private readonly float lobbyUpdateTimerMax = 1.1f;
    private bool isOwnerOfTheLobby = false;
    private List<LobbyPrefab> lobbyPrefabList = new List<LobbyPrefab>();
    private bool isGameStarted = false;


    // Test Variables Must Be removed
    private string testName = "Test";
    private bool isAuthenticated = false;

    #endregion

    #region Properties



    #endregion

    #region LifeCycle Methods

    private void Awake()
	{
        Instance = this;
        PlayerPrefs.DeleteAll();
        ChangeLobbyLableTextByValue(0);
        createButton.onClick.AddListener(() => CreateLobby());
        searchLobbyButton.onClick.AddListener(() =>SearchAvailableLobbies());
        maxPlayersDropdown.onValueChanged.AddListener(OnMaxPlayerDropDownChanged);
    }
	private void Start()
	{

	}
	private void Update()
	{
        if (isGameStarted) return;
        HandleLobbyHeartBeat();
        LobbyPollForUpdates();

#if UNITY_EDITOR

        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if (isAuthenticated) return;
            //VivoxPlayer.Instance.AuthenticateServies(testName);
            isAuthenticated = true;
        }
#endif
    }

    #endregion

    #region Private Methods

    private void OnMaxPlayerDropDownChanged(int newValue)
    {
        Debug.Log("MaxPlayerDropDownChanged: " + newValue);
        maxConnections = newValue + 1;
        Debug.Log("New Max Connections: " + maxConnections);
    }

    public async void CreateLobby()
    {
        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                    {
                    {startGameDataKey,new DataObject(DataObject.VisibilityOptions.Member,"0") }
                    }
            };

            hostLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxConnections, lobbyOptions);
            joinedLobby = hostLobby;
            isOwnerOfTheLobby = true;
            ChangeUiAfterCreatingLobby(hostLobby);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public async void SearchAvailableLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 4,
                Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                    },
                Order = new List<QueryOrder>
                    {
                        new QueryOrder(false,QueryOrder.FieldOptions.Created)
                    }

            };


            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            if (queryResponse.Results.Count > 0)
            {
                ClearLobbyPrefabList();
                ChangeLobbyLableTextByValue(2);
                foreach (var lobby in queryResponse.Results)
                {
                    CreateLobbyPrefab(lobby, isOwnerOfTheLobby);
                }
            }
            else
            {
                ClearLobbyPrefabList();
                ChangeLobbyLableTextByValue(0);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby == null) return;
        heartBeatTimer -= Time.deltaTime;
        if (heartBeatTimer < 0)
        {
            heartBeatTimer = heartBeatTimerMax;
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }
    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            if (lobby.AvailableSlots == 0) return;

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
            {

            });

            Debug.Log("JOINED LOBBY : " + joinedLobby.Id);
            DisableButtonsAfterCreatingLobby();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }


    }
    private void ClearLobbyPrefabList()
    {
        if (lobbyPrefabList.Count == 0) return;
        foreach (var lobbyPrefab in lobbyPrefabList)
        {
            Destroy(lobbyPrefab.gameObject);
        }
        lobbyPrefabList.Clear();
    }
    private void CreateLobbyPrefab(Lobby lobby, bool isOwnerOfTheLobby)
    {
        LobbyPrefab lobbyObject = Instantiate(lobbyPrefab).GetComponent<LobbyPrefab>();
        lobbyObject.transform.SetParent(lobbyPrefabParent, false);
        lobbyObject.UpdateLobbyPrefabUI(lobby, isOwnerOfTheLobby);
        lobbyPrefabList.Add(lobbyObject);
    }

    /// <summary>
    /// 0 - No Lobbies Available,1 - Joined Lobby, 2 - Available Lobbies
    /// </summary>
    /// <param name="value"></param>
    private void ChangeLobbyLableTextByValue(int value)
    {
        if (value > 2 || value < 0) return;
        switch (value)
        {
            case 0:
                lobbyLable.SetText("No Lobbies Available");
                break;
            case 1:
                lobbyLable.SetText("Joined Lobby");
                break;
            case 2:
                lobbyLable.SetText("Available Lobbies");
                break;
        }
    }
    private void ChangeUiAfterCreatingLobby(Lobby lobby)
    {
        ChangeLobbyLableTextByValue(1);
        ClearLobbyPrefabList();
        CreateLobbyPrefab(lobby, isOwnerOfTheLobby);
        DisableButtonsAfterCreatingLobby();
    }
    private async void LobbyPollForUpdates()
    {
        if (joinedLobby == null) return;
        lobbyUpdateTimer -= Time.deltaTime;
        if (lobbyUpdateTimer < 0)
        {
            lobbyUpdateTimer = lobbyUpdateTimerMax;
            joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            foreach (var lobbyPrefab in lobbyPrefabList)
            {
                lobbyPrefab.UpdateLobbyPrefabUI(joinedLobby, isOwnerOfTheLobby);
            }
            if (joinedLobby.Data[startGameDataKey].Value != "0")
            {
                if (!isOwnerOfTheLobby)
                {
                    JoinRelay(joinedLobby.Data[startGameDataKey].Value);
                    joinedLobby = null;
                    Debug.Log("Game Started");
                }
            }
        }
    }
    private async void UpdateRelayCode(string relayCode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    {
                        {startGameDataKey,new DataObject(DataObject.VisibilityOptions.Public,relayCode) }
                    }
            });
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }
    private async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);
            Debug.Log("Game Started");
            NetworkManager.Singleton.StartHost();
            Invoke(nameof(MakeIsGameStartedTrue), 5);
            netUICanvas.gameObject.SetActive(false);
            return joinCode;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            return null;
        }

    }
    private async void JoinRelay(string joincode)
    {
        try
        {
            JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joincode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            transport.SetRelayServerData(relayServerData);
            Debug.Log("JOINED RELAY WITH JOINCODE : " + joincode);
            NetworkManager.Singleton.StartClient();
            Invoke(nameof(MakeIsGameStartedTrue), 5);
            netUICanvas.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }


    }
    public async void StartGame()
    {
        if (isOwnerOfTheLobby)
        {
            try
            {
                string relayCode = await CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { startGameDataKey,new DataObject(DataObject.VisibilityOptions.Member,relayCode) }
                    }
                });

                joinedLobby = lobby;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }

        }
    }
    private void DisableButtonsAfterCreatingLobby()
    {
        createButton.interactable = false;
        searchLobbyButton.interactable = false;
    }
    
 
    private void MakeIsGameStartedTrue()
    {
        isGameStarted = true;
        Debug.Log("Setted IsGameStarted to True");
    }


    #endregion

    #region Public Methods

    public void SetMaxPlayersCount(int maxPlayersCount)
    {
        Debug.Log(maxPlayersCount);
    }
    #endregion
}
