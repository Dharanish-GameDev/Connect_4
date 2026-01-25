using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoP_PlayerData", menuName = "ScriptableObjects/PoP_PlayerData", order = 1)]
public class PoP_PlayerDataSO : ScriptableObject
{
   public string playerName;
   public int playerID;
   public int playerLevel;
   public int playerExp;
   public int coinsValue;
   public Sprite playerIcon;
}
