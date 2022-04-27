using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardHolder : MonoBehaviour
{
    public ScriptableCard card;
    private void Start() {
        GetComponent<Button>().image.sprite = card.portrait;
    }
}
