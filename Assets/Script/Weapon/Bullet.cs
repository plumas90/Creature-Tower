using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum BulletTarget
{
    Player,
    Enemy,
    All
}

public class Bullet : MonoBehaviour
{
    Vector2 collisionVector;

    public float ATK;
    public float BulletLifeTime;
    public float BulletSpeed = 15;
    public bool IsDamage = false;
    public bool fire;
    public bool water;
    // public bool ice;
    public bool burn;
    // public bool gravity;
    // public bool Penetrate;
    // public bool canresurrection;
    // public bool sniperAtkBuff;

    private HumanAttackintelligentmissile missile;

    public Dictionary<string, int> targets;

    public Vector2 _direction;
    float time = 0f;
    public int layerMask;

    public bool locator;
    public bool sniping;

    public int BulletOwner;

    RaycastHit2D hit;
    public bool canAngle;
    Vector3 income;
    Vector3 normal;
    [SerializeField] private float wallCollisionArmDelay = 0.03f;
    [SerializeField] private string sortingLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 2;
    private float spawnedAtTime;
    private SpriteRenderer spriteRenderer;
    private HashSet<int> damagedBossIds = new HashSet<int>();

    private void Awake()
    {
        targets = new Dictionary<string, int>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void Init()
    {        
        time = 0f;
        spawnedAtTime = Time.time;
        damagedBossIds.Clear();
        BulletLifeTime = UnityEngine.Random.Range(BulletLifeTime * 0.15f, BulletLifeTime * 0.2f);
        _direction = transform.right;
        layerMask = 1 << LayerMask.NameToLayer("Wall");
    }

    public void MissileFire(int i) 
    {
        missile = GetComponentInChildren<HumanAttackintelligentmissile>();
        missile.init(i);
    }

    void Update()
    {
        transform.Translate(Vector3.right * BulletSpeed * Time.deltaTime);
        ApplyYBasedSorting();
        time += Time.deltaTime;
        if (time>= BulletLifeTime) 
        {
            Destroy();
        }
        if (locator) 
        {
            ATK -= ATK*3f * Time.deltaTime;
            Debug.Log($"locator ATK: {ATK} time: {time}");
        }
        if (sniping) 
        {
            ATK += ATK * 2f * Time.deltaTime;
            Debug.Log($"sniping ATK: {ATK} time: {time}");
        }
    }

    private void ApplyYBasedSorting()
    {
        if (spriteRenderer == null)
            return;

        if (!string.IsNullOrEmpty(sortingLayerName))
            spriteRenderer.sortingLayerName = sortingLayerName;

        int order = ySortBaseOrder - Mathf.RoundToInt(transform.position.y * ySortScale) + ySortOrderOffset;
        spriteRenderer.sortingOrder = order;
    }

    public void Destroy()
    {
        gameObject.SetActive(false);
    }

    public bool TryMarkBossHit(int bossInstanceId)
    {
        if (bossInstanceId == 0)
            return true;

        if (damagedBossIds.Contains(bossInstanceId))
            return false;

        damagedBossIds.Add(bossInstanceId);
        return true;
    }

    private float GetAngle(Vector2 vec1, Vector2 vec2) 
    {
        float angle = (Mathf.Atan2(vec2.y, vec2.x) - Mathf.Atan2(vec1.y, vec1.x)) * Mathf.Rad2Deg;
        return angle;
    }

    public float CalculateAngle(Vector3 from, Vector3 to)
    {
        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;
    }

    public static float GetAngle2(Vector3 from, Vector3 to)
    {
        Vector3 v = to - from;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (IsBlockingWallCollision(collision))
        {
            if (canAngle)
            {
                hit = Physics2D.Raycast(this.transform.position, _direction, BulletSpeed * BulletLifeTime, layerMask);
                Debug.DrawRay(this.transform.position, _direction, UnityEngine.Color.red, 3f);

                if (!hit)
                {
                    Destroy();
                    return;
                }

                Vector3 reflectVector = Vector3.Reflect(_direction, hit.normal).normalized;
                float angle = Mathf.Atan2(reflectVector.y, reflectVector.x) * Mathf.Rad2Deg;

                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                this.transform.rotation = rotation;
                _direction = reflectVector;
            }
            else 
            {
                Destroy();
            }
            return;
        }
        else if ((targets.ContainsValue((int)BulletTarget.Player) || targets.ContainsValue((int)BulletTarget.All))
            && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerStatControl playerStat = collision.gameObject.GetComponentInParent<PlayerStatControl>();
            if (playerStat != null)
            {
                if (IsDamage)
                {
                    playerStat.TryApplyContactDamage(ATK, BulletOwner);
                }
            }
            Destroy();
            return;
        }
        // else if (Penetrate)
        // {
        //     return;
        // }
        else if (targets.ContainsValue((int)BulletTarget.Enemy) && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Destroy();
            return;
        }
    }

    private bool IsBlockingWallCollision(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Wall"))
            return false;

        if (Time.time - spawnedAtTime < wallCollisionArmDelay)
            return false;

        return true;
    }
}
