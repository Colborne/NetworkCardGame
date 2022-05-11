using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Mirror;
using UnityEngine.SceneManagement;

public class DeckBuilder : MonoBehaviour
{
    public TMP_Text DeckSize;
    public Button[] cards;
    public List<ScriptableCard> Deck;
    public NetworkManager manager;
    public TMP_InputField ipaddr;
    public bool clientCheck = false;
    public bool isHost = false;

    private void Awake() {
        Deck = new List<ScriptableCard>();
        manager = FindObjectOfType<NetworkManager>();
    }
    private void Update() 
    {
        int Temp = 0;
        for(int i = 0; i < cards.Length; i++)
            Temp += int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text);
        
        if(Temp.ToString() != DeckSize.text)
            DeckSize.text = "Deck: " + Temp.ToString() + "/40";

        if(!GetComponent<Canvas>().enabled && !clientCheck && !NetworkClient.isConnected && !NetworkServer.active)
        {
            GameObject.Find("GameState").transform.GetChild(5).gameObject.SetActive(true);
            GameObject.Find("GameState").transform.GetChild(6).gameObject.SetActive(true);
        }
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

        GetComponent<Canvas>().enabled = false;
        AttemptClient();
    }

    public void AttemptClient()
    {
        GameObject.Find("GameState").GetComponentInChildren<TMP_Text>().text = "Waiting for [" + manager.networkAddress + "]...";
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            ClientConnection();
        }

        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            if (GUILayout.Button("Client Ready"))
            {
                NetworkClient.Ready();
                if (NetworkClient.localPlayer == null)
                {
                    NetworkClient.AddPlayer();
                    GameObject.Find("GameState").SetActive(false);
                }
            }
        }
    }

    public void ClientConnection()
    {
        if (!NetworkClient.active)
        {
            manager.StartClient();
        }
        manager.networkAddress = ipaddr.text;
    }

    public void NewDeck()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Rematch()
    {
        Destroy(GameObject.Find("TurnManager").GetComponent<TurnManager>());
        GameObject.Find("TurnManager").AddComponent<TurnManager>();
        if(isHost)
            manager.StartHost();
        else
            AttemptClient();
  
    }
}