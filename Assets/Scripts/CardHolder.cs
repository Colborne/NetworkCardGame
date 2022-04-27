using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardHolder : MonoBehaviour
{
    public Card card;
    public TMP_Text amount;

    private void Start() {
        GetComponent<Button>().image.sprite = card.portrait;
    }
}
