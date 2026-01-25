using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkConnectionHandler : MonoBehaviour
{
    public void StartAsHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartAsClient()
    {
       NetworkManager.Singleton.StartClient();
    }
}
