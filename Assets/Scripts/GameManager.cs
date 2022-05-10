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
                    else if(player.currentCard.cardData.fusion == player.field[slot].title && player.field[slot].frozenTime == 0)
                    {
                        player.CmdDestroyFieldCard(slot);
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
                    if(player.sp + fieldSp >= spr && player.field[slot].frozenTime == 0)
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

    public void EndTurn()
    {
        player = PlayerManager.localPlayer;

        for(int i = 0; i < 5; i++)
        {
            if(player.field[i] != null)
            {
                if(player.field[i].rotPosition == player.field[i].cardPosition && player.field[i].rot)
                {
                    player.CmdDestroyFieldCard(i);
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == player.field[i])
                            Destroy(eff.gameObject);
                    }
                }
                else
                {
                    player.field[i].rot = false;
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == player.field[i])
                            Destroy(eff.gameObject);
                    }
                }
                if(player.field[i].frozenTime > 0)
                    player.field[i].frozenTime--;
                if(player.field[i].frozenTime == 0)
                {
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == player.field[i])
                            Destroy(eff.gameObject);
                    }
                }
                if(player.field[i].ability == FieldCard.Ability.Defend)
                    player.field[i].EffectSpawn(player);
            }
        }

        turnManager = FindObjectOfType<TurnManager>();
        turnManager.EndTurn(player, player.enemy);
    }
}