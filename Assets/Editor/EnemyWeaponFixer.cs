using UnityEngine;
using UnityEditor;
using Watermelon.SquadShooter;

public class EnemyWeaponFixer : EditorWindow
{
    [MenuItem("Tools/Fix Enemy Weapons")]
    public static void CheckAndFixWeapons()
    {
        Debug.Log("<b>[Enemy Weapon Fixer] Bắt đầu quét các Enemy Prefabs...</b>");

        // Tìm tất cả các prefab trong project có chứa script BaseEnemyBehavior
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;

            BaseEnemyBehavior enemy = prefab.GetComponent<BaseEnemyBehavior>();
            if (enemy != null)
            {
                count++;
                bool modified = false;

                // Kiểm tra 1: Xem Weapon GameObject có bị Disable không?
                WeaponRigBehavior[] weapons = prefab.GetComponentsInChildren<WeaponRigBehavior>(true); // Lấy cả những cái đang bị ẩn
                foreach (var weapon in weapons)
                {
                    if (!weapon.gameObject.activeSelf)
                    {
                        weapon.gameObject.SetActive(true);
                        Debug.LogWarning($"[Fix] Đã bật lại (SetActive=true) cho vũ khí <b>{weapon.gameObject.name}</b> trên Enemy <b>{prefab.name}</b> vì nó đang bị ẩn!");
                        modified = true;
                    }

                    // Kiểm tra 2: Kiểm tra cấu hình Anchor
                    SerializedObject weaponSo = new SerializedObject(weapon);
                    Transform primaryAnchor = weaponSo.FindProperty("primaryHandAnchor").objectReferenceValue as Transform;
                    if (primaryAnchor == null)
                    {
                        Debug.LogError($"[Lỗi] Vũ khí <b>{weapon.gameObject.name}</b> trên <b>{prefab.name}</b> chưa được gán <b>Primary Hand Anchor</b>!");
                    }

                    int rigType = weaponSo.FindProperty("rigType").enumValueIndex;
                    if (rigType == 1) // TwoHanded
                    {
                        Transform offAnchor = weaponSo.FindProperty("offHandAnchor").objectReferenceValue as Transform;
                        if (offAnchor == null)
                        {
                            Debug.LogError($"[Lỗi] Vũ khí <b>{weapon.gameObject.name}</b> trên <b>{prefab.name}</b> đang để Rig Type là <b>Two Handed</b> nhưng thiếu <b>Off Hand Anchor</b>! Đã tự động chuyển về One Handed.");
                            weaponSo.FindProperty("rigType").enumValueIndex = 0; // Đổi về OneHanded
                            weaponSo.ApplyModifiedProperties();
                            modified = true;
                        }
                    }
                }

                // Kiểm tra 3: Xem Hand Bones có bị null không
                SerializedObject enemySo = new SerializedObject(enemy);
                Transform rightBone = enemySo.FindProperty("rightHandBone").objectReferenceValue as Transform;
                Transform leftBone = enemySo.FindProperty("leftHandBone").objectReferenceValue as Transform;

                if (rightBone == null || leftBone == null)
                {
                    Debug.LogError($"[Lỗi] Enemy <b>{prefab.name}</b> chưa được gán đầy đủ Right Hand Bone hoặc Left Hand Bone!");
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    fixedCount++;
                }
            }
        }

        Debug.Log($"<b>[Enemy Weapon Fixer] Hoàn thành! Đã quét {count} Enemy prefabs. Tự động sửa chữa {fixedCount} prefabs.</b>");
        Debug.Log("<i>Hãy kiểm tra Console để xem các lỗi [Lỗi] màu đỏ. Nếu có, bạn cần tự gán tay vào bằng tay theo hướng dẫn của lỗi đó!</i>");
    }
}
