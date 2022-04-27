using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public enum Ability
    {
        Draw,
        Damage,
        Heal,
        Summoning,
        Duplicate,
        Swap,
        Evolve,
        Bomb,
        DrainLife,
        StealLife,
        DrainMana,
        StealMana,
        ClearBoard,
        RemoveCard,
        Spawn,
        StealCard,
        DeckCard
    }

    public enum Phase
    {
        Beginning,
        During,
        End
    }
    public string title;
    public int SPR;
    public Ability ability; 
    public Phase phase;
    public Sprite portrait;
    public int cardPosition;
    public Card evolution;
    public GameObject effect;
    public int[] attackPattern;
    public Card fusion;
    public Card spawn;
    void Start()
    {
        GetComponent<SpriteRenderer>().sprite = portrait;
    }

    public void UseAbility(Player player, Player target)
    {
        switch(ability)
        {
            case Ability.Draw:
                EffectSpawn(player);
                for(int i = 0; i < SPR; i++)
                    player.Draw();
                break;
            case Ability.Bomb:
                EffectSpawn(player);
                player.hp -= SPR;
                player.field[cardPosition] = null;
                player.visibleField[cardPosition].image.sprite = player.UISprite;
                break;
            case Ability.Damage:
                Damage(player, target);
                break;
            case Ability.Heal:
                EffectSpawn(player);
                player.hp += SPR;
                break;
            case Ability.Summoning:
                EffectSpawn(player);
                player.sp += SPR;
                break;
            case Ability.Duplicate:
                EffectSpawn(player);
                for(int i = 0; i < 5; i++)
                {
                    if(player.field[i] == null)
                    {
                        var dupe = Instantiate(this);
                        player.field[i] = dupe;
                        player.visibleField[i].image.sprite = this.portrait;
                        return;
                    }
                }
                break;
            case Ability.Swap:
                EffectSpawn(player);
                if(target.field[cardPosition] != null)
                {
                    Card temp = target.field[cardPosition];
                    target.field[cardPosition] = player.field[cardPosition];
                    player.field[cardPosition] = temp;
                    
                    player.visibleField[cardPosition].image.sprite = player.field[cardPosition].portrait;
                    target.visibleField[cardPosition].image.sprite = target.field[cardPosition].portrait;
                }
                break;
            case Ability.Evolve:
                EffectSpawn(player);
                if(evolution != null && player.sp > 0)
                {
                    player.sp--;
                    player.field[cardPosition] = Instantiate(evolution);
                    player.visibleField[cardPosition].image.sprite = evolution.portrait;
                    player.field[cardPosition].cardPosition = cardPosition; 
                }
                break;
            case Ability.DrainLife:
                EffectSpawn(player);
                target.hp = Mathf.Max(0, target.hp - player.field[cardPosition].SPR);
                break;
            case Ability.StealLife:
                EffectSpawn(player);
                target.hp = Mathf.Max(0, target.hp - 1);
                player.hp += 1;
                break;
            case Ability.DrainMana:
                EffectSpawn(player);
                target.sp = Mathf.Max(0, target.sp - player.field[cardPosition].SPR);
                break;
            case Ability.StealMana:
                EffectSpawn(player);
                target.sp = Mathf.Max(0, target.sp - 1);
                player.sp += 1;
                break;
            case Ability.ClearBoard:
                for(int i = 0; i < player.hand.Length; i++)
                {
                    player.hand[i] = null;
                    player.visibleHand[i].image.sprite = player.UISprite;
                    EffectSpawnSelected(player, true, i);
                }
                
                for(int i = 0; i < player.field.Length; i++)
                {
                    player.field[i] = null;
                    player.visibleField[i].image.sprite = player.UISprite;
                    EffectSpawnSelected(player, false, i);
                }
                
                for(int i = 0; i < target.hand.Length; i++)
                {
                    target.hand[i] = null;
                    target.visibleHand[i].image.sprite = target.UISprite;
                    EffectSpawnSelected(target, true, i);
                }
                
                for(int i = 0; i < target.field.Length; i++)
                {
                    target.field[i] = null;
                    target.visibleField[i].image.sprite = target.UISprite;
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
                    player.visibleHand[rand].image.sprite = player.UISprite;
                    EffectSpawnSelected(player, true, rand);
                }
                else if(rand == 1)
                {
                    rand = Random.Range(0, player.field.Length);
                    player.field[rand] = null;
                    player.visibleField[rand].image.sprite = player.UISprite;
                    EffectSpawnSelected(player, false, rand);
                }
                else if(rand == 2)
                {
                    rand = Random.Range(0, target.hand.Length);
                    target.hand[rand] = null;
                    target.visibleHand[rand].image.sprite = target.UISprite;
                    EffectSpawnSelected(target, true, rand);
                }
                else if(rand == 3)
                {
                    rand = Random.Range(0, target.field.Length);
                    target.field[rand] = null;
                    target.visibleField[rand].image.sprite = target.UISprite;
                    EffectSpawnSelected(target, false, rand);
                }
                break;
            case Ability.Spawn:
                for(int i = 0; i < 5; i++)
                {
                    if(target.field[i] == null)
                    {
                        target.field[i] = Instantiate(spawn);
                        target.field[i].cardPosition = i;
                        target.visibleField[i].image.sprite = spawn.portrait;
                        EffectSpawnSelected(target, false, i);
                    }
                }
                break;
            case Ability.StealCard:
                EffectSpawn(player);
                if(target.deck.Count > 0)
                {
                    player.field[cardPosition] = target.deck.Dequeue();
                    player.visibleField[cardPosition].image.sprite = player.field[cardPosition].portrait;
                    player.field[cardPosition].cardPosition = cardPosition;
                }
                break;
            case Ability.DeckCard:
                EffectSpawn(player);
                if(player.deck.Count > 0)
                {
                    player.field[cardPosition] = player.deck.Dequeue();
                    player.visibleField[cardPosition].image.sprite = player.field[cardPosition].portrait;
                    player.field[cardPosition].cardPosition = cardPosition;
                }
                break;
        }
    }

    public void Damage(Player player, Player target)
    {  
        int leftDamage = 0;
        int mainDamage = 0;
        int rightDamage = 0;
        bool[] spread = new bool[3];

        if(attackPattern.Length == 3)
        {
            leftDamage = attackPattern[0];
            mainDamage = attackPattern[1];
            rightDamage = attackPattern[2];
            spread[0] = true; 
            spread[1] = true; 
            spread[2] = true;
        }
        else
            mainDamage = attackPattern[0];
            spread[0] = false; 
            spread[1] = true; 
            spread[2] = false;

        for(int i = 0; i < 5; i++)
        {
            if(target.field[i] != null)
            {
                int damage = 0;
                if(i == cardPosition - 1)
                    damage = Mathf.Max(0, leftDamage - target.field[i].SPR);
                else if(i == cardPosition)
                    damage = Mathf.Max(0, mainDamage - target.field[i].SPR);
                else if(i == cardPosition + 1)
                
                    damage = Mathf.Max(0, rightDamage - target.field[i].SPR);
        
                if(damage > 0)
                {
                    target.hp -= damage;
                    target.field[i] = null;
                    target.visibleField[i].image.sprite = target.UISprite;
                    AttackSetup(player,target,i);
                }
            }
            else
            {
                if(i == cardPosition - 1 && leftDamage > 0){
                    target.hp -= leftDamage;
                    AttackSetup(player,target,i);
                }
                else if(i == cardPosition && mainDamage > 0)
                {
                    target.hp -= mainDamage;
                    AttackSetup(player,target,i);
                }
                else if(i == cardPosition + 1 && rightDamage > 0){
                    target.hp -= rightDamage;
                    AttackSetup(player,target,i);
                }
            }
        }
    }

    void AttackSetup(Player player, Player target,int i) 
    {
        RectTransform rect = player.visibleField[cardPosition].GetComponent<RectTransform>();
        var attack = Instantiate(effect, rect.localPosition, rect.rotation);
        attack.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        attack.GetComponent<RectTransform>().localPosition = new Vector3(
            player.visibleField[cardPosition].GetComponent<RectTransform>().localPosition.x, 
            player.visibleField[cardPosition].GetComponent<RectTransform>().localPosition.y, 
            -50);
        attack.GetComponent<Projectile>().destination = new Vector3(
            target.visibleField[i].GetComponent<RectTransform>().localPosition.x,
            target.visibleField[i].GetComponent<RectTransform>().localPosition.y,
            -50);
        attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
    }

    void EffectSpawn(Player player)
    {
        var eff = Instantiate(effect, player.visibleField[cardPosition].GetComponent<RectTransform>().localPosition, Quaternion.identity);
        eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        eff.GetComponent<RectTransform>().localPosition = new Vector3(
            player.visibleField[cardPosition].GetComponent<RectTransform>().localPosition.x, 
            player.visibleField[cardPosition].GetComponent<RectTransform>().localPosition.y, 
            -50);
        eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    void EffectSpawnSelected(Player player, bool isHand, int i)
    {
        if(isHand)
        {
            var eff = Instantiate(effect, player.visibleHand[i].GetComponent<RectTransform>().localPosition, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localPosition = new Vector3(
                player.visibleHand[i].GetComponent<RectTransform>().localPosition.x, 
                player.visibleHand[i].GetComponent<RectTransform>().localPosition.y, 
                -50);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
        else
        {
            var eff = Instantiate(effect, player.visibleField[i].GetComponent<RectTransform>().localPosition, Quaternion.identity);
            eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
            eff.GetComponent<RectTransform>().localPosition = new Vector3(
                player.visibleField[i].GetComponent<RectTransform>().localPosition.x, 
                player.visibleField[i].GetComponent<RectTransform>().localPosition.y, 
                -50);
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
    }
}