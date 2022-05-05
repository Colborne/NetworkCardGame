using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ScriptableCard", menuName = "NetworkCardGame/ScriptableCard", order = 0)]
public class ScriptableCard : ScriptableObject 
{
    public int cardID;
    public string title;
    public int spr;
    public Sprite portrait;
    public int[] attackPattern;
    public FieldCard.Ability ability;
    public GameObject effect;
    public ScriptableCard spawn;
    public string fusion;

    static Dictionary<int, ScriptableCard> _cache;
    public static Dictionary<int, ScriptableCard> Cache
    {
        get
        {
            if (_cache == null)
            {
                ScriptableCard[] cards = Resources.LoadAll<ScriptableCard>("");
                _cache = cards.ToDictionary(card => card.cardID, card => card);
            }
            return _cache;
        }
    }
}