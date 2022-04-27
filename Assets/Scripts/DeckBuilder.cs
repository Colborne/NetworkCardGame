using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeckBuilder : MonoBehaviour
{
    public TMP_Text DeckSize;
    public Button[] cards;



    private void Update() 
    {
        int Temp = 0;
        for(int i = 0; i < cards.Length; i++)
            Temp += int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text);
        
        if(Temp.ToString() != DeckSize.text)
            DeckSize.text = "Deck Size: " + Temp.ToString();
    }

    public void SelectCard(TMP_Text _text)
    {
        int i = int.Parse(_text.text);
        i++;
        _text.text = (i % 4).ToString();
    }

    public void BuildDeck()
    {
        for(int i = 0; i < cards.Length; i++)
        {
            for(int j = 0; j < int.Parse(cards[i].GetComponent<CardHolder>().amount.text); j++)
            {
                //Deck.Add(cards[i].GetComponent<CardHolder>().card);
            }
        }
    }
}
