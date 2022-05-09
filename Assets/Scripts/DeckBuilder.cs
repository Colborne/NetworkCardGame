using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class DeckBuilder : MonoBehaviour
{
    public TMP_Text DeckSize;
    public Button[] cards;
    public List<ScriptableCard> Deck;

    private void Awake() {
        Deck = new List<ScriptableCard>();
    }
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
        List<ScriptableCard> temp = new List<ScriptableCard>();
        for(int i = 0; i < cards.Length; i++)
        {
            for(int j = 0; j < int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text); j++)
                temp.Add(cards[i].GetComponent<CardHolder>().card);
        }
        System.Random rng = new System.Random();
        Deck = temp.OrderBy(x => rng.Next()).ToList();
    }

    public void CloseCanvas()
    {
        BuildDeck();
        GetComponent<Canvas>().enabled = false;
    }
}
