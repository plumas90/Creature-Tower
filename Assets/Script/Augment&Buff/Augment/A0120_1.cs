using UnityEngine;

public class A0120_1 : MonoBehaviour
{
    public float time = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            //Debuff.Instance.GiveWater(collision.gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        { 
            //Debuff.Instance.GiveWater(collision.gameObject);
        }
    }
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= 5f)
        {
            Destroy(gameObject);
        }
    }
}
