using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

[Serializable]
public struct PlayerInfo
{
    GameObject player;

    public PlayerInfo(GameObject player)
    {
        this.player = player;
    }

    public PlayerManager data
    {
        get
        {
            // Return ScriptableItem from our cached list, based on the card's uniqueID.
            return player.GetComponent<PlayerManager>();
        }
    }

    public string user => data.username;
    public int hp => data.hp;
    public int sp => data.sp;
    public int deckSize => data.deckSize;
}

public class PlayerManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdatePlayerName))] public string username;
    [SyncVar] public int hp;
    [SyncVar] public int sp;
    [SyncVar] public int deckSize;
    public PlayerManager enemy;
    public static PlayerManager localPlayer;
    public GameObject playerField;
    public GameObject enemyField;
    public GameObject cardToSpawn;
    public bool hasEnemy;
    public Queue<CardInfo> deck;
    [SerializeField] public CardInfo[] hand;
    public CardInfo[] field;
    DeckBuilder deckBuilder;
    
    [Command]
    public void CmdLoadPlayer(string user, int health, int sum, int deck)
    {
        username = user;
        hp = health;
        sp = sum;
        deckSize = deck;
    }

    void UpdatePlayerName(string oldUser, string newUser)
    {
        username = newUser;
        gameObject.name = newUser;
    }

    private void Awake() 
    {
        deckBuilder = FindObjectOfType<DeckBuilder>();
        hp = 20;
        sp = 1;
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
        InstantiatePlayer();
        CmdLoadPlayer(FindObjectOfType<TMP_InputField>().text, hp, sp, deckSize);
    }

    public void InstantiatePlayer()
    {
        deck = new Queue<CardInfo>();
        hand = new CardInfo[3];
        field = new CardInfo[5];

        for(int i = 0; i < deckBuilder.Deck.Count; i++)
            deck.Enqueue(new CardInfo(deckBuilder.Deck[i]));  

        deckSize = deck.Count;
        //for(int i = 0; i < hand.Length; i++)
            //hand[i] = deck.Dequeue();    
    }

    [Command]
    public void CmdUpdatePlayerText(string u, int h, int s, int d)
    {
        RpcUpdatePlayerText(u,h,s,d);
    }

    [ClientRpc]
    public void RpcUpdatePlayerText(string u, int h, int s, int d)
    {
        if(hasAuthority)
        {
            playerField.transform.GetChild(0).GetComponent<TMP_Text>().text = u;
            playerField.transform.GetChild(1).GetComponent<TMP_Text>().text = h.ToString();
            playerField.transform.GetChild(2).GetComponent<TMP_Text>().text = s.ToString();
            playerField.transform.GetChild(3).GetComponent<TMP_Text>().text = d.ToString();
        }
        else
        {
            enemyField.transform.GetChild(0).GetComponent<TMP_Text>().text = u;
            enemyField.transform.GetChild(1).GetComponent<TMP_Text>().text = h.ToString();
            enemyField.transform.GetChild(2).GetComponent<TMP_Text>().text = s.ToString();
            enemyField.transform.GetChild(3).GetComponent<TMP_Text>().text = d.ToString();
        }
    }

    private void Update() 
    {
        if(!hasEnemy)
        {
            UpdateEnemyInfo();
        }     
        deckSize = deck.Count; 
        CmdUpdatePlayerText(username,hp,sp,deckSize); 
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
                enemy = players;
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
        hand[index] = fc.cardData;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayCard(bc, index);
    }

    [Command(requiresAuthority = false)]
    public void CmdAttack(PlayerManager player, PlayerManager enemy)
    {
        enemy.hp -= player.hand[0].spr;
    }

    [ClientRpc]
    public void RpcDisplayCard(GameObject card, int index)
    {
        if(hasAuthority)
            card.transform.SetParent(playerField.transform.GetChild(4).GetChild(index), false);
        else
            card.transform.SetParent(enemyField.transform.GetChild(4).GetChild(index), false);
    }
}