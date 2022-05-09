using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 destination;
    void Update()
    { 
        if(Vector3.Distance(GetComponent<RectTransform>().position, destination) > .1f)
            GetComponent<RectTransform>().position = Vector3.Lerp(GetComponent<RectTransform>().position, destination, .015f);
        else
            Destroy(gameObject);
    }      
}