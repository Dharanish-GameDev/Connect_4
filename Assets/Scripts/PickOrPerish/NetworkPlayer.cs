using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkPlayer : NetworkBehaviour
{
    private PlayerUISet _playerUISet;
    
    // NetworkVariables
    
    private NetworkVariable<int> currentNumber = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    private NetworkVariable<int> currentScore = new NetworkVariable<int>(3,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    private NetworkVariable<int> playerActiveState = new NetworkVariable<int>(0); // 0 is Active, 1 - Eliminated

    private bool isSubmitted = false;
    
    public int GetPlayerID() => (int)OwnerClientId;

    public void RegisterCurrentNumberValueChanged(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        currentNumber.OnValueChanged -= callback;
        currentNumber.OnValueChanged += callback;
    }

    public void RegisterCurrentScoreValueChanged(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        currentScore.OnValueChanged -= callback;
        currentScore.OnValueChanged += callback;
    }

    public void RegisterPlayerStateChanges(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        playerActiveState.OnValueChanged -= callback;
        playerActiveState.OnValueChanged += callback;
    }

    public int GetCurrentScore()
    {
        return currentScore.Value;
    }

    public PlayerState GetCurrentPlayerState()
    {
        return (PlayerState)playerActiveState.Value;
    }
    

    #region  Networking Methods

    public override void OnNetworkSpawn()
    {
        // Need to Instantiate UI Set
        name = "Player_" + OwnerClientId.ToString();
        _playerUISet = NetworkGameManager.Instance.PanelManager?.CreatePlayerUISet();
        if (_playerUISet != null)
        {
            _playerUISet.SetPlayer(this);
        }
        NetworkGameManager.Instance.RegisterOnCurrentRoundValueChanged(OnCurrentRoundChanged);
        NetworkGameManager.Instance.RegisterMeToTheMatch(this);
        NetworkGameManager.Instance.OnTimerEnd += () =>
        {
            if (!isSubmitted && playerActiveState.Value == 0 && IsOwner) // Means Still Playing And Not Submitted
            {
                // Making it Force Submit
                _playerUISet.SetSubmitButtonInteractable(false);
                int value = _playerUISet.GetInputValue();
                if (value > 0)
                {
                    SetCurrentNumber(value);
                    Debug.Log("Force Submitting");
                }
                else
                {
                    Debug.Log("Submitting Previous Number : " + lastSubmitterValue);
                    SetCurrentNumber(lastSubmitterValue);
                }
            }
        };
        RegisterCurrentNumberValueChanged(OnCurrentNumberChanged);
    }

    public override void OnNetworkDespawn()
    {
        // Need to Destroy the UI Set
        if (_playerUISet != null)
        {
            Destroy(_playerUISet.gameObject);
        }
    }
    int lastSubmitterValue = 0;
    private void OnCurrentNumberChanged(int prev, int current)
    {
        if(current < 0 || !IsOwner) return;
        lastSubmitterValue = current;
        isSubmitted = true;
        SubmitCurrentNumberServerRPC();
    }
    private void OnCurrentRoundChanged(int prevRound,int currentRound)
    {
        if (IsOwner)
        {
            SetCurrentNumber(-1); // Means To Clear it Up
        }
        if (_playerUISet != null)
        {
            _playerUISet.SetSubmitButtonInteractable(true);
        }
        isSubmitted = false;
    }
    public void UpdateCurrentScore(int score)
    {
        Debug.Log($"Player_{GetPlayerID()}_New Score: {score}");
        UpdateCurrentScoreClientRPC(score);
    }
    [ClientRpc]
    private void UpdateCurrentScoreClientRPC(int score)
    {
        if(!IsOwner) return;
        UpdateCurrentScoreServerRPC(score);
    }
    [ServerRpc]
    private void UpdateCurrentScoreServerRPC(int score)
    {
        currentScore.Value = score;
        if (currentScore.Value <= 0)
        {
            playerActiveState.Value = 1; // Means Eliminated
        }
    }
    
    // RPC Methods
    public void SetCurrentNumber(int number)
    {
        SetCurrentNumberServerRPC(number);
    }
    [ServerRpc]
    private void SetCurrentNumberServerRPC(int number)
    {
        if (IsServer)
        {
            currentNumber.Value = number;
        }
    }
    [ServerRpc]
    public void SubmitCurrentNumberServerRPC()
    {
        NetworkGameManager.Instance.SubmittedNumber(GetPlayerID(), currentNumber.Value);
    }
    
    #endregion
    
    
}
