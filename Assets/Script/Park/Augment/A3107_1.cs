using UnityEngine;

public class A3107_1 : MonoBehaviour
{
    public float damage;
    private void OnTriggerExit2D(Collider2D collision)
    {
        //EnemyAI wjr = collision.GetComponent<EnemyAI>();
        //if (wjr != null )
        //{
        //   wjr.PV.RPC("DecreaseHP", RpcTarget.All, damage);
        //}
    }
    public void DamegeUpdate(float a) 
    {
        damage = a*0.8f;
    }

}
