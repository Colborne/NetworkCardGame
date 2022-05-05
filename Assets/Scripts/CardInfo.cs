using UnityEngine;
using System;

[Serializable]
public struct CardInfo
{
    public int cardID;
    public CardInfo(ScriptableCard data)
    {
        cardID = data.cardID;
    }
    public ScriptableCard data
    {
        get
        {
            return ScriptableCard.Cache[cardID];
        }
    }

    public Sprite image => data.portrait;
    public string title => data.title;
    public int spr => data.spr;
    public int[] attackPattern => data.attackPattern;
    public int ability => (int)data.ability;
    public GameObject effect => data.effect;
    public ScriptableCard spawn => data.spawn;
    public string fusion => data.fusion;
}