using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelite
{
    /// <summary>可选便捷工具：一键生成主场景、落盘配置资产、占位精灵。不参与运行时逻辑。</summary>
    public static class RogueliteMenu
    {
        const string SceneDir = "Assets/_Project/Scenes";
        const string ScenePath = SceneDir + "/Main.unity";
        const string SoDir = "Assets/_Project/ScriptableObjects";
        const string ArtDir = "Assets/_Project/Art/Sprites";

        [MenuItem("Roguelite/构造 Main 场景")]
        public static void BuildMainScene()
        {
            EnsureFolder(SceneDir, "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
            new GameObject("GameBootstrap", typeof(GameBootstrap));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Main 场景已生成: " + ScenePath);
        }

        [MenuItem("Roguelite/生成配置资产")]
        public static void BuildConfigAssets()
        {
            EnsureFolder(SoDir, "ScriptableObjects");
            Shop("Shop_伤害", "伤害+6", StatType.Damage, 15, 8, 6f);
            Shop("Shop_攻速", "攻速×0.85", StatType.AttackSpeed, 15, 8, 0.85f);
            Shop("Shop_生命", "生命+20", StatType.MaxHP, 10, 5, 20f);
            Shop("Shop_移速", "移速+0.5", StatType.MoveSpeed, 10, 5, 0.5f);
            Shop("Shop_射程", "射程+1", StatType.Range, 10, 5, 1f);
            Shop("Shop_暴击", "暴击+8%", StatType.CritChance, 20, 10, 0.08f);

            Weapon("Weapon_手枪", "手枪", WeaponType.Ranged, 10f, 0.6f, 8f, 10f, Color.yellow);
            Weapon("Weapon_冲锋枪", "冲锋枪", WeaponType.Ranged, 5f, 0.18f, 7f, 12f, Color.cyan);
            Weapon("Weapon_砍刀", "砍刀", WeaponType.Melee, 14f, 0.8f, 1.6f, 0f, Color.green);
            Weapon("Weapon_手雷", "手雷", WeaponType.AoE, 8f, 2f, 3.5f, 0f, Color.red);

            Enemy("Enemy_Chaser", EnemyType.Chaser, 20f, 2.6f, 10f, 1.5f, 0f, 0f, 8f, 1, 2, 1f, new Color(0.9f, 0.3f, 0.3f));
            Enemy("Enemy_Ranged", EnemyType.Ranged, 15f, 1.8f, 0f, 2.5f, 4.5f, 8f, 8f, 1, 2, 1f, new Color(0.7f, 0.4f, 0.9f));
            Enemy("Enemy_Tank", EnemyType.Tank, 60f, 1.2f, 20f, 1.5f, 0f, 0f, 8f, 3, 5, 1.6f, new Color(0.5f, 0.5f, 0.6f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Roguelite 配置资产已生成到: " + SoDir);
        }

        [MenuItem("Roguelite/生成占位精灵")]
        public static void BuildPlaceholderSprites()
        {
            EnsureFolder(ArtDir, "Sprites");
            WriteCircleSprite(ArtDir + "/circle.png");
            Debug.Log("占位精灵已生成: " + ArtDir);
        }

        #region Helpers
        static void EnsureFolder(string dir, string leaf)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;
            if (!AssetDatabase.IsValidFolder("Assets/_Project")) AssetDatabase.CreateFolder("Assets", "_Project");
            AssetDatabase.CreateFolder("Assets/_Project", leaf);
        }

        static void Shop(string asset, string name, StatType s, int baseP, int step, float add)
        {
            var i = LoadOrCreate<ShopItemData>(SoDir + "/" + asset + ".asset");
            i.displayName = name; i.statType = s; i.basePrice = baseP; i.priceStep = step; i.addValue = add;
            EditorUtility.SetDirty(i);
        }

        static void Weapon(string asset, string name, WeaponType t, float dmg, float interval, float range, float speed, Color c)
        {
            var w = LoadOrCreate<WeaponData>(SoDir + "/" + asset + ".asset");
            w.displayName = name; w.type = t; w.damage = dmg; w.attackInterval = interval;
            w.range = range; w.speed = speed; w.color = c;
            EditorUtility.SetDirty(w);
        }

        static void Enemy(string asset, EnemyType t, float hp, float spd, float cDmg, float interval, float keep, float pDmg, float pSpd, int gMin, int gMax, float scale, Color c)
        {
            var e = LoadOrCreate<EnemyData>(SoDir + "/" + asset + ".asset");
            e.type = t; e.maxHP = hp; e.moveSpeed = spd; e.contactDamage = cDmg; e.attackInterval = interval;
            e.keepDistance = keep; e.projectileDamage = pDmg; e.projectileSpeed = pSpd;
            e.goldMin = gMin; e.goldMax = gMax; e.scale = scale; e.color = c;
            EditorUtility.SetDirty(e);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void WriteCircleSprite(string path)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float c = (S - 1) / 2f;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(c - d + 0.5f));
                }
            tex.SetPixels(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = AssetDatabase.LoadAssetAtPath<TextureImporter>(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 32f;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            AssetDatabase.Refresh();
        }
        #endregion
    }
}