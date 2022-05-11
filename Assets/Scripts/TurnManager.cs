using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class TurnManager : NetworkBehaviour
{
    public PlayerManager playerOne, playerTwo;
    [SyncVar] public int whichPlayer = -1;
    public bool needPlayers = true;
    public Canvas canvas;

    private void Update() 
    {

        if(isServer && isClient && canvas.enabled)
            canvas.GetComponentInChildren<TMP_Text>().text = "Waiting for Client..."; 
        
        if(isClientOnly && canvas.enabled)
            canvas.GetComponentInChildren<TMP_Text>().text = "Waiting for Host...";

        if(needPlayers)
        {
            if(PlayerManager.localPlayer != null)
            {
                if(PlayerManager.localPlayer.isServer)
                {
                    playerOne = PlayerManager.localPlayer;
                    if(PlayerManager.localPlayer.enemy != null)
                    {
                        playerTwo = PlayerManager.localPlayer.enemy;
                        needPlayers = false;
                        whichPlayer = 0;
                        playerOne.isOurTurn = true;
                        canvas.enabled = false;
                    }
                    else return;
                }
            }
            else return;
        }


        
    }

    [Command(requiresAuthority = false)]
    public void CmdSetTurn(PlayerManager player)
    {
        RpcSetTurn(player);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetBool(PlayerManager player, bool value)
    {
        player.isOurTurn = value;
    }

    [ClientRpc]
    public void RpcSetTurn(PlayerManager player)
    {
        player.NewTurn();
    }

    public void EndTurn(PlayerManager currentPlayer, PlayerManager target)
    {
        CmdSetBool(currentPlayer, false);
        CmdSetBool(target, true);
        CmdSetTurn(currentPlayer.enemy);
        if(whichPlayer == 0)
            whichPlayer = 1;
        else if(whichPlayer == 1)
            whichPlayer = 0;
    }
}
