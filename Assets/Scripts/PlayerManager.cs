using System;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PlayerInfo
{
    private GameObject player;

    public PlayerInfo(GameObject player)
    {
        this.player = player;
    }

    public PlayerManager data => player.GetComponent<PlayerManager>();
    public string user => data.username;
    public int hp => data.hp;
    public int sp => data.sp;
    public int deckSize => data.deckSize;
}

public class PlayerManager : NetworkBehaviour
{
    private const int SlotCount = 5;
    private const int StartingHealth = 20;
    private const int StartingMana = 1;
    private const int StartingHandSize = 3;

    private readonly SyncVar<string> syncedUsername = new(string.Empty);
    private readonly SyncVar<int> syncedHealth = new(StartingHealth);
    private readonly SyncVar<int> syncedMana = new(StartingMana);
    private readonly SyncVar<int> syncedDeckSize = new();
    private readonly SyncVar<bool> syncedTurn = new();
    private readonly SyncVar<bool> syncedReady = new();

    public string username => syncedUsername.value;
    public int hp => syncedHealth.value;
    public int sp => syncedMana.value;
    public int deckSize => syncedDeckSize.value;
    public bool isOurTurn => syncedTurn.value;
    public bool isReady => syncedReady.value;
    public bool isResolvingActions { get; private set; }
    public string actionName => string.IsNullOrWhiteSpace(username) ? "Player" : username;

    public PlayerManager enemy;
    public static PlayerManager Local { get; private set; }
    public GameObject playerField;
    public GameObject enemyField;
    public GameObject cardToDraw;
    public GameObject cardToSpawn;
    public bool hasEnemy;
    [SerializeField] public Queue<CardInfo> deck;
    [SerializeField] public HandCard[] hand;
    [SerializeField] public FieldCard[] field;
    public CurrentCard currentCard;
    public Sprite CardBack;
    public GameObject endTurnButton;
    public TurnManager turnManager;

    [SerializeField, Min(0.05f)] private float actionDelay = BattleLog.DefaultActionDelaySeconds;

    private DeckBuilder deckBuilder;
    private GameManager gameManager;
    private bool ownerInitialized;
    private bool offlineParticipant;
    private bool offlineLocalHuman;
    private bool resultShown;

    private bool IsOnline => isSpawned;
    private bool UsesLocalBoard => IsOnline ? isOwner : offlineLocalHuman;

    private void OnUsernameChanged(string value)
    {
        gameObject.name = string.IsNullOrWhiteSpace(value) ? "Player" : value;
        RefreshPlayerText();
    }

    private void OnTurnChanged(bool value)
    {
        if (UsesLocalBoard && endTurnButton)
            endTurnButton.SetActive(value && !isResolvingActions);
    }

    private void OnStatChanged(int _)
    {
        RefreshPlayerText();
    }

    private void OnReadyChanged(bool _)
    {
        RefreshPlayerText();
    }

    private void Awake()
    {
        deck = new Queue<CardInfo>();
        hand = new HandCard[SlotCount];
        field = new FieldCard[SlotCount];

        syncedUsername.onChanged += OnUsernameChanged;
        syncedHealth.onChanged += OnStatChanged;
        syncedMana.onChanged += OnStatChanged;
        syncedDeckSize.onChanged += OnStatChanged;
        syncedTurn.onChanged += OnTurnChanged;
        syncedReady.onChanged += OnReadyChanged;

        ResolveSceneReferences();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        ResolveSceneReferences();
        TryInitializeOwner();
        RefreshPlayerText();
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        base.OnOwnerChanged(oldOwner, newOwner, asServer);

        if (!asServer)
            TryInitializeOwner();
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();

        if (Local == this)
            Local = null;
    }

    protected override void OnDestroy()
    {
        if (Local == this)
            Local = null;

        base.OnDestroy();
    }

