using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class BaseCard : NetworkBehaviour
{
    [SyncVar] public CardInfo cardData;
    public string title;
    public int spr;
    public Sprite portrait;
    public bool selected = false;
    public int cardPosition;
    public Sprite CardBack;
    [SyncVar(hook = nameof(UpdateTimer))] public int frozenTimer;

    public virtual void Update()
    {
        GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
    }

    public void UpdateTimer(int oldTime, int newTime)
    {
        frozenTimer = newTime;
    }
}