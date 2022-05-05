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
        if(player.isOurTurn)
            player.SelectCard(button.GetComponent<Slot>().slotNumber);
    }

    public void PlayCard(Button button)
    {
        player = PlayerManager.localPlayer;
        int slot = button.GetComponent<Slot>().slotNumber;
        if(player.isOurTurn)
        {
            if(player.currentCard.portrait == null)
            {
                player.SelectFieldCard(slot);
            }
            else
            {
                if(player.currentCard.alreadyPlayed)
                {
                    if(player.field[slot] == null)
                    {
                        player.CmdPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.currentCard.alreadyPlayed = false;
                    }
                    else if(player.currentCard.cardData.fusion == player.field[slot].title)
                    {
                        player.CmdPlayCard(new CardInfo(player.currentCard.cardData.spawn), slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.currentCard.alreadyPlayed = false;
                    }
                    return;
                }
                
                int spr = player.currentCard.cardData.spr;
                int fieldSp = player.field[slot] != null ? player.field[slot].spr : 0;

                if(fieldSp == 0)
                {
                    if(player.sp >= spr)
                    {
                        player.CmdSetMana(-spr);
                        player.CmdPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                    }
                }
                else
                {
                    if(player.sp + fieldSp >= spr)
                    {
                        player.CmdSetMana(-Mathf.Max(0, spr - fieldSp));
                        player.CmdDestroyFieldCard(slot);
                        player.CmdPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                    }
                }
            }
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