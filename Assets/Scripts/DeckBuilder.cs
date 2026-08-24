using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using PurrNet;
using PurrNet.Transports;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class DeckBuilder : MonoBehaviour
{
    public TMP_Text DeckSize;
    public TMP_Text description;
    public Button[] cards;
    public List<ScriptableCard> Deck;
    public NetworkManager manager;
    [SerializeField] private PlayerManager offlinePlayerPrefab;
    public TMP_InputField ipaddr;
    public bool isHost = false;

    private const int RequiredDeckSize = 40;
    private const int MaximumCopiesPerCard = 3;

    private bool offlineAIMode;
    private PlayerManager offlineHuman;
    private PlayerManager offlineAI;

    private void Awake() 
    {
        Deck = new List<ScriptableCard>();
        manager = FindFirstObjectByType<NetworkManager>();
        // Older saves may contain fewer entries when new cards are added.
        List<int> cardCount = SaveDeck.Load();
        if (cardCount != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (!cards[i])
                    continue;

                TMP_Text countText = cards[i].GetComponentInChildren<TMP_Text>();
                if (!countText)
                    continue;

                int savedCount = i < cardCount.Count ? cardCount[i] : 0;
                countText.text = Mathf.Clamp(savedCount, 0, 3).ToString();
            }
        }

    }

    private void Update() 
    {
        int Temp = 0;
        for(int i = 0; i < cards.Length; i++)
            Temp += int.Parse(cards[i].GetComponentInChildren<TMP_Text>().text);
        
        if(Temp.ToString() != DeckSize.text)
            DeckSize.text = "Deck: " + Temp.ToString() + "/40";

        if(!offlineAIMode && !GetComponent<Canvas>().enabled && IsDisconnected())
        {
            GameObject.Find("GameState").transform.GetChild(5).gameObject.SetActive(true);
            GameObject.Find("GameState").transform.GetChild(6).gameObject.SetActive(true);
        }

        RaycastDescription();
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
        if (SelectedCardCount() != RequiredDeckSize)
            return;

        isHost = true;
        BuildDeck();
        BattleLog.BeginBattle("Online Battle");
        BattleLog.Record("Hosting a match and waiting for an opponent.");
        GetComponent<Canvas>().enabled = false;
        if (manager)
            manager.StartHost();
    }

    public void Client()
    {
        if (SelectedCardCount() != RequiredDeckSize)
            return;

        BuildDeck();
        BattleLog.BeginBattle("Online Battle");
        BattleLog.Record("Connecting to the host.");

        if(ipaddr.text == string.Empty)
            ipaddr.text = ipaddr.placeholder.GetComponent<TMP_Text>().text;

        AttemptClient();
        GetComponent<Canvas>().enabled = false;
    }

    public void VsAI()
    {
        if (SelectedCardCount() != RequiredDeckSize)
        {
            SetDescription("Build a 40-card deck before starting a battle.");
            return;
        }

        if (!offlinePlayerPrefab)
        {
            Debug.LogError("DeckBuilder needs an offline PlayerManager prefab before Vs AI can start.", this);
            return;
        }

        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (!turnManager)
        {
            Debug.LogError("No TurnManager was found for the offline battle.", this);
            return;
        }

        StopSession();
        BuildDeck();

        List<ScriptableCard> aiDeck = BuildRandomAIDeck();
        if (aiDeck.Count != RequiredDeckSize)
        {
            Debug.LogError("The available card pool could not create a legal 40-card AI deck.", this);
            return;
        }

        offlineAIMode = true;
        GetComponent<Canvas>().enabled = false;
        HideConnectionStatus(turnManager);
        StartOfflineBattle(turnManager, aiDeck);
    }

    public void AttemptClient()
    {
        if (!manager)
            return;

        string address = GetComponent<Canvas>().enabled
            ? ipaddr.text
            : GameObject.Find("GameState").transform.GetChild(6).GetComponent<TMP_InputField>().text;

        if (manager.transport is UDPTransport udpTransport)
            udpTransport.address = address;

        if (IsDisconnected())
            ClientConnection();
    }

    public void ClientConnection()
    {
        if (!manager)
            return;

        string address = manager.transport is UDPTransport udpTransport ? udpTransport.address : ipaddr.text;
        GameObject.Find("GameState").GetComponentInChildren<TMP_Text>().text = "Waiting for [" + address + "]...";
        if (manager.clientState == ConnectionState.Disconnected)
            manager.StartClient();
    }

    public void NewDeck()
    {
        StopSession();
        SceneManager.LoadScene("SampleScene");
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Rematch()
    {
        if (offlineAIMode)
            StartCoroutine(RestartOfflineBattle());
        else
            StartCoroutine(RestartSession());
    }

    public void ToggleRules()
    {
        GameObject.Find("Rules").GetComponent<Canvas>().enabled = !GameObject.Find("Rules").GetComponent<Canvas>().enabled;
    }

    public void RaycastDescription()
    {
        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector2.zero);
        
        if(hit.collider != null)
        {
            description.text = hit.collider.GetComponent<CardHolder>().card.title + "\n"
                + hit.collider.GetComponent<CardHolder>().card.description;
        }
        else
            description.text = "";
    }

    private bool IsDisconnected()
    {
        return !manager || (manager.clientState == ConnectionState.Disconnected &&
                            manager.serverState == ConnectionState.Disconnected);
    }

    private int SelectedCardCount()
    {
        int count = 0;
        foreach (Button cardButton in cards)
        {
            if (!cardButton)
                continue;

            TMP_Text countText = cardButton.GetComponentInChildren<TMP_Text>();
            if (countText && int.TryParse(countText.text, out int copies))
                count += copies;
        }

        return count;
    }

    private List<ScriptableCard> BuildRandomAIDeck()
    {
        List<ScriptableCard> pool = cards
            .Where(button => button)
            .Select(button => button.GetComponent<CardHolder>())
            .Where(holder => holder && holder.card)
            .Select(holder => holder.card)
            .GroupBy(card => card.cardID)
            .Select(group => group.First())
            .ToList();

        List<ScriptableCard> result = new List<ScriptableCard>(RequiredDeckSize);
        Dictionary<int, int> copies = new Dictionary<int, int>();

        while (result.Count < RequiredDeckSize)
        {
            List<ScriptableCard> choices = pool
                .Where(card => !copies.TryGetValue(card.cardID, out int count) || count < MaximumCopiesPerCard)
                .ToList();

            if (choices.Count == 0)
                break;

            ScriptableCard chosen = ChooseWeightedRandomCard(choices);
            result.Add(chosen);
            copies[chosen.cardID] = copies.TryGetValue(chosen.cardID, out int count) ? count + 1 : 1;
        }

        return result.OrderBy(_ => UnityEngine.Random.value).ToList();
    }

    private static ScriptableCard ChooseWeightedRandomCard(IReadOnlyList<ScriptableCard> choices)
    {
        float totalWeight = 0f;
        for (int i = 0; i < choices.Count; i++)
            totalWeight += DeckWeight(choices[i]);

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < choices.Count; i++)
        {
            roll -= DeckWeight(choices[i]);
            if (roll <= 0f)
                return choices[i];
        }

        return choices[choices.Count - 1];
    }

    private static float DeckWeight(ScriptableCard card)
    {
        float curveWeight = card.spr switch
        {
            <= 1 => 1.35f,
            2 => 1.55f,
            3 => 1.2f,
            4 => 0.8f,
            _ => 0.55f
        };

        if (card.ability == FieldCard.Ability.Summoning ||
            card.ability == FieldCard.Ability.ManaBoost ||
            card.ability == FieldCard.Ability.Draw)
            curveWeight *= 1.2f;

        return curveWeight;
    }

    private string ResolvePlayerName()
    {
        TMP_InputField[] inputs = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        foreach (TMP_InputField input in inputs)
        {
            if (input && input.gameObject.name.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !string.IsNullOrWhiteSpace(input.text))
                return input.text;
        }

        return "Player";
    }

    private void SetDescription(string message)
    {
        if (description)
            description.text = message;
    }

    private void StopSession()
    {
        if (!manager)
            return;

        if (manager.clientState != ConnectionState.Disconnected)
            manager.StopClient();
        if (manager.serverState != ConnectionState.Disconnected)
            manager.StopServer();
    }

    private IEnumerator RestartSession()
    {
        StopSession();
        while (!IsDisconnected())
            yield return null;

        if (isHost)
            manager.StartHost();
        else
            AttemptClient();
    }

    private IEnumerator RestartOfflineBattle()
    {
        CleanupOfflinePlayer(offlineHuman);
        CleanupOfflinePlayer(offlineAI);
        yield return null;

        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (!turnManager)
            yield break;

        Deck = Deck.OrderBy(_ => UnityEngine.Random.value).ToList();
        List<ScriptableCard> aiDeck = BuildRandomAIDeck();
        if (aiDeck.Count == RequiredDeckSize)
            StartOfflineBattle(turnManager, aiDeck);
    }

    private void StartOfflineBattle(TurnManager turnManager, List<ScriptableCard> aiDeck)
    {
        HideConnectionStatus(turnManager);

        string humanName = ResolvePlayerName();
        BattleLog.BeginBattle($"{humanName} vs Strategist AI");

        offlineHuman = UnityProxy.InstantiateDirectly(offlinePlayerPrefab);
        offlineAI = UnityProxy.InstantiateDirectly(offlinePlayerPrefab);
        offlineHuman.name = "Offline Human";
        offlineAI.name = "Offline AI";

        offlineHuman.InitializeOffline(humanName, Deck, true);
        offlineAI.InitializeOffline("Strategist AI", aiDeck, false);
        turnManager.StartOfflineBattle(offlineHuman, offlineAI);

        OfflineAIController controller = offlineAI.gameObject.AddComponent<OfflineAIController>();
        controller.Initialize(offlineAI, offlineHuman, turnManager);
    }

    private static void HideConnectionStatus(TurnManager turnManager)
    {
        if (!turnManager || !turnManager.canvas)
            return;

        Transform status = turnManager.canvas.transform;
        if (status.childCount > 5)
            status.GetChild(5).gameObject.SetActive(false);
        if (status.childCount > 6)
            status.GetChild(6).gameObject.SetActive(false);
        turnManager.canvas.enabled = false;
    }

    private static void CleanupOfflinePlayer(PlayerManager player)
    {
        if (!player)
            return;

        for (int i = 0; i < 5; i++)
        {
            player.RequestDestroyHandCard(i);
            player.RequestDestroyFieldCard(i);
        }

        UnityProxy.DestroyDirectly(player.gameObject);
    }
}
