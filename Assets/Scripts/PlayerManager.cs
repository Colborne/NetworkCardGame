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
    [SyncVar(hook = nameof(UpdateTurn))] public bool isOurTurn;
    public PlayerManager enemy;
    public static PlayerManager localPlayer;
    public GameObject playerField;
    public GameObject enemyField;
    public GameObject cardToDraw;
    public GameObject cardToSpawn;
    public bool hasEnemy;
    [SerializeField] public Queue<CardInfo> deck;
    [SerializeField] public HandCard[] hand;
    [SerializeField] public FieldCard[] field;
    DeckBuilder deckBuilder;
    TurnManager turnManager;
    public CurrentCard currentCard;
    public Sprite CardBack;
    public GameObject endTurnButton;
    void UpdatePlayerName(string oldUser, string newUser)
    {
        username = newUser;
        gameObject.name = newUser;
    }
    void UpdateTurn(bool oldTurn, bool newTurn)
    {
        isOurTurn = newTurn;
    }
    private void Awake() 
    {
        deckBuilder = FindObjectOfType<DeckBuilder>();
        currentCard = GameObject.Find("CurrentCard").GetComponent<CurrentCard>();
        endTurnButton = GameObject.Find("Canvas/EndTurnButton");
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
        hand = new HandCard[5];
        field = new FieldCard[5];

        for(int i = 0; i < deckBuilder.Deck.Count; i++)
            deck.Enqueue(new CardInfo(deckBuilder.Deck[i]));  

        deckSize = deck.Count; 

        for(int i = 0; i < 3; i++)
            CmdAddCard(deck.Dequeue(), i); 

    }
    private void Update() 
    {
        if(!isServer)
        {
            if (!localPlayer)
                return;
        }
        if(!hasEnemy)
            UpdateEnemyInfo();

        if(deck != null)
        {
            deckSize = deck.Count; 
            CmdUpdatePlayerText(username,hp,sp,deckSize);
        }

        if(endTurnButton != null)
            endTurnButton.SetActive(isOurTurn);

        if(hp <= 0)
        {
            turnManager.canvas.enabled = true;
            turnManager.canvas.GetComponentInChildren<TMP_Text>().text = "You Lost!";
            turnManager.canvas.transform.GetChild(2).gameObject.SetActive(true);
            turnManager.canvas.transform.GetChild(3).gameObject.SetActive(true);
            turnManager.canvas.transform.GetChild(4).gameObject.SetActive(true);
        }  

        if(hasEnemy && enemy.hp <= 0)
        {
            turnManager.canvas.enabled = true;
            turnManager.canvas.GetComponentInChildren<TMP_Text>().text = "You Won!";
            turnManager.canvas.transform.GetChild(2).gameObject.SetActive(true);
            turnManager.canvas.transform.GetChild(3).gameObject.SetActive(true);
            turnManager.canvas.transform.GetChild(4).gameObject.SetActive(true);
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
                        enemy.hand[i] = cards[cards.Length - 1 - i];
                        enemy.hand[i].transform.SetParent(enemyField.transform.GetChild(5).GetChild(i), false);
                        enemy.hand[i].cardPosition = i;
                        enemy.hand[i].GetComponent<Image>().sprite = cards[i].GetComponent<HandCard>().CardBack;
                    }
                }
            }
        }
    }
    public void NewTurn()
    {
        sp++;
        int drawCount = 1;
        for(int i = 0; i < field.Length; i++)
        {
            if(field[i] != null && field[i].ability == FieldCard.Ability.Draw)
                drawCount += field[i].spr;
            if(field[i] != null)
                field[i].defense = 0;
            if(hand[i] != null)
                hand[i].seen = false;
        }

        if(deck.Count > 0)
        {
            for(int i = 0; i < hand.Length; i++)
            {
                if(hand[i] == null && drawCount > 0)
                {
                    CmdAddCard(deck.Dequeue(), i);
                    drawCount--;
                }
            }
        }

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
                if(field[i] != null && starting[i] == 1 && field[i].priority == enumCount && field[i].frozenTime == 0){
                    field[i].UseAbility(this, enemy);
                }
            }
        }
    }
    public void EndTurn()
    {
        for(int i = 0; i < 5; i++)
        {
            if(field[i] != null)
            {
                if(field[i].rotPosition == field[i].cardPosition && field[i].rot)
                {
                    CmdDestroyFieldCard(i);
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == field[i])
                            Destroy(eff.gameObject);
                    }
                }
                else
                {
                    field[i].rot = false;
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == field[i])
                            Destroy(eff.gameObject);
                    }
                }
                if(field[i].frozenTime > 0)
                    field[i].frozenTime--;
                if(field[i].frozenTime == 0)
                {
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == field[i])
                            Destroy(eff.gameObject);
                    }
                }
                if(field[i].ability == FieldCard.Ability.Defend)
                    field[i].EffectSpawn(localPlayer);
            }
            else if(enemy.field[i] != null)
            {
                if(enemy.field[i].frozenTime == 0)
                {
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == enemy.field[i])
                            Destroy(eff.gameObject);
                    }
                }
                if(enemy.field[i].rotPosition == enemy.field[i].cardPosition && enemy.field[i].rot)
                {
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == enemy.field[i])
                            Destroy(eff.gameObject);
                    }
                }
                else
                {
                    enemy.field[i].rot = false;
                    LingeringEffect[] effects = FindObjectsOfType<LingeringEffect>();
                    foreach(LingeringEffect eff in effects)
                    {
                        if(eff.target == enemy.field[i])
                            Destroy(eff.gameObject);
                    }
                }
            }
        }
    }
    public void SelectCard(int index)
    {   
        if(currentCard.alreadyPlayed == false)
        {
            if(currentCard.portrait != null)
            {
                if(hand[index] == null)
                {
                    CmdAddCard(currentCard.cardData, index);
                    currentCard.GetComponent<Image>().enabled = false;
                    currentCard.cardData = new CardInfo();
                    currentCard.portrait = null;
                }
            }
            else
            {
                currentCard.cardData = playerField.transform.GetChild(5).GetChild(index).GetComponentInChildren<HandCard>().cardData;
                currentCard.portrait = playerField.transform.GetChild(5).GetChild(index).GetComponentInChildren<HandCard>().cardData.image;
                currentCard.GetComponent<Image>().enabled = true;
                currentCard.GetComponent<Image>().sprite = currentCard.portrait;
                CmdDestroyHandCard(index);
            }   
        } 
    }
    public void SelectFieldCard(int index)
    {   
        if(playerField.transform.GetChild(4).GetChild(index).GetComponentInChildren<FieldCard>().frozenTime == 0)
        {
            currentCard.cardData = playerField.transform.GetChild(4).GetChild(index).GetComponentInChildren<FieldCard>().cardData;
            currentCard.portrait = playerField.transform.GetChild(4).GetChild(index).GetComponentInChildren<FieldCard>().cardData.image;
            currentCard.alreadyPlayed = true;
            currentCard.GetComponent<Image>().enabled = true;
            currentCard.GetComponent<Image>().sprite = currentCard.portrait;
            CmdDestroyFieldCard(index);
        }
    }
    [Command] public void CmdUpdatePlayerText(string u, int h, int s, int d)
    {
        RpcUpdatePlayerText(u,h,s,d);
    }
    [Command(requiresAuthority = false)] public void CmdDestroyHandCard(int index)
    {
        RpcDestroyHandCard(index);
    }
    [Command(requiresAuthority = false)] public void CmdDestroyFieldCard(int index)
    {
        RpcDestroyFieldCard(index);
    }
    [Command] public void CmdAddCard(CardInfo card, int index)
    {
        GameObject bc = Instantiate(cardToDraw, new Vector3(0, 0, 0), Quaternion.identity);
        HandCard hc = bc.GetComponent<HandCard>();
        hc.cardData = new CardInfo(card.data);
        hc.title = card.title;
        hc.spr = card.spr;
        hc.portrait = card.image;
        hc.cardPosition = index;
        bc.GetComponent<Image>().sprite = hc.CardBack;
        hand[index] = hc;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayHand(bc, index);
    }
    [Command] public void CmdPlayCard(CardInfo card, int index)
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
        fc.effect = card.effect;
        fc.spawn = card.spawn;
        bc.GetComponent<Image>().sprite = fc.portrait;
        field[index] = fc;
        NetworkServer.Spawn(bc);

        if(isServer) RpcDisplayCard(bc, index);
    }
    [Command] public void CmdLoadPlayer(string user, int health, int sum, int deck)
    {
        username = user;
        hp = health;
        sp = sum;
        deckSize = deck;
    }
    [Command] public void CmdSetMana(int value)
    {
        sp += value;
    }
    [ClientRpc] public void RpcDestroyHandCard(int index)
    {
        if(hasAuthority)
        {
            if(playerField.transform.GetChild(5).GetChild(index).childCount > 0)
                Destroy(playerField.transform.GetChild(5).GetChild(index).GetChild(0).gameObject);
            hand[index] = null;
        }
        else
        {
            if(enemyField.transform.GetChild(5).GetChild(index).childCount > 0)
                Destroy(enemyField.transform.GetChild(5).GetChild(index).GetChild(0).gameObject);
        }
    }
    [ClientRpc] public void RpcDestroyFieldCard(int index)
    {
        if(hasAuthority)
        {
            if(playerField.transform.GetChild(4).GetChild(index).childCount > 0)
                Destroy(playerField.transform.GetChild(4).GetChild(index).GetChild(0).gameObject);
            field[index] = null;
        }
        else
        {
            if(enemyField.transform.GetChild(4).GetChild(index).childCount > 0)
                Destroy(enemyField.transform.GetChild(4).GetChild(index).GetChild(0).gameObject);
        }
    }
    [ClientRpc] public void RpcDisplayCard(GameObject card, int index)
    {
        if(hasAuthority)
            card.transform.SetParent(playerField.transform.GetChild(4).GetChild(index), false);
        else
            card.transform.SetParent(enemyField.transform.GetChild(4).GetChild(index), false);
    }
    [ClientRpc] public void RpcDisplayHand(GameObject card, int index)
    {
        if(hasAuthority){
            card.transform.SetParent(playerField.transform.GetChild(5).GetChild(index), false);
            card.GetComponent<Image>().sprite = card.GetComponent<HandCard>().portrait;
        }
        else{
            card.transform.SetParent(enemyField.transform.GetChild(5).GetChild(index), false);
            card.GetComponent<Image>().sprite = card.GetComponent<HandCard>().CardBack;
        }
    }
    [ClientRpc] public void RpcUpdatePlayerText(string u, int h, int s, int d)
    {
        if(hasAuthority)
        {
            playerField.transform.GetChild(0).GetComponent<TMP_Text>().text = u;
            playerField.transform.GetChild(1).GetComponent<TMP_Text>().text = h.ToString();
            playerField.transform.GetChild(2).GetComponent<TMP_Text>().text = s.ToString();
            playerField.transform.GetChild(3).GetComponent<TMP_Text>().text = d.ToString();
            
            for(int i = 0; i < 5; i++)
            {
                if(playerField.transform.GetChild(5).GetChild(i).childCount > 0)
                    localPlayer.hand[i] = playerField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<HandCard>();
                else
                    localPlayer.hand[i] = null;

                if(playerField.transform.GetChild(4).GetChild(i).childCount > 0)
                    localPlayer.field[i] = playerField.transform.GetChild(4).GetChild(i).GetChild(0).GetComponent<FieldCard>();
                 else
                    localPlayer.field[i] = null;

                if(hasEnemy)
                {
                    if(enemyField.transform.GetChild(5).GetChild(i).childCount > 0)
                        localPlayer.enemy.hand[i] = enemyField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<HandCard>();               
                    else
                        localPlayer.enemy.hand[i] = null;

                    if(enemyField.transform.GetChild(4).GetChild(i).childCount > 0)
                        localPlayer.enemy.field[i] = enemyField.transform.GetChild(4).GetChild(i).GetChild(0).GetComponent<FieldCard>();                
                    else
                        localPlayer.enemy.field[i] = null;
                }
            }
        }
        else
        {
            enemyField.transform.GetChild(0).GetComponent<TMP_Text>().text = u;
            enemyField.transform.GetChild(1).GetComponent<TMP_Text>().text = h.ToString();
            enemyField.transform.GetChild(2).GetComponent<TMP_Text>().text = s.ToString();
            enemyField.transform.GetChild(3).GetComponent<TMP_Text>().text = d.ToString();

            for(int i = 0; i < 5; i++)
            {
                if(enemyField.transform.GetChild(5).GetChild(i).childCount > 0){
                    enemyField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<Image>().sprite = CardBack;
                    if(enemyField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<HandCard>().seen)
                        enemyField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<Image>().sprite = enemyField.transform.GetChild(5).GetChild(i).GetChild(0).GetComponent<HandCard>().portrait;
                }
            }
        }
    }
}