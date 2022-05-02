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
    public GameObject cardToDraw;
    public GameObject cardToSpawn;
    public bool hasEnemy;
    [SerializeField] Queue<CardInfo> deck;
    [SerializeField] public HandCard[] hand;
    [SerializeField] public FieldCard[] field;
    DeckBuilder deckBuilder;
    public CurrentCard currentCard;
    
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
        currentCard = GameObject.Find("CurrentCard").GetComponent<CurrentCard>();    
        
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
        hand = new HandCard[5];
        field = new FieldCard[5];

        for(int i = 0; i < deckBuilder.Deck.Count; i++)
            deck.Enqueue(new CardInfo(deckBuilder.Deck[i]));  

        deckSize = deck.Count; 

        for(int i = 0; i < 3; i++)
            CmdAddCard(deck.Dequeue(), i); 
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
        if (!localPlayer)
            return;
        if(!hasEnemy)
            UpdateEnemyInfo();

        if(deck != null)
        {
            deckSize = deck.Count; 
            CmdUpdatePlayerText(username,hp,sp,deckSize); 
        }
    }

    public void UpdateEnemyInfo()
    {
        PlayerManager[] onlinePlayers = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager players in onlinePlayers)
        {
            if (players != this)
            {   
                enemy = players;
                hasEnemy = true;

                if(!isServer)
                {
                    HandCard[] cards = FindObjectsOfType<HandCard>();

                    for(int i = 0; i < cards.Length; i++)
                    {
                        enemy.hand[i] = cards[i];
                        cards[i].transform.SetParent(enemyField.transform.GetChild(5).GetChild(i), false);
                        cards[i].cardPosition = i;
                        cards[i].GetComponent<Image>().sprite = cards[i].GetComponent<HandCard>().CardBack;
                    }
                }
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
        fc.attackPattern = card.attackPattern;
        fc.ability = (FieldCard.Ability)card.ability;
        fc.cardPosition = index;
        bc.GetComponent<Image>().sprite = fc.portrait;
        field[index] = fc;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayCard(bc, index);
    }

    [ClientRpc]
    public void RpcDisplayCard(GameObject card, int index)
    {
        if(hasAuthority)
            card.transform.SetParent(playerField.transform.GetChild(4).GetChild(index), false);
        else
            card.transform.SetParent(enemyField.transform.GetChild(4).GetChild(index), false);
    }

    [ClientRpc]
    public void RpcDisplayHand(GameObject card, int index)
    {
        if(hasAuthority)
            card.transform.SetParent(playerField.transform.GetChild(5).GetChild(index), false);
        else
        {
            card.transform.SetParent(enemyField.transform.GetChild(5).GetChild(index), false);
            card.GetComponent<Image>().sprite = card.GetComponent<HandCard>().CardBack;
        }
    }

    public void Draw() 
    {
        if(deck.Count > 0)
        {
            for(int i = 0; i < hand.Length; i++)
            {
                if(hand[i] == null)
                {
                    CmdAddCard(deck.Dequeue(), i);
                    return;
                }
            }

            if(hand.Length < 5)
            {
                Array.Resize(ref hand, hand.Length + 1);
                CmdAddCard(deck.Dequeue(), hand.Length - 1);
                return;
            }
        }
    }

    [Command]
    public void CmdAddCard(CardInfo card, int index)
    {
        GameObject bc = Instantiate(cardToDraw, new Vector3(0, 0, 0), Quaternion.identity);
        HandCard hc = bc.GetComponent<HandCard>();
        hc.cardData = new CardInfo(card.data);
        hc.title = card.title;
        hc.spr = card.spr;
        hc.portrait = card.image;
        hc.cardPosition = index;
        bc.GetComponent<Image>().sprite = hc.portrait;
        hand[index] = hc;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayHand(bc, index);
    }

    [Command]
    public void CmdEndTurn()
    {
        enemy.CmdStartNewTurn();
    }

    [Command(requiresAuthority = false)]
    public void CmdStartNewTurn()
    {
        sp++;
        Draw();

        int[] starting = new int[] {0,0,0,0,0};
        
        for(int i = 0; i < field.Length; i++)
        {
            if(field[i] != null)
                starting[i] = 1;
        }
        
        for(int enumCount = 0; enumCount < Enum.GetNames(typeof(FieldCard.Ability)).Length - 1; enumCount++)
        {
            for(int i = 0; i < field.Length; i++)
            {
                if(field[i] != null && starting[i] == 1 && field[i].priority == enumCount){
                    field[i].UseAbility(this, enemy);
                }
            }
        }
    }
}