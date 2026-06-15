using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class SetupTurtleBossPrefab
{
    static SetupTurtleBossPrefab()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        if (EditorPrefs.GetBool("SetupTurtleBossPrefabDone3", false))
            return;

        string bossPrefabPath = "Assets/Prefabs/Boss/TurtleBoss/Boss_Turtle.prefab";
        string soDir = "Assets/SOData/Boss/TurtleBoss";
        string soPath = "Assets/SOData/Boss/TurtleBoss/TurtleBossSO.asset";
        
        // 1. Create SO Data Directory if not exists
        if (!Directory.Exists(soDir))
        {
            Directory.CreateDirectory(soDir);
            AssetDatabase.Refresh();
        }

        // 2. Create or Load SO
        TurtleBossSO so = AssetDatabase.LoadAssetAtPath<TurtleBossSO>(soPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<TurtleBossSO>();
            so.hp = 1000;
            so.atk = 10;
            so.speed = 3;
            so.bossCount = 1;
            AssetDatabase.CreateAsset(so, soPath);
        }

        // 3. Assign Bullets to SO
        GameObject missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss/TurtleBoss/TurtleMissilebullet.prefab");
        GameObject thornPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss/TurtleBoss/TurtleThornbullet.prefab");
        
        so.missileBulletPrefab = missilePrefab;
        so.thornBulletPrefab = thornPrefab;

        // 4. Assign Sprites to SO
        string sheetPath = "Assets/sprite/Boss/TurtleBoss/TurtleBoss_Sheet.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        System.Collections.Generic.List<Sprite> idleSpritesList = new System.Collections.Generic.List<Sprite>();
        System.Collections.Generic.List<Sprite> rollingSpritesList = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                if (s.name.ToLower().Contains("idle"))
                    idleSpritesList.Add(s);
                else if (s.name.ToLower().Contains("roll"))
                    rollingSpritesList.Add(s);
            }
        }
        
        idleSpritesList.Sort((a, b) => a.name.CompareTo(b.name));
        rollingSpritesList.Sort((a, b) => a.name.CompareTo(b.name));

        so.idleSprites = idleSpritesList.ToArray();
        so.rollingSprites = rollingSpritesList.ToArray();

        EditorUtility.SetDirty(so);

        // 5. Clean up Prefab missing scripts and assign SO
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);
        if (bossPrefab != null)
        {
            // Remove missing scripts
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bossPrefab);

            // Get TurtleBoss component
            TurtleBoss tb = bossPrefab.GetComponent<TurtleBoss>();
            if (tb != null)
            {
                // Assign SO via SerializedObject to access private fields
                SerializedObject serializedBoss = new SerializedObject(tb);
                SerializedProperty soProp = serializedBoss.FindProperty("turtleSO");
                if (soProp != null)
                {
                    soProp.objectReferenceValue = so;
                    
                    Transform weaponPivot = bossPrefab.transform.Find("WeaponPivot");
                    SerializedProperty aimProp = serializedBoss.FindProperty("bossAim");
                    if (aimProp != null && weaponPivot != null)
                    {
                        aimProp.objectReferenceValue = weaponPivot;
                    }
                    serializedBoss.ApplyModifiedProperties();
                }
            }

            EditorUtility.SetDirty(bossPrefab);
        }

        AssetDatabase.SaveAssets();
        EditorPrefs.SetBool("SetupTurtleBossPrefabDone3", true);
        Debug.Log("TurtleBoss prefab and SO setup completed automatically!");
    }
}

