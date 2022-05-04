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
    public ScriptableCard spawn;
    public int priority;

    public override void Update()
    {
        if(title == "")
        {
            title = cardData.title;
            spr = cardData.spr;
            portrait = cardData.image;
            effect = cardData.effect;
            attackPattern = cardData.attackPattern;
            priority = cardData.ability;
            spawn = cardData.spawn;
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
                for(int i = 0; i < spr; i++)
                    player.Draw();
                break;
            case Ability.Bomb:
                EffectSpawn(player);
                CmdHeal(player, -spr);
                player.CmdDestroyFieldCard(cardPosition);
                break;
            case Ability.Damage:
                CmdDamage(player, target);
                break;
            case Ability.Heal:
                EffectSpawn(player);
                CmdHeal(player, spr);
                break;
            case Ability.Summoning:
                EffectSpawn(player);
                CmdMana(player, spr);
                break;
            case Ability.Duplicate:
                EffectSpawn(player);
                for(int i = 0; i < 5; i++)
                {
                    if(player.field[i] == null)
                    {
                        player.CmdPlayCard(cardData, i);
                        return;
                    }
                }
                break;
            case Ability.Swap:
                EffectSpawn(player);
                if(target.field[cardPosition] != null)
                {
                    FieldCard temp = target.field[cardPosition];
                    target.field[cardPosition] = this;
                    player.field[cardPosition] = temp;
                    
                }
                break;
            case Ability.Evolve:
                EffectSpawn(player);
                if(spawn != null && player.sp > 0)
                {
                    player.CmdPlayCard(new CardInfo(spawn), cardPosition);
                    player.field[cardPosition].cardPosition = cardPosition;
                    Destroy(this);
                }
                break;
            case Ability.DrainLife:
                EffectSpawn(player);
                CmdHeal(target, -spr);
                break;
            case Ability.StealLife:
                EffectSpawn(player);
                CmdHeal(target, -1);
                CmdHeal(player, 1);
                break;
            case Ability.DrainMana:
                EffectSpawn(player);
                CmdMana(target, -spr);
                break;
            case Ability.StealMana:
                EffectSpawn(player);
                CmdMana(target, -1);
                CmdMana(player, 1);
                break;
            case Ability.ClearBoard:
                for(int i = 0; i < player.hand.Length; i++)
                {
                    player.hand[i] = null;
                    EffectSpawnSelected(player, true, i);
                    player.field[i] = null;
                    EffectSpawnSelected(player, false, i);
                    target.hand[i] = null;
                    EffectSpawnSelected(target, true, i);
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
                }
                break;
            case Ability.Spawn:
                for(int i = 0; i < 5; i++)
                {
                    if(target.field[i] == null)
                    {
                        target.CmdPlayCard(new CardInfo(spawn), i);
                        target.field[i].cardPosition = i;
                        EffectSpawnSelected(target, false, i);
                    }
                }
                break;
            case Ability.StealCard:
                EffectSpawn(player);
                if(target.deck.Count > 0)
                {
                    player.CmdPlayCard(target.deck.Dequeue(), cardPosition);
                    player.field[cardPosition].cardPosition = cardPosition;
                    Destroy(gameObject);
                }
                break;
            case Ability.DeckCard:
                EffectSpawn(player);
                if(player.deck.Count > 0)
                {
                    player.CmdPlayCard(player.deck.Dequeue(), cardPosition);
                    player.field[cardPosition].cardPosition = cardPosition;
                    Destroy(gameObject);
                }
                break;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdHeal(PlayerManager player, int amount)
    {
        player.hp += amount;
    }
    
    [Command(requiresAuthority = false)]
    public void CmdMana(PlayerManager player, int amount)
    {
        player.sp += amount;
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
                        target.field[i] = null;
                        AttackSetup(player, target, i);
                    }
                }
                else
                {
                    if(i == cardPosition - 2 && farLeftDamage > 0){
                        target.hp -= farLeftDamage;
                        AttackSetup(player,target, i);
                    }
                    else if(i == cardPosition - 1 && leftDamage > 0){
                        target.hp -= leftDamage;
                        AttackSetup(player, target, i);
                    }
                    else if(i == cardPosition && mainDamage > 0)
                    {
                        target.hp -= mainDamage;
                        AttackSetup(player, target, i);
                    }
                    else if(i == cardPosition + 1 && rightDamage > 0){
                        target.hp -= rightDamage;
                        AttackSetup(player, target, i);
                    }
                    else if(i == cardPosition + 2 && farRightDamage > 0){
                        target.hp -= farRightDamage;
                        AttackSetup(player, target, i);
                    }
                }
            }
        }
    }
    
    void AttackSetup(PlayerManager player, PlayerManager target, int i) 
    {
        var attack = Instantiate(effect, transform.position, Quaternion.identity);
        attack.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        attack.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(4).GetChild(i).position;
        attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
    }

    void EffectSpawn(PlayerManager player)
    {
        var eff = Instantiate(effect, transform.position, Quaternion.identity);
        eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    void EffectSpawnSelected(PlayerManager player, bool isHand, int i)
    {
        if(isHand)
        {
            var eff = Instantiate(effect, player.playerField.transform.GetChild(5).GetChild(i).position, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
        else
        {
            var eff = Instantiate(effect, player.playerField.transform.GetChild(4).GetChild(i).position, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
    }
}