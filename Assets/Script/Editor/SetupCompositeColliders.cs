using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class SetupCompositeColliders : Editor
{
    [MenuItem("Tools/Apply Composite Colliders to Stages")]
    public static void ApplyToStages()
    {
        string[] stagePaths = new string[]
        {
            "Assets/Prefabs/Map/StageCaptainCrap.prefab",
            "Assets/Prefabs/Map/StageThreeMonkey.prefab",
            "Assets/Prefabs/Map/StageTheWorm.prefab",
            "Assets/Prefabs/Map/StageBases.prefab",
            "Assets/Prefabs/Map/StageGreenPea.prefab",
            "Assets/Prefabs/Map/StageTurtle.prefab",
            "Assets/Prefabs/Map/StageHauntedCrystalBall.prefab",
            "Assets/Prefabs/Map/NormalStageBases.prefab",
            "Assets/Prefabs/NormalStage/NormalStage1~5.prefab"
        };

        int modifiedCount = 0;

        foreach (string path in stagePaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogWarning($"[SetupCompositeColliders] 프리팹을 로드할 수 없습니다: {path}");
                continue;
            }

            TilemapCollider2D[] tilemapColliders = root.GetComponentsInChildren<TilemapCollider2D>(true);
            bool isModified = false;

            foreach (var tilemapCollider in tilemapColliders)
            {
                GameObject obj = tilemapCollider.gameObject;

                // CompositeCollider2D 컴포넌트 유무 확인 후 추가
                CompositeCollider2D composite = obj.GetComponent<CompositeCollider2D>();
                if (composite == null)
                {
                    composite = obj.AddComponent<CompositeCollider2D>();
                    Debug.Log($"[SetupCompositeColliders] {path} -> {obj.name}에 CompositeCollider2D 추가됨.");
                    isModified = true;
                }

                // 동반 생성된 Rigidbody2D 가져오기 및 Static 설정
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                {
                    rb.bodyType = RigidbodyType2D.Static;
                    Debug.Log($"[SetupCompositeColliders] {path} -> {obj.name}의 Rigidbody2D를 Static으로 변경함.");
                    isModified = true;
                }

                // Used by Composite 체크 활성화
                if (!tilemapCollider.usedByComposite)
                {
                    tilemapCollider.usedByComposite = true;
                    Debug.Log($"[SetupCompositeColliders] {path} -> {obj.name}의 TilemapCollider2D.usedByComposite를 true로 설정함.");
                    isModified = true;
                }
            }

            if (isModified)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                modifiedCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"{modifiedCount}개의 스테이지 프리팹에 Composite Collider 설정을 성공적으로 적용했습니다.", "확인");
    }
}
