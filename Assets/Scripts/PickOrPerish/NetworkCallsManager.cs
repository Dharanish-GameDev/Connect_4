using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkCallsManager : NetworkBehaviour
{
    public static NetworkCallsManager Instance { get;  private set; }
    private bool isConnectedToNetwork = false;
    private void Awake()
    {
        Instance = this;
    }

    private event Action OnConnectedToNetwork;

    public void RegisterOnConnectedToNetwork(Action action)
    {
        OnConnectedToNetwork += action;
        if (isConnectedToNetwork)
        {
            OnConnectedToNetwork?.Invoke();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isConnectedToNetwork = true;
        OnConnectedToNetwork?.Invoke();
        string temp = NetworkManager.Singleton.IsHost ? "Host" : $"Client_{NetworkManager.Singleton.LocalClientId}";
        Debug.Log("Connected to Network! & You're " + temp);
        if(IsHost)
            NetworkGameManager.Instance.PanelManager.ShowStartMatchButton();
    }
}