    private void ResolveSceneReferences()
    {
        if (!deckBuilder)
            deckBuilder = FindFirstObjectByType<DeckBuilder>();
        if (!gameManager)
            gameManager = FindFirstObjectByType<GameManager>();
        if (!turnManager)
            turnManager = FindFirstObjectByType<TurnManager>();
        if (!currentCard)
        {
            GameObject current = GameObject.Find("CurrentCard");
            if (current)
                currentCard = current.GetComponent<CurrentCard>();
        }
        if (!endTurnButton)
            endTurnButton = GameObject.Find("Canvas/EndTurnButton");
        if (!playerField)
            playerField = GameObject.Find("Player Field");
        if (!enemyField)
            enemyField = GameObject.Find("Enemy Field");
    }

    private void TryInitializeOwner()
    {
        if (ownerInitialized || !isOwner)
            return;

        ownerInitialized = true;
        offlineParticipant = false;
        offlineLocalHuman = true;
        Local = this;

        if (gameManager)
            gameManager.player = this;

        List<ScriptableCard> selectedDeck = deckBuilder ? deckBuilder.Deck : null;
        CardInfo[] preparedDeck = BuildDeckData(selectedDeck);
        RequestInitializeOnlinePlayer(ResolveUsername(), preparedDeck);
    }

    private string ResolveUsername()
    {
        TMP_InputField[] inputs = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        foreach (TMP_InputField input in inputs)
        {
            if (input && input.gameObject.name.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0)
                return input.text;
        }

        return inputs.Length > 0 ? inputs[0].text : "Player";
    }

