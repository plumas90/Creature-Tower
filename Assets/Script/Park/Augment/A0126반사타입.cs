using UnityEngine;

public class A0126 : MonoBehaviour
{
    private PlayerStatControl playerStatHandler;
    private CapsuleCollider2D capsuleColl;
    public float DamageCoeff;

    private void Awake()
    {
        playerStatHandler = GetComponent<PlayerStatControl>();
        capsuleColl = GetComponent<CapsuleCollider2D>();
        DamageCoeff = 0.15f;
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            var collObject = coll.gameObject;
            //var collEnemy = collObject.GetComponent<EnemyAI>();

            //if (collEnemy == null)
            //{
            //    return;
            //}

            //collEnemy.knockbackDistance = 3f;
        }
    }
}
