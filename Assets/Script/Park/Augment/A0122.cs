using UnityEngine;

public class A0122 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStatHandler;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStatHandler= GetComponent<PlayerStatControl>();
            controller.OnRollEvent += CreateFire;
    }
    // Update is called once per frame
    void CreateFire()
    {
        /*
        GameObject A =PhotonNetwork.Instantiate("AugmentList/A0122", this.transform.localPosition, Quaternion.identity);
        A0122_1 AB= A.GetComponent<A0122_1>();
        AB.Init(photonView.ViewID, playerStatHandler.ATK.total);
        */
    }
}
