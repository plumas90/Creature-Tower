using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A2203_1 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnEndRollEvent += MakeHeal;
    }
    // Update is called once per frame
    void MakeHeal()
    {
        /*
        GameObject fire = PhotonNetwork.Instantiate("AugmentList/A2203",transform.localPosition,Quaternion.identity);
        A2203 a2203 = fire.GetComponent<A2203>();
        */
    }
}
