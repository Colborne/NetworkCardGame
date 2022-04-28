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
            // Return ScriptableCard from our cached list, based on the card's uniqueID.
            return ScriptableCard.Cache[cardID];
        }
    }

    public Sprite image => data.portrait;
    public string title => data.title;
    public int spr => data.spr;
}