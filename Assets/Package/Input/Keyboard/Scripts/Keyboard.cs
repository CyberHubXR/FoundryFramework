using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keyboard : MonoBehaviour
{
    public GameObject lowercaseHolder;
    public GameObject uppercaseHolder;
    
    bool capital;
    
    public void CapitalSwitch () 
    {
        capital = !capital;
        
        uppercaseHolder.SetActive(capital);
    }
}
