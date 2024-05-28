using UnityEngine;


public class A0120 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            controller.OnRollEvent += CreateWater;
    }
    // Update is called once per frame
    void CreateWater()
    {
        //Instantiate("AugmentList/A0120", this.transform.localPosition, Quaternion.identity);
        //PhotonNetwork.Instantiate("AugmentList/A0120", this.transform.localPosition, Quaternion.identity);
    }

}