    private static CardInfo[] BuildDeckData(IReadOnlyList<ScriptableCard> cards)
    {
        if (cards == null)
            return Array.Empty<CardInfo>();

        CardInfo[] result = new CardInfo[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            result[i] = new CardInfo(cards[i]);
        return result;
    }

    public void InitializeOffline(string playerName, IReadOnlyList<ScriptableCard> preparedDeck, bool localHuman)
    {
        if (isSpawned)
        {
            Debug.LogWarning("A spawned network player cannot be converted into an offline participant.", this);
            return;
        }

        ResolveSceneReferences();
        offlineParticipant = true;
        offlineLocalHuman = localHuman;
        ownerInitialized = true;

        if (localHuman)
        {
            Local = this;
            if (gameManager)
                gameManager.player = this;
        }

        InitializeState(playerName, BuildDeckData(preparedDeck));
        DrawStartingHand();
        RefreshPlayerText();
    }

    private void Update()
    {
        ResolveSceneReferences();

        if (IsOnline && !isOwner)
        {
            RefreshPlayerText();
            return;
        }

        if (!IsOnline && !offlineParticipant)
            return;

        if (!hasEnemy)
            UpdateEnemyInfo();

        RefreshBoardReferences();
        RefreshPlayerText();

        if (UsesLocalBoard && endTurnButton)
            endTurnButton.SetActive(isOurTurn && !isResolvingActions);

        if (!resultShown && UsesLocalBoard && hp <= 0)
            ShowResult("You Lost!");
        else if (!resultShown && UsesLocalBoard && enemy && enemy.hp <= 0)
            ShowResult("You Won!");
    }

    private void ShowResult(string message)
    {
        if (!gameManager || !gameManager.canvas)
            return;

        resultShown = true;
        RecordAction($"<color=#FFD56A><b>{message}</b></color>");
        gameManager.canvas.enabled = true;
        gameManager.canvas.GetComponentInChildren<TMP_Text>().text = message;
        SetCanvasChildActive(2, true);
        SetCanvasChildActive(4, true);
        SetCanvasChildActive(5, IsOnline);
        SetCanvasChildActive(6, IsOnline);
    }

    private void SetCanvasChildActive(int index, bool value)
    {
        if (gameManager.canvas.transform.childCount > index)
            gameManager.canvas.transform.GetChild(index).gameObject.SetActive(value);
    }

    public void UpdateEnemyInfo()
    {
        PlayerManager[] players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        foreach (PlayerManager candidate in players)
        {
            if (candidate == this || (IsOnline && !candidate.isSpawned))
                continue;

            enemy = candidate;
            hasEnemy = true;
            break;
        }
    }

    public void NewTurn()
    {
        if ((IsOnline && !isOwner) || isResolvingActions)
            return;

        StartCoroutine(ResolveNewTurn());
    }

    private IEnumerator ResolveNewTurn()
    {
        isResolvingActions = true;
        RecordAction($"<color=#8FD3FF><b>{actionName}'s turn begins.</b></color>");

        RequestAdjustMana(1);
        yield return WaitForAction();
        int drawCount = 1;

        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] && field[i].ability == FieldCard.Ability.Draw && field[i].frozenTimer == 0)
                drawCount += field[i].spr;
            if (field[i])
                RequestSetDefense(i, 0, true);
            if (hand[i])
                RequestSetHandSeen(i, false);
        }

        for (int i = 0; i < hand.Length && drawCount > 0; i++)
        {
            if (!hand[i] && deckSize > 0)
            {
                RequestDrawCard(i);
                drawCount--;
                yield return WaitForAction();
            }
        }

        int[] starting = new int[SlotCount];
        for (int i = 0; i < field.Length; i++)
            starting[i] = field[i] ? 1 : 0;

        for (int priority = 0; priority < Enum.GetNames(typeof(FieldCard.Ability)).Length - 1; priority++)
        {
            for (int i = 0; i < field.Length; i++)
            {
                if (field[i] && starting[i] == 1 && field[i].priority == priority &&
                    field[i].frozenTimer == 0 && field[i].ability != FieldCard.Ability.Defend)
                {
                    yield return field[i].UseAbility(this, enemy);
                    yield return WaitForAction();
                }
            }
        }

        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] && field[i].ability == FieldCard.Ability.Bomb)
            {
                yield return field[i].UseAbility(this, enemy);
                yield return WaitForAction();
            }
        }

        isResolvingActions = false;
    }

    public IEnumerator ResolveEndTurn()
    {
        if (isResolvingActions)
            yield break;

        isResolvingActions = true;
        for (int i = 0; i < SlotCount; i++)
        {
            if (field[i])
            {
                Slot slot = field[i].GetComponentInParent<Slot>();
                if (slot && slot.rot)
                {
                    RequestDestroyFieldCard(i);
                    RequestSetRot(i, false);
                    yield return WaitForAction();
                    continue;
                }

                if (field[i].frozenTimer > 0)
                {
                    RequestSetFrozen(i, field[i].frozenTimer - 1);
                    yield return WaitForAction();
                }

                FieldCard.Ability ability = field[i].ability;
                if (ability == FieldCard.Ability.Defend || ability == FieldCard.Ability.DeckCard ||
                    ability == FieldCard.Ability.Sacrifice || ability == FieldCard.Ability.Blitz ||
                    ability == FieldCard.Ability.Freeze || ability == FieldCard.Ability.ReturnToDeck)
                {
                    yield return field[i].UseAbility(this, enemy);
                    yield return WaitForAction();
                }
            }

            RequestSetRot(i, false);
        }

        RecordAction($"<color=#AEB8C8>{actionName} ends the turn.</color>");
        yield return WaitForAction();
        isResolvingActions = false;
    }

    private WaitForSeconds WaitForAction()
    {
        return new WaitForSeconds(actionDelay);
    }

    public void PauseAfterInteraction()
    {
        if (!isResolvingActions)
            StartCoroutine(PauseInteraction());
    }

    private IEnumerator PauseInteraction()
    {
        isResolvingActions = true;
        yield return WaitForAction();
        isResolvingActions = false;
    }

    public IEnumerator PlayAbilityFeedback(int sourceSlot, int[] opponentSlots, int[] friendlySlots)
    {
        if (!IsValidSlot(sourceSlot))
            yield break;

        opponentSlots ??= Array.Empty<int>();
        friendlySlots ??= Array.Empty<int>();

        if (!IsOnline)
        {
            yield return AnimateAbilityFeedback(sourceSlot, opponentSlots, friendlySlots);
            yield break;
        }

        if (isServer)
            ObserversPlayAbilityFeedback(sourceSlot, opponentSlots, friendlySlots);
        else
            ServerPlayAbilityFeedback(sourceSlot, opponentSlots, friendlySlots);

        yield return new WaitForSecondsRealtime(CardGameFeel.AbilityAnimationSeconds);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerPlayAbilityFeedback(int sourceSlot, int[] opponentSlots, int[] friendlySlots)
    {
        ObserversPlayAbilityFeedback(sourceSlot, opponentSlots, friendlySlots);
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversPlayAbilityFeedback(int sourceSlot, int[] opponentSlots, int[] friendlySlots)
    {
        if (isServer && !isClient)
            return;

        StartCoroutine(AnimateAbilityFeedback(sourceSlot, opponentSlots, friendlySlots));
    }

    private IEnumerator AnimateAbilityFeedback(int sourceSlot, int[] opponentSlots, int[] friendlySlots)
    {
        RectTransform source = ResolveFieldVisual(false, sourceSlot);
        if (!source)
            yield break;

        List<RectTransform> targets = new List<RectTransform>();
        AddVisualTargets(targets, true, opponentSlots);
        AddVisualTargets(targets, false, friendlySlots);
        yield return CardGameFeel.AnimateAbility(source, targets);
    }

    private void AddVisualTargets(List<RectTransform> targets, bool opponent, int[] slots)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform target = ResolveFieldVisual(opponent, slots[i]);
            if (target)
                targets.Add(target);
        }
    }

    private RectTransform ResolveFieldVisual(bool opponent, int slot)
    {
        if (!IsValidSlot(slot))
            return null;

        ResolveSceneReferences();
        GameObject board = opponent
            ? (UsesLocalBoard ? enemyField : playerField)
            : (UsesLocalBoard ? playerField : enemyField);
        if (!board || board.transform.childCount <= 4)
            return null;

        Transform fieldRoot = board.transform.GetChild(4);
        if (fieldRoot.childCount <= slot)
            return null;

        Transform slotTransform = fieldRoot.GetChild(slot);
        Transform visual = slotTransform.childCount > 0 ? slotTransform.GetChild(0) : slotTransform;
        return visual as RectTransform;
    }

    public void SelectCard(int index)
    {
        if (!IsValidSlot(index) || !currentCard || currentCard.alreadyPlayed)
            return;

        if (currentCard.portrait)
        {
            if (!hand[index])
            {
                RequestAddCard(currentCard.cardData, index);
                ClearCurrentCard(false);
            }
            return;
        }

        if (!hand[index])
            return;

        currentCard.cardData = hand[index].cardData;
        currentCard.portrait = hand[index].cardData.image;
        currentCard.GetComponent<Image>().enabled = true;
        currentCard.GetComponent<Image>().sprite = currentCard.portrait;
        RequestDestroyHandCard(index);
    }

    public void SelectFieldCard(int index)
    {
        if (!IsValidSlot(index) || !field[index] || !currentCard)
            return;

        currentCard.cardData = field[index].cardData;
        currentCard.portrait = field[index].cardData.image;
        currentCard.alreadyPlayed = true;
        currentCard.GetComponent<Image>().enabled = true;
        currentCard.GetComponent<Image>().sprite = currentCard.portrait;
        RequestDestroyFieldCard(index);
    }

    private void ClearCurrentCard(bool alreadyPlayed)
    {
        currentCard.GetComponent<Image>().enabled = false;
        currentCard.cardData = new CardInfo();
        currentCard.portrait = null;
        currentCard.alreadyPlayed = alreadyPlayed;
    }

    private static bool IsValidSlot(int index)
    {
        return index >= 0 && index < SlotCount;
    }

    private void RequestInitializeOnlinePlayer(string playerName, CardInfo[] preparedDeck)
    {
        ServerInitializePlayer(playerName, preparedDeck);
    }

    [ServerRpc]
    private void ServerInitializePlayer(string playerName, CardInfo[] preparedDeck)
    {
        InitializeState(playerName, preparedDeck);
        DrawStartingHand();
    }

    private void InitializeState(string playerName, CardInfo[] preparedDeck)
    {
        syncedUsername.value = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        syncedHealth.value = StartingHealth;
        syncedMana.value = StartingMana;
        syncedTurn.value = false;

        deck.Clear();
        if (preparedDeck != null)
        {
            foreach (CardInfo card in preparedDeck)
                deck.Enqueue(card);
        }

        syncedDeckSize.value = deck.Count;
        syncedReady.value = true;
        RecordAction($"{actionName} enters the battle with {deck.Count} cards.");
    }

    private void DrawStartingHand()
    {
        for (int i = 0; i < StartingHandSize && deck.Count > 0; i++)
            DrawCardAuthoritative(i, false);
        syncedDeckSize.value = deck.Count;
    }

    public void RequestDrawCard(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            DrawCardAuthoritative(index, false);
        else
            ServerDrawCard(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerDrawCard(int index)
    {
        DrawCardAuthoritative(index, false);
    }

    public void RequestDrawCardToField(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            DrawCardAuthoritative(index, true);
        else
            ServerDrawCardToField(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerDrawCardToField(int index)
    {
        DrawCardAuthoritative(index, true);
    }

    public void RequestReplaceFieldWithDeckCard(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            ReplaceFieldWithDeckCardAuthoritative(index);
        else
            ServerReplaceFieldWithDeckCard(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerReplaceFieldWithDeckCard(int index)
    {
        ReplaceFieldWithDeckCardAuthoritative(index);
    }

    private void ReplaceFieldWithDeckCardAuthoritative(int index)
    {
        if (deck.Count == 0)
            return;

        DestroyFieldCardAuthoritative(index);
        DrawCardAuthoritative(index, true);
    }

    private void DrawCardAuthoritative(int index, bool toField)
    {
        if (!IsValidSlot(index) || deck.Count == 0)
            return;

        if (toField)
        {
            if (field[index])
                return;
            SpawnFieldCardAuthoritative(deck.Dequeue(), index);
        }
        else
        {
            if (hand[index])
                return;
            SpawnHandCardAuthoritative(deck.Dequeue(), index);
            RecordAction($"{actionName} draws a card.");
        }

        syncedDeckSize.value = deck.Count;
    }

    public void RequestBurnDeck(int amount)
    {
        if (!IsOnline)
            BurnDeckAuthoritative(amount);
        else
            ServerBurnDeck(amount);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerBurnDeck(int amount)
    {
        BurnDeckAuthoritative(amount);
    }

    private void BurnDeckAuthoritative(int amount)
    {
        int previousCount = deck.Count;
        for (int i = 0; i < Mathf.Max(0, amount) && deck.Count > 0; i++)
            deck.Dequeue();
        syncedDeckSize.value = deck.Count;

        int burned = previousCount - deck.Count;
        if (burned > 0)
            RecordAction($"{actionName} burns {burned} card{(burned == 1 ? string.Empty : "s")} from the deck.");
    }

    public void RequestReturnHandToDeck(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            ReturnHandToDeckAuthoritative(index);
        else
            ServerReturnHandToDeck(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerReturnHandToDeck(int index)
    {
        ReturnHandToDeckAuthoritative(index);
    }

    private void ReturnHandToDeckAuthoritative(int index)
    {
        if (!hand[index])
            return;

        RecordAction($"{actionName} returns a card from hand to the deck.");
        deck.Enqueue(hand[index].cardData);
        DestroyHandCardAuthoritative(index);
        syncedDeckSize.value = deck.Count;
    }

    public void RequestAddCard(CardInfo card, int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            SpawnHandCardAuthoritative(card, index);
        else
            ServerAddCard(card, index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerAddCard(CardInfo card, int index)
    {
        SpawnHandCardAuthoritative(card, index);
    }

    public void RequestPlayCard(CardInfo card, int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            SpawnFieldCardAuthoritative(card, index);
        else
            ServerPlayCard(card, index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerPlayCard(CardInfo card, int index)
    {
        SpawnFieldCardAuthoritative(card, index);
    }

    private void SpawnHandCardAuthoritative(CardInfo card, int index)
    {
        if (!cardToDraw || !IsValidSlot(index) || hand[index])
            return;

        GameObject instance = IsOnline
            ? Instantiate(cardToDraw, Vector3.zero, Quaternion.identity)
            : UnityProxy.InstantiateDirectly(cardToDraw, Vector3.zero, Quaternion.identity);
        HandCard handCard = instance.GetComponent<HandCard>();
        handCard.cardData = card;
        handCard.title = card.title;
        handCard.spr = card.spr;
        handCard.portrait = card.image;
        handCard.cardPosition = index;
        instance.GetComponent<Image>().sprite = handCard.CardBack;
        hand[index] = handCard;

        if (IsOnline)
        {
            NetworkManager.main.Spawn(instance);
            ObserversDisplayHand(instance, index);
        }
        else
        {
            DisplayHandLocal(instance, index);
        }
    }

    private void SpawnFieldCardAuthoritative(CardInfo card, int index)
    {
        if (!cardToSpawn || !IsValidSlot(index) || field[index])
            return;

        GameObject instance = IsOnline
            ? Instantiate(cardToSpawn, Vector3.zero, Quaternion.identity)
            : UnityProxy.InstantiateDirectly(cardToSpawn, Vector3.zero, Quaternion.identity);
        FieldCard fieldCard = instance.GetComponent<FieldCard>();
        fieldCard.cardData = card;
        fieldCard.title = card.title;
        fieldCard.spr = card.spr;
        fieldCard.portrait = card.image;
        fieldCard.attackPattern = card.attackPattern;
        fieldCard.ability = (FieldCard.Ability)card.ability;
        fieldCard.priority = card.ability;
        fieldCard.cardPosition = index;
        fieldCard.effect = card.effect;
        fieldCard.spawn = card.spawn;
        instance.GetComponent<Image>().sprite = fieldCard.portrait;
        field[index] = fieldCard;

        if (IsOnline)
        {
            NetworkManager.main.Spawn(instance);
            ObserversDisplayField(instance, index);
        }
        else
        {
            DisplayFieldLocal(instance, index);
        }

        RecordAction($"{actionName} plays <b>{card.title}</b> in lane {index + 1}.");
    }

    public void RequestDestroyHandCard(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            DestroyHandCardAuthoritative(index);
        else
            ServerDestroyHandCard(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerDestroyHandCard(int index)
    {
        DestroyHandCardAuthoritative(index);
    }

    private void DestroyHandCardAuthoritative(int index)
    {
        if (!IsValidSlot(index))
            return;

        HandCard card = hand[index];
        hand[index] = null;

        if (card)
        {
            if (card.isSpawned)
                card.Despawn();
            else
                UnityProxy.DestroyDirectly(card.gameObject);
        }

        if (IsOnline)
            ObserversClearHand(index);
    }

    public void RequestDestroyFieldCard(int index)
    {
        if (!IsValidSlot(index))
            return;

        if (!IsOnline)
            DestroyFieldCardAuthoritative(index);
        else
            ServerDestroyFieldCard(index);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerDestroyFieldCard(int index)
    {
        DestroyFieldCardAuthoritative(index);
    }

    private void DestroyFieldCardAuthoritative(int index)
    {
        if (!IsValidSlot(index))
            return;

        FieldCard card = field[index];
        field[index] = null;

        if (card)
        {
            RecordAction($"<b>{card.title}</b> leaves {actionName}'s lane {index + 1}.");
            if (card.isSpawned)
                card.Despawn();
            else
                UnityProxy.DestroyDirectly(card.gameObject);
        }

        if (IsOnline)
            ObserversClearField(index);
    }

    public void RequestSetFrozen(int index, int amount)
    {
        if (!IsOnline)
            SetFrozenAuthoritative(index, amount);
        else
            ServerSetFrozen(index, amount);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetFrozen(int index, int amount)
    {
        SetFrozenAuthoritative(index, amount);
    }

    private void SetFrozenAuthoritative(int index, int amount)
    {
        if (IsValidSlot(index) && field[index])
        {
            field[index].frozenTimer = Mathf.Max(0, amount);
            if (field[index].frozenTimer == 0)
                RecordAction($"{actionName}'s <b>{field[index].title}</b> is no longer frozen.");
            else
                RecordAction($"{actionName}'s <b>{field[index].title}</b> is frozen for {field[index].frozenTimer} turn{(field[index].frozenTimer == 1 ? string.Empty : "s")}.");
        }
    }

    public void RequestSetDefense(int index, int amount, bool replace = false)
    {
        if (!IsOnline)
            SetDefenseAuthoritative(index, amount, replace);
        else
            ServerSetDefense(index, amount, replace);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetDefense(int index, int amount, bool replace)
    {
        SetDefenseAuthoritative(index, amount, replace);
    }

    private void SetDefenseAuthoritative(int index, int amount, bool replace)
    {
        if (!IsValidSlot(index) || !field[index])
            return;

        int previousDefense = field[index].defense;
        field[index].defense = replace ? amount : field[index].defense + amount;
        if (IsOnline)
            ObserversSetDefense(index, field[index].defense);

        if (field[index].defense != previousDefense)
            RecordAction($"{actionName}'s <b>{field[index].title}</b> defense changes to {field[index].defense}.");
    }

    public void RequestSetRot(int index, bool rotting)
    {
        if (!IsOnline)
            ApplyRotToView(index, rotting);
        else
            ServerSetRot(index, rotting);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetRot(int index, bool rotting)
    {
        ObserversSetRot(index, rotting);
    }

    public void RequestSetHandSeen(int index, bool value)
    {
        if (!IsOnline)
            SetHandSeenAuthoritative(index, value);
        else
            ServerSetHandSeen(index, value);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetHandSeen(int index, bool value)
    {
        SetHandSeenAuthoritative(index, value);
    }

    private void SetHandSeenAuthoritative(int index, bool value)
    {
        if (IsValidSlot(index) && hand[index])
            hand[index].SetSeenAuthoritative(value);
    }

    public void RequestAdjustMana(int amount)
    {
        if (!IsOnline)
            AdjustManaAuthoritative(amount);
        else
            ServerAdjustMana(amount);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerAdjustMana(int amount)
    {
        AdjustManaAuthoritative(amount);
    }

    private void AdjustManaAuthoritative(int amount)
    {
        int previousMana = syncedMana.value;
        syncedMana.value = Mathf.Max(0, syncedMana.value + amount);
        int actualChange = syncedMana.value - previousMana;
        if (actualChange != 0)
            RecordAction($"{actionName} {(actualChange > 0 ? "gains" : "spends")} {Mathf.Abs(actualChange)} mana ({syncedMana.value} remaining).");
    }

    public void RequestAdjustHealth(int amount)
    {
        if (!IsOnline)
            AdjustHealthAuthoritative(amount);
        else
            ServerAdjustHealth(amount);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerAdjustHealth(int amount)
    {
        AdjustHealthAuthoritative(amount);
    }

    private void AdjustHealthAuthoritative(int amount)
    {
        syncedHealth.value += amount;
        if (amount != 0)
            RecordAction(amount > 0
                ? $"{actionName} heals {amount} health ({syncedHealth.value} remaining)."
                : $"{actionName} takes {Mathf.Abs(amount)} damage ({syncedHealth.value} health remaining).");
    }

    public void RecordAction(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!IsOnline)
            BattleLog.Record(message);
        else if (isServer)
            ObserversRecordAction(message);
        else
            ServerRecordAction(message);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerRecordAction(string message)
    {
        ObserversRecordAction(message);
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversRecordAction(string message)
    {
        BattleLog.Record(message);
    }

    internal void SetTurnAuthoritative(bool value)
    {
        if (!isSpawned || isServer)
            syncedTurn.value = value;
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversDisplayHand(GameObject card, int index)
    {
        if (!card || !IsValidSlot(index) || (isServer && !isClient))
            return;

        DisplayHandLocal(card, index);
    }

    private void DisplayHandLocal(GameObject card, int index)
    {
        if (!card || !IsValidSlot(index))
            return;

        ResolveSceneReferences();
        HandCard handCard = card.GetComponent<HandCard>();
        hand[index] = handCard;
        Transform board = UsesLocalBoard ? playerField.transform : enemyField.transform;
        handCard.transform.SetParent(board.GetChild(5).GetChild(index), false);
        handCard.cardPosition = index;
        handCard.GetComponent<Image>().sprite = UsesLocalBoard ? handCard.portrait : handCard.CardBack;
        StartCoroutine(CardGameFeel.AnimateReveal(handCard.GetComponent<RectTransform>()));
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversDisplayField(GameObject card, int index)
    {
        if (!card || !IsValidSlot(index) || (isServer && !isClient))
            return;

        DisplayFieldLocal(card, index);
    }

    private void DisplayFieldLocal(GameObject card, int index)
    {
        if (!card || !IsValidSlot(index))
            return;

        ResolveSceneReferences();
        FieldCard fieldCard = card.GetComponent<FieldCard>();
        field[index] = fieldCard;
        Transform board = UsesLocalBoard ? playerField.transform : enemyField.transform;
        fieldCard.transform.SetParent(board.GetChild(4).GetChild(index), false);
        fieldCard.cardPosition = index;
        StartCoroutine(CardGameFeel.AnimateReveal(fieldCard.GetComponent<RectTransform>()));
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversClearHand(int index)
    {
        if (!IsValidSlot(index))
            return;

        hand[index] = null;
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversClearField(int index)
    {
        if (!IsValidSlot(index))
            return;

        field[index] = null;
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversSetDefense(int index, int value)
    {
        if (isServer || !IsValidSlot(index) || !field[index])
            return;

        field[index].defense = value;
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversSetRot(int index, bool rotting)
    {
        ApplyRotToView(index, rotting);
    }

    private void ApplyRotToView(int index, bool rotting)
    {
        if (!IsValidSlot(index))
            return;

        ResolveSceneReferences();
        GameObject board = UsesLocalBoard ? playerField : enemyField;
        if (!board)
            return;

        board.transform.GetChild(4).GetChild(index).GetComponent<Slot>().rot = rotting;
    }

    private void RefreshBoardReferences()
    {
        GameObject board = UsesLocalBoard ? playerField : enemyField;
        if (!board)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            Transform fieldSlot = board.transform.GetChild(4).GetChild(i);
            Transform handSlot = board.transform.GetChild(5).GetChild(i);
            field[i] = fieldSlot.childCount > 0 ? fieldSlot.GetChild(0).GetComponent<FieldCard>() : null;
            hand[i] = handSlot.childCount > 0 ? handSlot.GetChild(0).GetComponent<HandCard>() : null;
        }
    }

    private void RefreshPlayerText()
    {
        ResolveSceneReferences();
        GameObject board = UsesLocalBoard ? playerField : enemyField;
        if (!board || board.transform.childCount < 6)
            return;

        SetBoardText(board.transform, 0, username);
        SetBoardText(board.transform, 1, hp.ToString());
        SetBoardText(board.transform, 2, sp.ToString());
        SetBoardText(board.transform, 3, deckSize.ToString());

        if (UsesLocalBoard)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            Transform slot = board.transform.GetChild(5).GetChild(i);
            if (slot.childCount == 0)
                continue;

            HandCard handCard = slot.GetChild(0).GetComponent<HandCard>();
            if (handCard)
                handCard.GetComponent<Image>().sprite = handCard.seen ? handCard.portrait : CardBack;
        }
    }

    private static void SetBoardText(Transform board, int index, string value)
    {
        if (board.childCount <= index)
            return;

        TMP_Text text = board.GetChild(index).GetComponent<TMP_Text>();
        if (text)
            text.text = value;
    }
}
