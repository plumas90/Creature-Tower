using UnityEngine;

// result_dna 스프라이트 오브젝트에 부착
// 플레이어가 접촉하면 증강 선택 UI(ResultManager)를 열어줌
// 획득 후 오브젝트는 비활성화 (재입장 시 중복 획득 방지)
public class ResultDNA : MonoBehaviour
{
    private bool picked = false;
    public Sprite resultdna;
    public Sprite resultdna_red;

    private void Awake()
    {
        RegisterChildTriggerRelays();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryConsume(collision);
    }

    // Stage.Awake의 ResultSummon에서 호출 - 방 재사용 시 초기화
    public void Init()
    {
        picked = false;
        gameObject.SetActive(true);
    }

    public void TryConsume(Collider2D collision)
    {
        if (picked) return;
        if (collision == null) return;

        PlayerStatControl playerStat = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStat == null) return;
        if (ResultManager.Instance == null) return;

        picked = true;
        gameObject.SetActive(false);

        // 현재 플레이어 기준으로 증강 매니저를 재동기화한 뒤 결과 UI를 연다.
        ResultManager.Instance.OpenSpecialResult(playerStat.gameObject);
    }

    private void RegisterChildTriggerRelays()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject)
                continue;

            ResultDNATriggerRelay relay = col.GetComponent<ResultDNATriggerRelay>();
            if (relay == null)
                relay = col.gameObject.AddComponent<ResultDNATriggerRelay>();

            relay.Init(this);
        }
    }
}

public class ResultDNATriggerRelay : MonoBehaviour
{
    private ResultDNA owner;

    public void Init(ResultDNA target)
    {
        owner = target;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner == null)
            owner = GetComponentInParent<ResultDNA>();

        if (owner != null)
            owner.TryConsume(collision);
    }
}
