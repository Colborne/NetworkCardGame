using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : MonoBehaviour
{
    public PlayerManager player;
    bool started = false;
 
    public void PlayCard()
    {
        player = PlayerManager.localPlayer;
        player.CmdPlayCard(player.deck.Dequeue(),2);
    }

    public void DealDamage()
    {
        player = PlayerManager.localPlayer;
        for(int i = 0; i < player.field.Length; i++)
        {
            if(player.field[i] != null)
            {
                Debug.Log(i);
                player.field[i].CmdDamage(player, player.enemy);
            }              
        }       
    }
}