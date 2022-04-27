using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;

public class Player : MonoBehaviour
{
    public Queue<Card> deck;
    public Card[] hand;
    public Card[] field;
    public Button[] visibleHand;
    public Button[] visibleField;
    public Card currentCard;
    public Sprite UISprite;
    public TMP_Text deckSize;
    public TMP_Text spSize;
    public TMP_Text hpSize;
    public Image currentTurn;
    public Image tempCard;
    public int hp, sp;
    public bool alreadyPlayed;

    private void Start() 
    {
        deck = new Queue<Card>();
        hand = new Card[3];
        field = new Card[5];
        hp = 10;
        sp = 1;
        alreadyPlayed = false;

        for(int i = 0; i < 20; i++)
            deck.Enqueue(Instantiate(FindObjectOfType<TurnManager>().allCards[UnityEngine.Random.Range(0, FindObjectOfType<TurnManager>().allCards.Count)].GetComponent<Card>()));

        for(int i = 0; i < hand.Length; i++)
            hand[i] = deck.Dequeue();
    }

    private void Update() 
    {
        for(int i = 0; i < hand.Length; i++){
            if(hand[i] != null)
                visibleHand[i].image.sprite = hand[i].portrait;

        }
        deckSize.text = deck.Count.ToString();
        spSize.text = sp.ToString();
        hpSize.text = hp.ToString();
        CheckMoving();
    }

    public void PlayCard(Button button)
    {
        if(FindObjectOfType<TurnManager>().currentPlayer == this)
        {
            var index = Array.IndexOf(visibleHand, button);
            
            if(currentCard != null)
            {
                if(!alreadyPlayed)
                {
                    if(hand[index] == null)
                    {
                        hand[index] = currentCard;
                        visibleHand[index].image.sprite = currentCard.portrait;
                        currentCard = null;
                    }
                }
            }
            else
            {
                currentCard = hand[index];
                hand[index] = null;
                visibleHand[index].image.sprite = UISprite;
            }
        }
    }

    public void MoveToField(Button button) 
    {
        if(FindObjectOfType<TurnManager>().currentPlayer == this)
        {
            if(!visibleField.Contains(button))
                return;

            var index = Array.IndexOf(visibleField, button);

            if (currentCard == null)
            {
                currentCard = field[index];
                field[index] = null;
                visibleField[index].image.sprite = UISprite;
                alreadyPlayed = true;
                return;
            }

            if(alreadyPlayed)
            {
                if(field[index] == null)
                {
                    field[index] = currentCard;
                    field[index].cardPosition = index;
                    visibleField[index].image.sprite = currentCard.portrait;
                    currentCard = null;
                    alreadyPlayed = false;
                }
                else if(currentCard.fusion.title == field[index].title)
                {
                    field[index] = currentCard.evolution;
                    field[index].cardPosition = index;
                    visibleField[index].image.sprite = currentCard.evolution.portrait;
                    currentCard = null;
                    alreadyPlayed = false;
                }
                return;
            }

            if(visibleField[index].image.sprite == UISprite && !alreadyPlayed)
            {
                if(sp >= currentCard.SPR)
                {
                    sp -= currentCard.SPR;
                    field[index] = currentCard;
                    field[index].cardPosition = index;
                    visibleField[index].image.sprite = currentCard.portrait;
                    currentCard = null;
                }    
            }
            else
            {
                if(sp + field[index].SPR >= currentCard.SPR)
                {
                    int spdif = Mathf.Max(0,currentCard.SPR - field[index].SPR);
                    sp -= spdif;
                    field[index] = currentCard;
                    field[index].cardPosition = index;
                    visibleField[index].image.sprite = currentCard.portrait;
                    currentCard = null;
                }
            }      
        }  
    }

    public void CheckMoving()
    {
        if(currentCard != null)
        {          
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0);

            tempCard.GetComponent<RectTransform>().position = fixedPosition;
            
            tempCard.sprite = currentCard.portrait;
            tempCard.enabled = true;
        }
        else
            tempCard.enabled = false; 
    }

    public void Draw() 
    {
        if(deck.Count > 0)
        {
            for(int i = 0; i < hand.Length; i++)
            {
                if(hand[i] == null)
                {
                    hand[i] = deck.Dequeue();
                    return;
                }
            }

            if(hand.Length < 5)
            {
                Array.Resize(ref hand, hand.Length + 1);
                hand[hand.Length - 1] = deck.Dequeue();
                return;
            }
        }
    }
}