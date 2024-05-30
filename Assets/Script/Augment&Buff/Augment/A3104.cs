
using UnityEngine;

public class A3104 : MonoBehaviour
{
    private PlayerStatControl playerStatHandler;
    private TopDownMoveBase topDown;
    private CapsuleCollider2D capsuleColl;
    public float DamageCoeff;
    public bool isRoll;

    private void Awake()
    {
        playerStatHandler = GetComponent<PlayerStatControl>();
        capsuleColl = GetComponent<CapsuleCollider2D>();
        topDown = GetComponent<TopDownMoveBase>();
        DamageCoeff = 0.15f;
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        isRoll = topDown.isRoll;            
        if (topDown.isRoll)
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
}
