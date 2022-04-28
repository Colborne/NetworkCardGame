using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : MonoBehaviour
{
    public PlayerManager player;
    
    public void PlayCard()
    {
        player = PlayerManager.localPlayer;
        player.CmdPlayCard(player.deck.Dequeue(),0);
    }

    public void DealDamage()
    {
        player = PlayerManager.localPlayer;
        player.CmdAttack(player, player.enemy);
    }
}
