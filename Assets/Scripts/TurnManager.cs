using PurrNet;
using TMPro;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    private readonly SyncVar<int> syncedWhichPlayer = new(-1);

    public PlayerManager playerOne;
    public PlayerManager playerTwo;
    public int whichPlayer => syncedWhichPlayer.value;
    public bool needPlayers = true;
    public Canvas canvas;

    private bool gameStarted;
    private bool offlineBattle;

    private void Update()
    {
        if (offlineBattle)
            return;

        if (!isSpawned)
            return;

        if (networkManager.isClientOnly && canvas && canvas.enabled && !gameStarted)
        {
            gameStarted = true;
            canvas.enabled = false;
        }

        if (networkManager.isHost && canvas && canvas.enabled && !gameStarted)
            canvas.GetComponentInChildren<TMP_Text>().text = "Waiting for Client...";

        if (isServer && needPlayers)
            TryStartOnlineBattle();
    }

    private void TryStartOnlineBattle()
    {
        PlayerManager[] players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        PlayerManager first = null;
        PlayerManager second = null;

        foreach (PlayerManager candidate in players)
        {
            if (!candidate.isSpawned || !candidate.isReady)
                continue;

            if (!first)
                first = candidate;
            else if (candidate != first)
            {
                second = candidate;
                break;
            }
        }

        if (!first || !second)
            return;

        playerOne = first;
        playerTwo = second;
        playerOne.enemy = playerTwo;
        playerTwo.enemy = playerOne;
        playerOne.hasEnemy = true;
        playerTwo.hasEnemy = true;
        playerOne.SetTurnAuthoritative(true);
        playerTwo.SetTurnAuthoritative(false);
        syncedWhichPlayer.value = 0;
        needPlayers = false;
        gameStarted = true;
        playerOne.RecordAction($"<color=#8FD3FF><b>{playerOne.actionName} takes the first turn.</b></color>");
        ObserversBattleStarted();
    }

    public void StartOfflineBattle(PlayerManager human, PlayerManager ai)
    {
        if (!human || !ai)
            return;

        offlineBattle = true;
        gameStarted = true;

        if (canvas)
            canvas.enabled = false;

        playerOne = human;
        playerTwo = ai;
        playerOne.enemy = playerTwo;
        playerTwo.enemy = playerOne;
        playerOne.hasEnemy = true;
        playerTwo.hasEnemy = true;
        playerOne.SetTurnAuthoritative(true);
        playerTwo.SetTurnAuthoritative(false);
        if (!isSpawned || isServer)
            syncedWhichPlayer.value = 0;
        needPlayers = false;
        playerOne.RecordAction($"<color=#8FD3FF><b>{playerOne.actionName} takes the first turn.</b></color>");
    }

    public void EndTurn(PlayerManager currentPlayer, PlayerManager target)
    {
        if (!currentPlayer || !target)
            return;

        if (offlineBattle || !isSpawned)
            ApplyTurnChange(currentPlayer, target, false);
        else
            ServerEndTurn(currentPlayer, target);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerEndTurn(PlayerManager currentPlayer, PlayerManager target)
    {
        if (!currentPlayer || !target || !currentPlayer.isOurTurn || currentPlayer.enemy != target)
            return;

        ApplyTurnChange(currentPlayer, target, true);
    }

    private void ApplyTurnChange(PlayerManager currentPlayer, PlayerManager target, bool notifyObservers)
    {
        currentPlayer.SetTurnAuthoritative(false);
        target.SetTurnAuthoritative(true);
        syncedWhichPlayer.value = syncedWhichPlayer.value == 0 ? 1 : 0;

        if (notifyObservers)
            ObserversBeginTurn(target);
        else
            target.NewTurn();
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversBattleStarted()
    {
        gameStarted = true;
        if (canvas)
            canvas.enabled = false;
    }

    [ObserversRpc(runLocally: true)]
    private void ObserversBeginTurn(PlayerManager player)
    {
        if (player && player.isOwner)
            player.NewTurn();
    }
}
