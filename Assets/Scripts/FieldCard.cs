using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class FieldCard : BaseCard
{
    public enum Ability
    {
        Swap,
        Bomb,
        Draw, 
        StealCard,
        DeckCard,
        Sacrifice,
        Defend,
        Heal,
        Summoning,
        Damage,
        Duplicate,
        Evolve,
        Spawn,
        DrainLife,
        StealLife,
        DrainMana,
        StealMana,
        ClearBoard,
        RemoveCard
    }
    public bool clicked = false;
    public int[] attackPattern;
    public Ability ability;
    public GameObject effect;
    public int priority;

    public override void Update()
    {
        if(title == "")
        {
            title = cardData.title;
            spr = cardData.spr;
            portrait = cardData.image;
            GetComponent<Image>().sprite = portrait;
        }
        GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    public void UseAbility(PlayerManager player, PlayerManager target)
    {
        switch(ability)
        {
            case Ability.Draw:
                EffectSpawn(player);
                /*for(int i = 0; i < spr; i++)
                    player.Draw();*/
                break;
            case Ability.Bomb:
                EffectSpawn(player);
                player.hp -= spr;
                player.field[cardPosition] = null;
                break;
            case Ability.Damage:
                CmdDamage(player, target);
                break;
            case Ability.Heal:
                EffectSpawn(player);
                player.hp += spr;
                break;
            case Ability.Summoning:
                EffectSpawn(player);
                player.sp += spr;
                break;
            case Ability.Duplicate:
                EffectSpawn(player);
                for(int i = 0; i < 5; i++)
                {
                    if(player.field[i] == null)
                    {
                        var dupe = Instantiate(this);
                        player.field[i] = dupe;
                        return;
                    }
                }
                break;
            case Ability.Swap:
                EffectSpawn(player);
                if(target.field[cardPosition] != null)
                {
                    FieldCard temp = target.field[cardPosition];
                    target.field[cardPosition] = player.field[cardPosition];
                    player.field[cardPosition] = temp;
                    
                }
                break;
            /*case Ability.Evolve:
                EffectSpawn(player);
                if(evolution != null && player.sp > 0)
                {
                    player.sp--;
                    player.field[cardPosition] = Instantiate(evolution);
                    player.field[cardPosition].cardPosition = cardPosition; 
                }
                break;*/
            case Ability.DrainLife:
                EffectSpawn(player);
                target.hp = Mathf.Max(0, target.hp - player.field[cardPosition].spr);
                break;
            case Ability.StealLife:
                EffectSpawn(player);
                target.hp = Mathf.Max(0, target.hp - 1);
                player.hp += 1;
                break;
            case Ability.DrainMana:
                EffectSpawn(player);
                target.sp = Mathf.Max(0, target.sp - player.field[cardPosition].spr);
                break;
            case Ability.StealMana:
                EffectSpawn(player);
                target.sp = Mathf.Max(0, target.sp - 1);
                player.sp += 1;
                break;
            case Ability.ClearBoard:
            /*
                for(int i = 0; i < player.hand.Length; i++)
                {
                    //player.hand[i] = null;
                    EffectSpawnSelected(player, true, i);
                }
                
                for(int i = 0; i < player.field.Length; i++)
                {
                    player.field[i] = null;
                    EffectSpawnSelected(player, false, i);
                }
                
                for(int i = 0; i < target.hand.Length; i++)
                {
                    target.hand[i] = null;
                    EffectSpawnSelected(target, true, i);
                }
                
                for(int i = 0; i < target.field.Length; i++)
                {
                    target.field[i] = null;
                    EffectSpawnSelected(target, false, i);
                }
                break;
            case Ability.RemoveCard:
                EffectSpawn(player);
                int rand = Random.Range(0,3);
                if(rand == 0)
                {
                    rand = Random.Range(0, player.hand.Length);
                    player.hand[rand] = null;
                    EffectSpawnSelected(player, true, rand);
                }
                else if(rand == 1)
                {
                    rand = Random.Range(0, player.field.Length);
                    player.field[rand] = null;
                    EffectSpawnSelected(player, false, rand);
                }
                else if(rand == 2)
                {
                    rand = Random.Range(0, target.hand.Length);
                    target.hand[rand] = null;
                    EffectSpawnSelected(target, true, rand);
                }
                else if(rand == 3)
                {
                    rand = Random.Range(0, target.field.Length);
                    target.field[rand] = null;
                    EffectSpawnSelected(target, false, rand);
                }*/
                break;
            /*case Ability.Spawn:
                for(int i = 0; i < 5; i++)
                {
                    if(target.field[i] == null)
                    {
                        target.field[i] = Instantiate(spawn);
                        target.field[i].cardPosition = i;
                        EffectSpawnSelected(target, false, i);
                    }
                }
                break;
            case Ability.StealCard:
                EffectSpawn(player);
                if(target.deck.Count > 0)
                {
                    player.field[cardPosition] = target.deck.Dequeue();
                    player.field[cardPosition].cardPosition = cardPosition;
                }
                break;
            case Ability.DeckCard:
                EffectSpawn(player);
                if(player.deck.Count > 0)
                {
                    player.field[cardPosition] = player.deck.Dequeue();
                    player.field[cardPosition].cardPosition = cardPosition;
                }
                break;*/
        }
    }



    [Command(requiresAuthority = false)]
    public void CmdDamage(PlayerManager player, PlayerManager target)
    {  
        int farLeftDamage = attackPattern[0];
        int leftDamage = attackPattern[1];
        int mainDamage = attackPattern[2];
        int rightDamage = attackPattern[3];
        int farRightDamage = attackPattern[4];

        for(int i = 0; i < 5; i++)
        {
            if(attackPattern[i] != 0)
            {
                if(target.field[i] != null)
                {
                    int damage = 0;
                    if(i == cardPosition - 2)
                        damage = Mathf.Max(0, farLeftDamage - target.field[i].spr);
                    else if(i == cardPosition - 1)
                        damage = Mathf.Max(0, leftDamage - target.field[i].spr);
                    else if(i == cardPosition)
                        damage = Mathf.Max(0, mainDamage - target.field[i].spr);
                    else if(i == cardPosition + 1)
                        damage = Mathf.Max(0, rightDamage - target.field[i].spr);
                    else if(i == cardPosition + 2)
                        damage = Mathf.Max(0, farRightDamage - target.field[i].spr);
            
                    if(damage > 0)
                    {
                        target.hp -= damage;
                        //target.field[i] = null;
                        //AttackSetup(player, target,i);
                    }
                }
                else
                {
                    if(i == cardPosition - 2 && farLeftDamage > 0){
                        target.hp -= farLeftDamage;
                        //AttackSetup(player,target,i);
                    }
                    else if(i == cardPosition - 1 && leftDamage > 0){
                        target.hp -= leftDamage;
                        //AttackSetup(player, target,i);
                    }
                    else if(i == cardPosition && mainDamage > 0)
                    {
                        target.hp -= mainDamage;
                        //AttackSetup(player, target,i);
                    }
                    else if(i == cardPosition + 1 && rightDamage > 0){
                        target.hp -= rightDamage;
                        //AttackSetup(player, target,i);
                    }
                    else if(i == cardPosition + 2 && farRightDamage > 0){
                        target.hp -= farRightDamage;
                        //AttackSetup(player, target,i);
                    }
                }
            }
        }
    }
    
    void AttackSetup(PlayerManager player, PlayerManager target, int i) 
    {
        RectTransform rect = player.field[cardPosition].GetComponent<RectTransform>();
        var attack = Instantiate(effect, rect.localPosition, rect.rotation);
        attack.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        attack.GetComponent<RectTransform>().localPosition = new Vector3(
            player.field[cardPosition].GetComponent<RectTransform>().localPosition.x, 
            player.field[cardPosition].GetComponent<RectTransform>().localPosition.y, 
            -50);
        attack.GetComponent<Projectile>().destination = new Vector3(
            target.field[i].GetComponent<RectTransform>().localPosition.x,
            target.field[i].GetComponent<RectTransform>().localPosition.y,
            -50);
        attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
    }

    void EffectSpawn(PlayerManager player)
    {
        var eff = Instantiate(effect, player.field[cardPosition].GetComponent<RectTransform>().localPosition, Quaternion.identity);
        eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        eff.GetComponent<RectTransform>().localPosition = new Vector3(
            player.field[cardPosition].GetComponent<RectTransform>().localPosition.x, 
            player.field[cardPosition].GetComponent<RectTransform>().localPosition.y, 
            -50);
        eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    void EffectSpawnSelected(PlayerManager player, bool isHand, int i)
    {
        if(isHand)
        {
            var eff = Instantiate(effect, player.field[i].GetComponent<RectTransform>().localPosition, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localPosition = new Vector3(
                player.field[i].GetComponent<RectTransform>().localPosition.x, 
                player.field[i].GetComponent<RectTransform>().localPosition.y, 
                -50);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
        else
        {
            var eff = Instantiate(effect, player.field[i].GetComponent<RectTransform>().localPosition, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localPosition = new Vector3(
                player.field[i].GetComponent<RectTransform>().localPosition.x, 
                player.field[i].GetComponent<RectTransform>().localPosition.y, 
                -50);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
    }
}

/*

        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10);
        Ray castPoint = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(castPoint, out hit, Mathf.Infinity))
            {
                clicked = !clicked;
            }
        }

        if(clicked)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0);
            GetComponent<RectTransform>().position =  fixedPosition;
        }

*/