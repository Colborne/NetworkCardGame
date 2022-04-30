using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentCard : MonoBehaviour
{
    public CardInfo cardData;
    public Sprite portrait;
    void Update()
    {
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
        Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0);
        GetComponent<RectTransform>().position =  fixedPosition; 
    }
}
