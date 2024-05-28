
using UnityEngine;

public class A3204 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTimeController;
    GameObject Prefabs;
    A3204_1 nullcheck;

    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnEndRollEvent += Makeshield;
            nullcheck = null;

    }

    void Makeshield()
    {

            if (nullcheck == null)
            {
            /*
                Prefabs = PhotonNetwork.Instantiate("AugmentList/A3204_1", Vector3.zero, Quaternion.identity);
                int PvNum= Prefabs.GetPhotonView().ViewID;
                nullcheck = Prefabs.GetComponent<A3204_1>();
                nullcheck.Init(playerStat);
                photonView.RPC("FindMaster", RpcTarget.All, PvNum);
            */
            }
            else
            {
                nullcheck.reloading();
            }
    }
    /*
    private void FindMaster(int num)
    {
        PhotonView a = PhotonView.Find(num);
        a.transform.SetParent(this.gameObject.transform);
        a.transform.localPosition = Vector3.zero;
    }
    */
}
