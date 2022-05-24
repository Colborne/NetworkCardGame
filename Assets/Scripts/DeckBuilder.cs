using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Mirror;
using UnityEngine.SceneManagement;
using System.IO;

public class DeckBuilder : MonoBehaviour
{
    public TMP_Text DeckSize;
    public Button[] cards;
    public List<ScriptableCard> Deck;
    public NetworkManager manager;
    public TMP_InputField ipaddr;
    public bool isHost = false;

    private void Awake() 
    {
        Deck = new List<ScriptableCard>();
        manager = FindObjectOfType<NetworkManager>();
        List<int> cardCount = SaveDeck.Load();
        if(cardCount != null)
        {
            for(int i = 0; i < cards.Length; i++)
                cards[i].GetComponentInChildren<TMP_Text>().text = cardCount[i].ToString();
        }
    }

    private void Update() 
    {
        int Temp = 0;
        for(int i = 0; i < cards.Length; i++)
            Temp += int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text);
        
        if(Temp.ToString() != DeckSize.text)
            DeckSize.text = "Deck: " + Temp.ToString() + "/40";

        if(!GetComponent<Canvas>().enabled  && !NetworkClient.isConnected && !NetworkServer.active)
        {
            GameObject.Find("GameState").transform.GetChild(5).gameObject.SetActive(true);
            GameObject.Find("GameState").transform.GetChild(6).gameObject.SetActive(true);
        }
    }

    public void SelectCard(TMP_Text _text)
    {
        int i = int.Parse(_text.text);
        if(i < 3)
            i++;
        _text.text = (i % 4).ToString();
    }

    public void DeselectCard(TMP_Text _text)
    {
        int i = int.Parse(_text.text);
        if(i > 0)
            i--;
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
        
        List<int> cardCount = new List<int>();
        for(int i = 0; i < cards.Length; i++)
            cardCount.Add(int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text));
        SaveDeck.Save(cardCount);
    }

    public void Host()
    {
        int temp = 0;
        for(int i = 0; i < cards.Length; i++)
        {
            for(int j = 0; j < int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text); j++)
            {
                temp++;
            }
        }

        if(temp != 40)
            return;

        isHost = true;
        BuildDeck();
        GetComponent<Canvas>().enabled = false;
        manager.StartHost();
    }

    public void Client()
    {
        int temp = 0;
        for(int i = 0; i < cards.Length; i++)
        {
            for(int j = 0; j < int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text); j++)
            {
                temp++;
            }
        }

        if(temp != 40)
            return;

        BuildDeck();

        if(ipaddr.text == string.Empty)
            ipaddr.text = ipaddr.placeholder.GetComponent<TMP_Text>().text;

        AttemptClient();
        GetComponent<Canvas>().enabled = false;
    }

    public void AttemptClient()
    {
        if(GetComponent<Canvas>().enabled)
            manager.networkAddress = ipaddr.text;
        else
           manager.networkAddress = GameObject.Find("GameState").transform.GetChild(6).GetComponent<TMP_InputField>().text;

        if (!NetworkClient.isConnected && !NetworkServer.active)
            ClientConnection();

        if (NetworkClient.isConnected && !NetworkClient.ready)
            NetworkClient.Ready();
    }

    public void ClientConnection()
    {
        GameObject.Find("GameState").GetComponentInChildren<TMP_Text>().text = "Waiting for [" + manager.networkAddress + "]...";
        if (!NetworkClient.active)
            manager.StartClient();
    }

    public void NewDeck()
    {
        if(isHost)
            manager.StopHost();
        else
            manager.StopClient();
        SceneManager.LoadScene("SampleScene");
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Rematch()
    {            
        if(isHost)
        {
            manager.StopHost();
            manager.StartHost();
        }
        else
        {
            manager.StopClient();
            AttemptClient();
        }
    }
}