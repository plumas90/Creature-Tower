using UnityEngine;

public class Stage : MonoBehaviour
{
    public int roomNumber;

    [Header("Door")]
    public Door botDoor;
    public Door topDoor;

    private bool firstIn;

    protected virtual void Awake()
    {
        firstIn = true;
        ObjActiveFalse();
    }

    public virtual void ReadyStage()
    {
        ObjActiveTrue();
    }

    protected bool TryConsumeFirstIn()
    {
        if (!firstIn)
            return false;

        firstIn = false;
        return true;
    }

    public virtual void InCheckClear(GameObject player)
    {
        if (!TryConsumeFirstIn())
            return;

        Debug.Log($"[Stage] InCheckClear on '{name}' | player={player?.name}");
    }

    public virtual Transform GetPlayerSpawnPoint()
    {
        return null;
    }

    public virtual Vector2 GetRandomPositionInZone()
    {
        return transform.position;
    }

    public virtual bool IsPositionInZone(Vector2 position)
    {
        return false;
    }

    public virtual Vector2 GetZoneCenter()
    {
        return transform.position;
    }

    public virtual Bounds GetZoneBounds()
    {
        return new Bounds(transform.position, new Vector3(10f, 10f, 0f));
    }

    public virtual void RegisterBossSpawnCount(int count)
    {
    }

    public virtual void NotifyBossDied(BossBase deadBoss, int count)
    {
    }

    public void ObjActiveTrue()
    {
        gameObject.SetActive(true);
    }

    public void ObjActiveFalse()
    {
        gameObject.SetActive(false);
    }

    public void OpenBotDoor()
    {
        if (botDoor != null)
            botDoor.UnLock();
    }

    public void CloseBotDoor()
    {
        if (botDoor != null)
            botDoor.Lock();
    }

    public void OpenTopDoor()
    {
        if (topDoor != null)
            topDoor.UnLock();
    }

    public void CloseTopDoor()
    {
        if (topDoor != null)
            topDoor.Lock();
    }
}
