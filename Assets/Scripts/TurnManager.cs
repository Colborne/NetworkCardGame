using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public Player playerOne, playerTwo;
    public Player currentPlayer, target;
    public List<GameObject> allCards;
    public bool startingTurn;
    public int whichPlayer = 1;
    public int[] starting;

    void Start()
    {
        startingTurn = false;
        currentPlayer = playerOne;
        target = playerTwo;
        target.currentTurn.enabled = false;
        target.sp = 0;
    }

    void Update()
    {
        if(startingTurn)
        {
            NewTurn();
            startingTurn = false;
        }
    }

    void NewTurn()
    {
        currentPlayer.currentTurn.enabled = true;
        target.currentTurn.enabled = false;
        currentPlayer.sp++;
        currentPlayer.Draw();

        starting = new int[] {0,0,0,0,0};
        for(int i = 0; i < currentPlayer.field.Length; i++)
        {
            if(currentPlayer.field[i] != null)
                starting[i] = 1;
        }

        for(int i = 0; i < currentPlayer.field.Length; i++)
        {
            if(currentPlayer.field[i] != null && starting[i] == 1 && currentPlayer.field[i].phase == Card.Phase.Beginning){
                currentPlayer.field[i].UseAbility(currentPlayer, target);
            }
        }
        
        for(int i = 0; i < currentPlayer.field.Length; i++)
        {
            if(currentPlayer.field[i] != null && currentPlayer.field[i].phase == Card.Phase.During)
                currentPlayer.field[i].UseAbility(currentPlayer, target);
        }
    }
    
    public void EndTurn()
    {
        for(int i = 0; i < currentPlayer.field.Length; i++)
        {
            if(currentPlayer.field[i] != null && currentPlayer.field[i].phase == Card.Phase.End)
                currentPlayer.field[i].UseAbility(currentPlayer, target);
        }      

        if(currentPlayer == playerOne)
        {
            currentPlayer = playerTwo;
            target = playerOne;
        }
        else
        {
            currentPlayer = playerOne;
            target = playerTwo;
        }
        
        startingTurn = true;
    }
}
