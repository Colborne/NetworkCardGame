using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Linq;
using TMPro;

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
        RemoveCard,
        Freeze,
        Rearrange,
        DeckBurn,
        ClearField,
        Rot,
        Sight,
        Blitz
    }

    public int[] attackPattern;
    public Ability ability;
    public GameObject effect;
    public ScriptableCard spawn;
    public int priority;
    public int defense;
    public int rotPosition;
    public bool rot = false;
    public int frozenTime;
    public override void Update()
    {
        if(title == "")
        {
            title = cardData.title;
            spr = cardData.spr;
            portrait = cardData.image;
            effect = cardData.effect;
            attackPattern = cardData.attackPattern;
            ability = (FieldCard.Ability)cardData.ability;
            priority = (int)ability;
            spawn = cardData.spawn;
            cardPosition = GetComponentInParent<Slot>().slotNumber;
        }
    
        GetComponent<Image>().sprite = portrait;
        GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    public void UseAbility(PlayerManager player, PlayerManager target)
    {
        switch(ability)
        {
            case Ability.Draw:
                EffectSpawn(player);
                break;
            case Ability.Bomb:
                EffectSpawn(player);
                player.hp = Mathf.Max(0, player.hp - spr);
                player.CmdDestroyFieldCard(cardPosition);
                break;
            case Ability.Damage:
                Damage(player, target);
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
                        GameObject bc = Instantiate(player.cardToSpawn);
                        FieldCard temp = bc.GetComponent<FieldCard>();
                        temp.cardData = new CardInfo(this.cardData.data);
                        temp.title = this.title;
                        temp.spr = this.spr;
                        temp.portrait = this.portrait;
                        temp.attackPattern = this.attackPattern;
                        temp.ability = this.ability;
                        temp.cardPosition = i;
                        temp.effect = this.effect;
                        temp.spawn = this.spawn;
                        bc.GetComponent<Image>().sprite = temp.portrait;
                        player.field[i] = temp;
                        NetworkServer.Spawn(bc);

                        player.RpcDisplayCard(bc, i);
                        return;
                    }
                }
                break;
            case Ability.Swap:
                EffectSpawn(player);
                if(target.field[cardPosition] != null)
                {
                    player.RpcDisplayCard(target.field[cardPosition].gameObject, cardPosition);
                    target.RpcDisplayCard(this.gameObject, cardPosition);
                }
                break;
            case Ability.Evolve:
                EffectSpawn(player);
                if(spawn != null)
                {
                    cardData = new CardInfo(spawn);
                    title = spawn.title;
                    spr = spawn.spr;
                    portrait = spawn.portrait;
                    attackPattern = spawn.attackPattern;
                    ability = (FieldCard.Ability)spawn.ability;
                    effect = spawn.effect;
                    spawn = spawn.spawn;
                    GetComponent<Image>().sprite = portrait;
                }
                break;
            case Ability.DrainLife:
                DrainEffectSpawn(player, 1);
                target.hp = Mathf.Max(0, target.hp - spr);
                break;
            case Ability.StealLife:
                StealEffectSpawn(player, 1);
                if(target.hp >= 1)
                {
                    target.hp -= 1;
                    player.hp += 1;
                }
                break;
            case Ability.DrainMana:
                DrainEffectSpawn(player, 2);
                target.sp = Mathf.Max(0, target.sp - spr);
                break;
            case Ability.StealMana:
                StealEffectSpawn(player, 2);
                if(target.sp >= 1)
                {
                    target.sp -= 1;
                    player.sp += 1;
                }
                break;
            case Ability.ClearBoard:
                for(int i = 0; i < player.hand.Length; i++)
                {
                    player.CmdDestroyHandCard(i);
                    EffectSpawnSelected(player, false, true, i);
                    player.CmdDestroyFieldCard(i);
                    EffectSpawnSelected(player, false, false, i);
                    target.CmdDestroyHandCard(i);
                    EffectSpawnSelected(target, true, true, i);
                    target.CmdDestroyFieldCard(i);
                    EffectSpawnSelected(target, true, false, i);
                }
                break;
            case Ability.ClearField:
                for(int i = 0; i < player.hand.Length; i++)
                {
                    player.CmdDestroyFieldCard(i);
                    EffectSpawnSelected(player, false, false, i);
                    target.CmdDestroyFieldCard(i);
                    EffectSpawnSelected(target, true, false, i);
                }
                break;
            case Ability.RemoveCard:
                EffectSpawn(player);
                int rand = Random.Range(0,3);
                if(rand == 0)
                {
                    rand = Random.Range(0, player.hand.Length);
                    player.CmdDestroyHandCard(rand);
                    EffectSpawnSelected(player, false, true, rand);
                }
                else if(rand == 1)
                {
                    rand = Random.Range(0, player.field.Length);
                    player.CmdDestroyFieldCard(rand);
                    EffectSpawnSelected(player, false, false, rand);
                }
                else if(rand == 2)
                {
                    rand = Random.Range(0, target.hand.Length);
                    target.CmdDestroyHandCard(rand);
                    EffectSpawnSelected(target, true, true, rand);
                }
                else if(rand == 3)
                {
                    rand = Random.Range(0, target.field.Length);
                    target.CmdDestroyFieldCard(rand);
                    EffectSpawnSelected(target, true, false, rand);
                }
                break;
            case Ability.Spawn:
                for(int i = 0; i < 5; i++)
                {
                    if(target.field[i] == null)
                    {
                        target.CmdPlayCard(new CardInfo(spawn), i);
                        EffectSpawnSelected(target, true, false, i);
                    }
                }
                break;
            case Ability.DeckCard:
                EffectSpawn(player);
                if(player.deckSize > 1)
                {
                    player.CmdPlayCard(player.deck.Dequeue(), cardPosition);
                    Destroy(gameObject);
                }
                break;
            case Ability.Defend:
                Defend(player);
                break;
            case Ability.DeckBurn:
                DrainEffectSpawn(player, 3);
                for(int i = 0; i < spr/2; i++)
                {
                    if(target.deck.Count > 0)
                        target.deck.Dequeue();
                }
                break;
            case Ability.Rearrange:
                System.Random rng = new System.Random();
                int[] isCard = new int[5] {0,0,0,0,0};
                List<FieldCard> fc = new List<FieldCard>();
               
               for(int i = 0; i < 5; i++)
                {
                    if(target.field[i] != null)
                    {
                        isCard[i] = 1;
                        fc.Add(target.field[i]);
                        Destroy(target.field[i].gameObject);
                        target.field[i] = null;
                    }
                }

                isCard = isCard.OrderBy(x => rng.Next()).ToArray();
                fc = fc.OrderBy(x => rng.Next()).ToList();
                
                for(int i = 0; i < 5; i++)
                {
                    if(isCard[i] == 1)
                    {
                        var t = fc[Random.Range(0, fc.Count)];
                        target.CmdPlayCard(t.cardData,i);
                        fc.Remove(t);
                    }               
                }
                break;
            case Ability.Freeze:
                if(target.field[cardPosition] != null)
                {
                    LingeringEffectSpawnSelected(target.field[cardPosition], false);
                    player.CmdDestroyFieldCard(cardPosition);
                    target.field[cardPosition].frozenTime = spr/2;   
                }
                break;
            case Ability.Rot:
                if(target.field[cardPosition] != null)
                {
                    target.field[cardPosition].rotPosition = cardPosition;
                    target.field[cardPosition].rot = true;
                    LingeringEffectSpawnSelected(target.field[cardPosition], true);
                    player.CmdDestroyFieldCard(cardPosition);
                }
                break;
            case Ability.Sacrifice:
                break;
            case Ability.Sight:
                if(target.hand[cardPosition] != null)
                {
                    target.hand[cardPosition].GetComponent<Image>().sprite = target.hand[cardPosition].portrait; 
                    target.hand[cardPosition].seen = true;
                }
                EffectSpawn(player);
                break;
            case Ability.Blitz:
                if(target.field[cardPosition] != null)
                {
                    player.CmdDestroyFieldCard(cardPosition);
                    EffectSpawnSelected(player, false, false, cardPosition);
                    target.CmdDestroyFieldCard(cardPosition);
                    EffectSpawnSelected(target, true, false, cardPosition);
                }
                break;
        }
    }

    public void Damage(PlayerManager player, PlayerManager target)
    {  
        int[] actualAttack = new int[5]{0,0,0,0,0};

        if(cardPosition - 2 >= 0 && cardPosition - 2 < 5)   actualAttack[cardPosition - 2] = attackPattern[0];
        if(cardPosition - 1 >= 0 && cardPosition - 1 < 5)   actualAttack[cardPosition - 1] = attackPattern[1];
        if(cardPosition >= 0 && cardPosition < 5)           actualAttack[cardPosition] = attackPattern[2];
        if(cardPosition + 1 >= 0 && cardPosition + 1 < 5)   actualAttack[cardPosition + 1] = attackPattern[3];
        if(cardPosition + 2 >= 0 && cardPosition + 2 < 5)   actualAttack[cardPosition + 2] = attackPattern[4];

        int totalDamage = 0;
        for(int i = 0; i < 5; i++)
        {  
            if(target.field[i] != null && target.field[i].frozenTime == 0)
            {
                int damage = 0;
                if(cardPosition - 2 == i && actualAttack[i] > 0)
                {
                    damage = Mathf.Max(0, actualAttack[i] - target.field[i].spr - target.field[i].defense);
                    AttackSetup(player, i, damage);   
                    totalDamage += damage;
                }
                else if(cardPosition - 1 == i && actualAttack[i] > 0)
                {
                    damage = Mathf.Max(0, actualAttack[i] - target.field[i].spr - target.field[i].defense);
                    AttackSetup(player, i, damage);   
                    totalDamage += damage;
                }
                else if(cardPosition == i && actualAttack[i] > 0)
                {
                    damage = Mathf.Max(0, actualAttack[i] - target.field[i].spr - target.field[i].defense);
                    AttackSetup(player, i, damage);   
                    totalDamage += damage;
                }
                else if(cardPosition + 1 == i && actualAttack[i] > 0)
                {
                    damage = Mathf.Max(0, actualAttack[i] - target.field[i].spr - target.field[i].defense);
                    AttackSetup(player, i, damage);   
                    totalDamage += damage;
                }
                else if(cardPosition + 2 == i && actualAttack[i] > 0)
                {
                    damage = Mathf.Max(0, actualAttack[i] - target.field[i].spr - target.field[i].defense);
                    AttackSetup(player, i, damage);   
                    totalDamage += damage;
                }
                
                if(damage > 0)
                {
                    target.hp -= damage;
                    target.CmdDestroyFieldCard(i);
                }
            }
            else
            {
                if(cardPosition - 2 == i && actualAttack[i] > 0)
                {
                    target.hp -= actualAttack[i];
                    totalDamage += actualAttack[i];
                    AttackSetup(player, i, actualAttack[i]);       
                }
                else if(cardPosition - 1 == i && actualAttack[i] > 0)
                {
                    target.hp -= actualAttack[i];
                    totalDamage += actualAttack[i];
                    AttackSetup(player, i, actualAttack[i]);   
                }
                else if(cardPosition == i && actualAttack[i] > 0)
                {
                    target.hp -= actualAttack[i];
                    totalDamage += actualAttack[i];
                    AttackSetup(player, i, actualAttack[i]);   
                }
                else if(cardPosition + 1 == i && actualAttack[i] > 0)
                {
                    target.hp -= actualAttack[i];
                    totalDamage += actualAttack[i];
                    AttackSetup(player, i, actualAttack[i]);   
                }
                else if(cardPosition + 2 == i && actualAttack[i] > 0)
                {
                    target.hp -= actualAttack[i];
                    totalDamage += actualAttack[i];
                    AttackSetup(player, i, actualAttack[i]);   
                }
            }
        }
    }

    public void Defend(PlayerManager player)
    {  
        for(int i = 0; i < 5; i++)
        {
            if(spr >= 4)
            {
                if(player.field[i] != null)
                {
                    if(i == cardPosition - 2)
                        player.field[i].defense += 1;
                    else if(i == cardPosition - 1)
                        player.field[i].defense += 1;
                    else if(i == cardPosition + 1)
                        player.field[i].defense += 1;
                    else if(i == cardPosition + 2)
                        player.field[i].defense += 1;
                }
            }
            else if(spr == 2)
            {
                if(player.field[i] != null)
                {
                    if(i == cardPosition - 1)
                        player.field[i].defense += 1;
                    else if(i == cardPosition + 1)
                        player.field[i].defense += 1;
                }
            }
        }
    }
    
    void AttackSetup(PlayerManager player, int i, int damage) 
    {
        if(transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, transform.position, Quaternion.identity);
            attack.GetComponent<Projectile>().destination = player.enemyField.transform.GetChild(4).GetChild(i).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
        else if(transform.parent.parent.parent == player.enemyField.transform)
        {
            var attack = Instantiate(effect, transform.position, Quaternion.identity);
            attack.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(4).GetChild(i).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
    }

    void StealEffectSpawn(PlayerManager player, int child) 
    {
        //child(1) = hp, child(2) = sp, child(3) = decksize
        if(transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, player.enemyField.transform.GetChild(child).position, Quaternion.identity);
            attack.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(child).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
        else if(transform.parent.parent.parent == player.enemyField.transform)
        {
            var attack = Instantiate(effect, player.playerField.transform.GetChild(child).position, Quaternion.identity);
            attack.GetComponent<Projectile>().destination = player.enemyField.transform.GetChild(child).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
    }

    void DrainEffectSpawn(PlayerManager player, int child) 
    {
        //child(1) = hp, child(2) = sp, child(3) = decksize
        if(transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, player.enemyField.transform.GetChild(child).position, Quaternion.identity);
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
        else if(transform.parent.parent.parent == player.enemyField.transform)
        {
            var attack = Instantiate(effect, player.playerField.transform.GetChild(child).position, Quaternion.identity);
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
    }
    
    public void EffectSpawn(PlayerManager player)
    {
        var eff = Instantiate(effect, transform.position, Quaternion.identity);
        eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    void EffectSpawnSelected(PlayerManager player, bool isenemy, bool isHand, int i)
    {
        if(isenemy)
        {
            if(isHand)
            {
                var eff = Instantiate(effect, player.enemyField.transform.GetChild(5).GetChild(i).position, Quaternion.identity);
                eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
                eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
            }
            else
            {
                var eff = Instantiate(effect, player.enemyField.transform.GetChild(4).GetChild(i).position, Quaternion.identity);
                eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
                eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
            }
        }
        else
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
    void LingeringEffectSpawnSelected(FieldCard fieldCard, bool rot)
    {
            var eff = Instantiate(effect, fieldCard.transform.position, Quaternion.identity);
            eff.GetComponent<LingeringEffect>().rotting = rot;
            eff.GetComponent<LingeringEffect>().target = fieldCard;
            eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }
}