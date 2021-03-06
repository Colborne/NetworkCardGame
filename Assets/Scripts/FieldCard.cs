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
        ClearBoard,
        ClearField,
        RemoveCard,
        Swap,
        Rearrange,
        Draw, 
        Spawn,   
        Evolve,
        Duplicate,
        Bomb, //Hit before heals and before damage but after swap
        Heal,
        Summoning,
        DrainLife,
        StealLife,
        DrainMana,
        StealMana,
        Blitz,
        Damage, //After Healing
        Freeze, //Order Doesn't Matter
        DeckBurn, //Order Doesn't Matter
        Rot, //Order Doesn't Matter
        Sight, //Order Doesn't Matter
        Sacrifice, //Happens During Enemy Turn
        Defend, //Happens During Enemy Turn
        DeckCard, //Happens at End of Turn
        ReturnToDeck,
        ConvertToMana,
        ManaBoost,
        Luck
    }

    /*
        Reshuffle,
        ConvertHand,
        ManaBoost

        Fix Connection boxes!
        Reshuffle card 1 mana
        Sacrifice for mana
        Straight Discard
    */

    public int[] attackPattern;
    public Ability ability;
    public GameObject effect;
    public ScriptableCard spawn;
    public int priority;
    public int defense;
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

        if(frozenTimer > 0)
            GetComponent<Image>().color = new Color32(200,200,225,255);
        else if(transform.parent != null && GetComponentInParent<Slot>().rot)
            GetComponent<Image>().color = new Color32(55,150,110,255);
        else
            GetComponent<Image>().color = new Color32(255,255,255,255);
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
                player.hp -=spr;
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
                    var trgt = target.field[cardPosition].cardData.data;
                    var plyr = player.field[cardPosition].cardData.data;

                    target.field[cardPosition].cardData = new CardInfo(plyr);
                    target.field[cardPosition].title = plyr.title;
                    target.field[cardPosition].spr = plyr.spr;
                    target.field[cardPosition].portrait = plyr.portrait;
                    target.field[cardPosition].attackPattern = plyr.attackPattern;
                    target.field[cardPosition].ability = (FieldCard.Ability)plyr.ability;
                    target.field[cardPosition].effect = plyr.effect;
                    target.field[cardPosition].spawn = plyr.spawn;
                    GetComponent<Image>().sprite = portrait;

                    player.field[cardPosition].cardData = new CardInfo(trgt);
                    player.field[cardPosition].title = trgt.title;
                    player.field[cardPosition].spr = trgt.spr;
                    player.field[cardPosition].portrait = trgt.portrait;
                    player.field[cardPosition].attackPattern = trgt.attackPattern;
                    ability = (FieldCard.Ability)trgt.ability;
                    player.field[cardPosition].effect = trgt.effect;
                    player.field[cardPosition].spawn = trgt.spawn;
                    GetComponent<Image>().sprite = portrait;

                    //player.RpcDisplayCard(target.field[cardPosition].gameObject, cardPosition);
                    //target.RpcDisplayCard(this.gameObject, cardPosition);
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
                target.hp -= spr;
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
                if(target.sp >= spr)
                    target.sp -= spr; 
                else
                    target.sp = 0;
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
                EffectSpawn(player);
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
                    target.CmdFreeze(cardPosition, spr);
                    EffectSpawnSelected(player, false, false, cardPosition);
                    EffectSpawnSelected(target, true, false, cardPosition);
                    player.CmdDestroyFieldCard(cardPosition);   
                }
                break;
            case Ability.Rot:
                target.CmdRot(cardPosition, true);
                EffectSpawnSelected(player, false, false, cardPosition);
                EffectSpawnSelected(target, true, false, cardPosition);
                if (spr == 4)
                {
                    if(0 <= cardPosition - 1 && cardPosition - 1 <= 4)
                    {
                        target.CmdRot(cardPosition - 1, true);
                        EffectSpawnSelected(target, true, false, cardPosition - 1);
                    }
                    if(0 <= cardPosition + 1 && cardPosition + 1 <= 4)
                    {
                        target.CmdRot(cardPosition + 1, true);
                        EffectSpawnSelected(target, true, false, cardPosition + 1);
                    }
                }
                player.CmdDestroyFieldCard(cardPosition);
                break;
            case Ability.Sacrifice:
                player.CmdSetHealth(spr * 2);
                player.CmdDestroyFieldCard(cardPosition);
                EffectSpawn(player);
                break;
            case Ability.ManaBoost:
                player.CmdSetMana(spr * 3);
                player.CmdDestroyFieldCard(cardPosition);
                EffectSpawn(player);
                break;
            case Ability.ReturnToDeck:
                for(int i = 0; i < 5; i++)
                {
                    if(player.hand[i] != null)
                    {
                        player.deck.Enqueue(player.hand[i].cardData);
                        player.CmdDestroyHandCard(i);
                        ConversionEffectSpawn(player, i, 3);
                    }
                }
                player.CmdDestroyFieldCard(cardPosition);
                EffectSpawn(player);
                break;
            case Ability.ConvertToMana:
                int totalMana = 0;
                for(int i = 0; i < 5; i++)
                {
                    if(player.hand[i] != null)
                    {
                        totalMana += (player.hand[i].spr / 2);
                        player.CmdDestroyHandCard(i);
                        ConversionEffectSpawn(player, i, 2);
                    }
                }
                player.CmdSetMana(totalMana);
                player.CmdDestroyFieldCard(cardPosition);
                EffectSpawn(player);
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
                    target.CmdDestroyFieldCard(cardPosition);
                    AttackSetup(player, cardPosition, 0); 
                }
                break;
            case Ability.Luck:
                if(spr == 2)
                {
                    Ability[] luck = new Ability[]
                    {
                        Ability.Bomb, Ability.Draw, Ability.Heal, Ability.Blitz, Ability.Sacrifice, Ability.Freeze, Ability.Rot, 
                        Ability.Rearrange, Ability.ReturnToDeck, Ability.ClearField, Ability.Swap, Ability.Sight
                    };
                    ability = luck[Random.Range(0,luck.Length-1)];
                }
                if(spr == 4)
                {
                    Ability[] luck = new Ability[]
                    {
                        Ability.Heal, Ability.Blitz, Ability.Sacrifice, Ability.Freeze, Ability.Rot, Ability.Rearrange, Ability.ReturnToDeck, Ability.ClearField, 
                        Ability.ManaBoost, Ability.DrainLife,Ability.StealLife, Ability.DrainMana,Ability.StealMana, Ability.DeckCard, Ability.Defend
                    };
                    ability = luck[Random.Range(0,luck.Length-1)];
                }
            Debug.Log(ability);
            UseAbility(player, target);
            ability = Ability.Luck;
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
            if(target.field[i] != null && target.field[i].frozenTimer == 0)
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
                        player.CmdDefense(i, 1);
                    else if(i == cardPosition - 1)
                        player.CmdDefense(i, 1);
                    else if(i == cardPosition + 1)
                        player.CmdDefense(i, 1);
                    else if(i == cardPosition + 2)
                        player.CmdDefense(i, 1);
                }
            }
            else if(spr == 3)
            {
                if(player.field[i] != null)
                {
                    if(i == cardPosition - 1)
                        player.CmdDefense(i, 1);
                    else if(i == cardPosition + 1)
                        player.CmdDefense(i, 1);
                }
            }
        }
    }
    
    void AttackSetup(PlayerManager player, int i, int damage) 
    {
        if(transform.parent != null && transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, transform.position, Quaternion.identity);
            if(attack.GetComponent<Projectile>())
                attack.GetComponent<Projectile>().destination = player.enemyField.transform.GetChild(4).GetChild(i).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        }
        else if(transform.parent != null && transform.parent.parent.parent == player.enemyField.transform)
        {
            var attack = Instantiate(effect, transform.position, Quaternion.identity);
            if(attack.GetComponent<Projectile>())
                attack.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(4).GetChild(i).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
    }

    void StealEffectSpawn(PlayerManager player, int child) 
    {
        //child(1) = hp, child(2) = sp, child(3) = decksize
        if(transform.parent != null && transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, player.enemyField.transform.GetChild(child).position, Quaternion.identity);
            if(attack.GetComponent<Projectile>())
                attack.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(child).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
        else if(transform.parent != null && transform.parent.parent.parent == player.enemyField.transform)
        {
            var attack = Instantiate(effect, player.playerField.transform.GetChild(child).position, Quaternion.identity);
            if(attack.GetComponent<Projectile>())
                attack.GetComponent<Projectile>().destination = player.enemyField.transform.GetChild(child).position;
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
    }

    void DrainEffectSpawn(PlayerManager player, int child) 
    {
        //child(1) = hp, child(2) = sp, child(3) = decksize
        if(transform.parent != null && transform.parent.parent.parent == player.playerField.transform)
        {
            var attack = Instantiate(effect, player.enemyField.transform.GetChild(child).position, Quaternion.identity);
            attack.GetComponent<RectTransform>().localScale = new Vector3(1,1,1); 
        }
        else if(transform.parent != null && transform.parent.parent.parent == player.enemyField.transform)
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

    void ConversionEffectSpawn(PlayerManager player, int child, int target)
    {
        var eff = Instantiate(effect, player.playerField.transform.GetChild(5).GetChild(child).position, Quaternion.identity);
        eff.GetComponent<RectTransform>().SetParent(FindObjectOfType<Canvas>().transform);
        if(eff.GetComponent<Projectile>())
            eff.GetComponent<Projectile>().destination = player.playerField.transform.GetChild(target).position;
        eff.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
        //child(1) = hp, child(2) = sp, child(3) = decksize
    }
}