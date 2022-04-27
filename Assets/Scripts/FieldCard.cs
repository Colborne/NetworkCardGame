using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class FieldCard : NetworkBehaviour
{
    [SyncVar] public CardInfo cardData;

    public string title;
    public int spr;
    public Sprite portrait;
    public bool clicked = false;

    public void Update()
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