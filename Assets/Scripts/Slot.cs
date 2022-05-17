using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public int slotNumber;
    public bool rot;

    private void Update() {
        if(rot)
            GetComponent<Image>().color = new Color32(60,85,60,255);
        else
            GetComponent<Image>().color = new Color32(255,255,255,255);
    }
}
