using UnityEngine;

// result_dna 스프라이트 오브젝트에 부착
// 플레이어가 접촉하면 증강 선택 UI(ResultManager)를 열어줌
// 획득 후 오브젝트는 비활성화 (재입장 시 중복 획득 방지)
public class ResultDNA : MonoBehaviour
{
    private bool picked = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (picked) return;
        if (!collision.TryGetComponent(out PlayerStatControl _)) return;

        picked = true;
        gameObject.SetActive(false);

        // 현재 스테이지 기준으로 특수 증강(캐릭터 전용) 리스트 제시
        ResultManager.Instance.SpecialResult();
    }

    // Stage.Awake의 ResultSummon에서 호출 - 방 재사용 시 초기화
    public void Init()
    {
        picked = false;
        gameObject.SetActive(true);
    }
}
