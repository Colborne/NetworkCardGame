using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

[SerializeField]
public struct PlayerInfo
{
    public int hp;
    public int sp;
    public int deckSize;

    public PlayerInfo(int hp, int sp, int deckSize)
    {
        this.hp = hp;
        this.sp = sp;
        this.deckSize = deckSize;
    }
}

public class PlayerManager : NetworkBehaviour
{
    public PlayerInfo player;
    public PlayerInfo enemy;
    public static PlayerManager localPlayer;
    public GameObject playerField;
    public GameObject enemyField;
    public GameObject cardToSpawn;
    public bool hasEnemy;
    public CardInfo hand;
    public ScriptableCard handCard;

    private void Awake() {
        player = new PlayerInfo(10,10,20);
        hand = new CardInfo(handCard);
    }

    public override void OnStartLocalPlayer()
    {
        localPlayer = this;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        playerField = GameObject.Find("Player Field");
        enemyField = GameObject.Find("Enemy Field");    

        FindObjectOfType<GameManager>().player = this;
        CmdUpdatePlayerText();
    }

    [Command]
    public void CmdUpdatePlayerText()
    {
        RpcUpdatePlayerText();
    }

    [ClientRpc]
    public void RpcUpdatePlayerText()
    {
        if(hasAuthority)
        {
            playerField.transform.GetChild(0).GetComponent<TMP_Text>().text = player.hp.ToString();
            playerField.transform.GetChild(1).GetComponent<TMP_Text>().text = player.sp.ToString();
            playerField.transform.GetChild(2).GetComponent<TMP_Text>().text = player.deckSize.ToString();
            enemyField.transform.GetChild(0).GetComponent<TMP_Text>().text = enemy.hp.ToString();
            enemyField.transform.GetChild(1).GetComponent<TMP_Text>().text = enemy.sp.ToString();
            enemyField.transform.GetChild(2).GetComponent<TMP_Text>().text = enemy.deckSize.ToString();
        }
    }

    private void Update() 
    {
        if(!hasEnemy)
        {
            UpdateEnemyInfo();
            CmdUpdatePlayerText();
        }
    }

    public void UpdateEnemyInfo()
    {
        // Find all Players and add them to the list.
        PlayerManager[] onlinePlayers = FindObjectsOfType<PlayerManager>();

        // Loop through all online Players (should just be one other Player)
        foreach (PlayerManager players in onlinePlayers)
        {
            if (players != this)
            {
                enemy = new PlayerInfo(players.player.hp, players.player.sp, players.player.deckSize);
                hasEnemy = true;
            }
        }
    }

    [Command]
    public void CmdPlayCard(CardInfo card, int index)
    {
        GameObject bc = Instantiate(cardToSpawn, new Vector3(0, 0, 0), Quaternion.identity);
        FieldCard fc = bc.GetComponent<FieldCard>();
        fc.cardData = new CardInfo(card.data);
        fc.title = card.title;
        fc.spr = card.spr;
        fc.portrait = card.image;
        bc.GetComponent<Image>().sprite = fc.portrait;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayCard(bc, index);
    }

    [ClientRpc]
    public void RpcDisplayCard(GameObject card, int index)
    {
        if(hasAuthority)
            card.transform.SetParent(playerField.transform.GetChild(3).GetChild(index), false);
        else
            card.transform.SetParent(enemyField.transform.GetChild(3).GetChild(index), false);
    }
}