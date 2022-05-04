using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class GameManager : MonoBehaviour
{
    public PlayerManager player;
    public TurnManager turnManager;
 
    public void SelectCard(Button button)
    {
        player = PlayerManager.localPlayer;
        player.SelectCard(button.GetComponent<Slot>().slotNumber);
    }

    public void PlayCard(Button button)
    {
        if(player.currentCard.portrait != null)
        {
            player = PlayerManager.localPlayer;
            player.CmdPlayCard(player.currentCard.cardData, button.GetComponent<Slot>().slotNumber);
            player.currentCard.GetComponent<Image>().enabled = false;
            player.currentCard.cardData = new CardInfo();
            player.currentCard.portrait = null;
        }
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

    public void EndTurn()
    {
        player = PlayerManager.localPlayer;
        turnManager = FindObjectOfType<TurnManager>();
        turnManager.EndTurn(player, player.enemy);
    }
}