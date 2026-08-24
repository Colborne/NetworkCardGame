using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class GameManager : MonoBehaviour
{
    public PlayerManager player;
    public TurnManager turnManager;
    public Canvas canvas;
 
    public void SelectCard(Button button)
    {
        player = PlayerManager.Local;
        if (!player)
            return;
        if(player.isOurTurn && !player.isResolvingActions)
            player.SelectCard(button.GetComponent<Slot>().slotNumber);
    }

    public void PlayCard(Button button)
    {
        player = PlayerManager.Local;
        if (!player)
            return;
        int slot = button.GetComponent<Slot>().slotNumber;
        if(player.isOurTurn && !player.isResolvingActions)
        {
            if(player.currentCard.portrait == null && player.field[slot] != null && player.field[slot].frozenTimer == 0)
            {
                player.SelectFieldCard(slot);
            }
            else
            {
                if(player.currentCard.alreadyPlayed)
                {
                    if(player.field[slot] == null)
                    {
                        player.RequestPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.currentCard.alreadyPlayed = false;
                        player.PauseAfterInteraction();
                    }
                    else if(player.currentCard.cardData.fusion == player.field[slot].title && player.field[slot].frozenTimer == 0)
                    {
                        player.RecordAction($"{player.actionName} fuses <b>{player.currentCard.cardData.title}</b> with <b>{player.field[slot].title}</b>.");
                        player.RequestDestroyFieldCard(slot);
                        player.RequestPlayCard(new CardInfo(player.currentCard.cardData.spawn), slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.currentCard.alreadyPlayed = false;
                        player.PauseAfterInteraction();
                    }
                    return;
                }
                
                int spr = player.currentCard.cardData.spr;
                int fieldSp = player.field[slot] != null ? player.field[slot].spr : 0;

                if(fieldSp == 0)
                {
                    if(player.sp >= spr)
                    {
                        player.RequestAdjustMana(-spr);
                        player.RequestPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.PauseAfterInteraction();
                    }
                }
                else
                {
                    if(player.sp + fieldSp >= spr && player.field[slot].frozenTimer == 0)
                    {
                        player.RequestAdjustMana(-Mathf.Max(0, spr - fieldSp));
                        player.RequestDestroyFieldCard(slot);
                        player.RequestPlayCard(player.currentCard.cardData, slot);
                        player.currentCard.GetComponent<Image>().enabled = false;
                        player.currentCard.cardData = new CardInfo();
                        player.currentCard.portrait = null;
                        player.PauseAfterInteraction();
                    }
                }
            }
        }
    }

    public void EndTurn()
    {
        player = PlayerManager.Local;
        if (!player)
            return;
        if(player.currentCard.portrait == null && player.isOurTurn && !player.isResolvingActions)
            StartCoroutine(EndTurn(player, player.enemy));
    }

    IEnumerator EndTurn(PlayerManager player, PlayerManager enemy)
    {
        yield return player.ResolveEndTurn();
        turnManager = FindFirstObjectByType<TurnManager>();
        if (!turnManager)
            yield break;

        turnManager.EndTurn(player, enemy);
    }
}
