using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public int slotNumber;
    public bool rot;
    public Sprite img;

    private void Update() 
    {
        if(rot)
            GetComponent<Image>().sprite = img;
        else
            GetComponent<Image>().sprite = null;
    }
}
