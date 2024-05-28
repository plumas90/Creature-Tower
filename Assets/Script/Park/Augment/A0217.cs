using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A0217 : MonoBehaviour
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
        //GameObject fire = PhotonNetwork.Instantiate("AugmentList/A0217", transform.localPosition, Quaternion.identity);
        //A0217_1 a2203 = fire.GetComponent<A0217_1>();
    }
}
