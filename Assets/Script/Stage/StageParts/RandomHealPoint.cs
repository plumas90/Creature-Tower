using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomHealPoint : MonoBehaviour
{
    public Potion potion;


    public void MakePotion() 
    {
        potion.Init();
    }
}
