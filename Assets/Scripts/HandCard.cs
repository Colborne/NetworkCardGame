using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class HandCard : BaseCard
{
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
}