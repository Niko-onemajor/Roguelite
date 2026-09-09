# Roguelite MVP 实施计划（类土豆兄弟）

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务执行本计划。步骤用 `- [ ]` 复选框追踪进度。

**Goal:** 在空 Unity 2022.3 LTS + URP 2D 项目中，从零实现可完整打完 5 波的类《土豆兄弟》2D 肉鸽 MVP（自动攻击、三大类武器、3 种敌人、波间商店、对象池、ScriptableObject 数据驱动）。

**Architecture:** 模块化组件架构（方案 A）。数据层用 ScriptableObject 实例（运行时 `CreateInstance` 生成默认配置，Editor 工具可落盘为资产）；实体走对象池回收；波次/商店用状态机驱动，状态变更经静态事件中心 `GameEvents` 广播，UI 订阅刷新。游戏对象由 `GameBootstrap` 运行时用代码构建（不依赖手写 .prefab/.unity），Editor 菜单仅负责一键生成场景与落盘配置。

**Tech Stack:** Unity 2022.3.62f3c1、C#（asmdef：Runtime / Editor / Tests）、URP 2D、UGUI（legacy `UnityEngine.UI.Text`，内置字体，避免 TMP 资源依赖）、Unity Test Framework（EditMode 单测）、对象池、静态事件中心、Coroutine 状态机。

**约定：**
- 运行时类统一 `namespace Roguelite`，位于 `Assets/_Project/Scripts/`
- 纯逻辑全部抽成 static/可序列化方法，供 EditMode 单测
- 每次 Commit 后必须 `git push origin main`；提交信息用 conventional commits，标题须概括该任务全部改动
- Unity Editor 路径：`C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe`（下文用 `$UNITY`）
- 验证日志输出 `E:\Projects\Roguelite\Logs\`（已 gitignore）。首次运行 Unity CLI 会全量 import，耗时数分钟属正常

**常用命令：**
```powershell
$UNITY = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
# 编译检查：
& $UNITY -batchmode -nographics -quit -projectPath "E:\Projects\Roguelite" -logFile "E:\Projects\Roguelite\Logs\compile.log"
# 期望：exit code 0，compile.log 无 "error CS"
# EditMode 测试：
& $UNITY -batchmode -nographics -projectPath "E:\Projects\Roguelite" -runTests -testPlatform EditMode -testResults "E:\Projects\Roguelite\Logs\editmode.xml" -logFile "E:\Projects\Roguelite\Logs\editmode.log"
# 期望：editmode.xml 根节点 test-run result="Passed" failed="0"
```
> 实测（2022.3.62f3c1）：`-runTests` **不要加 `-quit`**，否则测试不会执行、编辑器直接退出且不生成结果文件；去掉 `-quit` 后测试跑完与退出码零自动退出。测试程序集 asmdef 必须带 `UNITY_INCLUDE_TESTS` 约束并引用 `UnityEngine.TestRunner`/`UnityEditor.TestRunner`。

---

## 文件结构总览

```
Assets/_Project/
├── Roguelite.Runtime.asmdef
├── Scripts/
│   ├── Core/        GameEvents.cs, DamageUtilities.cs
│   ├── Data/        WeaponData.cs, EnemyData.cs, WaveConfig.cs, ShopItemData.cs, GameConfig.cs
│   ├── Entities/    PlayerStats.cs, PlayerController.cs, Enemy.cs, EnemyChaser.cs,
│   │                EnemyRanged.cs, EnemyTank.cs, Projectile.cs, Pickup.cs
│   ├── Systems/     CombatSystem.cs, PlayerWeapon.cs, RangedWeapon.cs, MeleeWeapon.cs,
│   │                AoEWeapon.cs, DamageSystem.cs, EnemySpawner.cs, ShopSystem.cs
│   ├── Managers/    WaveManager.cs
│   ├── UI/          HudView.cs, ShopView.cs, GameOverView.cs
│   ├── Utils/       PlaceholderArt.cs, PoolManager.cs, Poolable.cs, EnemyFactory.cs,
│   │                ProjectileFactory.cs, PickupFactory.cs, EnemyRegistry.cs, UIBuilder.cs
│   ├── Editor/      Roguelite.Editor.asmdef, RogueliteMenu.cs
│   └── GameBootstrap.cs
├── Scenes/          Main.unity（Editor 菜单生成，可选；默认任意场景可玩）
├── Prefabs/         预留（首版代码构建）
├── ScriptableObjects/ 配置落盘区（Editor 菜单生成，可选）
└── Tests/           Roguelite.Tests.asmdef + EditMode/ 测试
```

**任务依赖顺序**：1(骨架) → 2(事件/纯逻辑) → 3(数据SO) → 4(玩家) → 5(注册表/子弹/拾取) → 6(敌人) → 7(池/工厂) → 8(伤害/武器/索敌) → 9(刷怪/波次) → 10(商店) → 11(UI) → 12(装配) → 13(端到端验收)。

---

## Task 1: 项目骨架（目录 + asmdef + 冒烟测试）

**Files:**
- Create: `Assets/_Project/Roguelite.Runtime.asmdef`
- Create: `Assets/_Project/Scripts/Editor/Roguelite.Editor.asmdef`
- Create: `Assets/_Project/Tests/Roguelite.Tests.asmdef`
- Test: `Assets/_Project/Tests/EditMode/SmokeTests.cs`

- [x] **Step 1: 创建三个 asmdef**

`Assets/_Project/Roguelite.Runtime.asmdef`:
```json
{
    "name": "Roguelite.Runtime",
    "rootNamespace": "Roguelite",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_Project/Scripts/Editor/Roguelite.Editor.asmdef`:
```json
{
    "name": "Roguelite.Editor",
    "rootNamespace": "Roguelite.EditorTools",
    "references": ["Roguelite.Runtime"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_Project/Tests/Roguelite.Tests.asmdef`:
```json
{
    "name": "Roguelite.Tests",
    "rootNamespace": "Roguelite.Tests",
    "references": [
        "Roguelite.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```
> 注：Unity 2022.3 已废弃旧格式 `optionalUnityReferences: ["TestAssemblies"]`（CLI 运行测试时不识别测试程序集）；改用引用 `UnityEngine.TestRunner`/`UnityEditor.TestRunner` + `defineConstraints: UNITY_INCLUDE_TESTS` 的现代格式。

- [x] **Step 2: 写入冒烟测试**

`Assets/_Project/Tests/EditMode/SmokeTests.cs`:
```csharp
using NUnit.Framework;

namespace Roguelite.Tests
{
    public class SmokeTests
    {
        [Test]
        public void Framework_Is_Loaded()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
        }
    }
}
```
> 注：为遵守 AGENTS.md「禁止提交包含编译错误的代码」，Task 1 的冒烟测试采用自包含断言（不引用 Task 2/4 才实现的类型），保证本提交编译通过；Task 2 完成后将其升级为 `Assert.That(typeof(GameEvents), Is.Not.Null)`，Task 4 完成后改为 `Assert.That(typeof(PlayerStats), Is.Not.Null)`。

- [x] **Step 3: 编译检查（asmdef 语法校验）**

Run: 编译命令（见「常用命令」）。
Expected: 无 asmdef 语法报错；`SmokeTests` 的编译失败属已知中间态（Task 2/4 补齐后消失）。
> 实测结果：编译通过（`error CS` 0 个，`Exiting batchmode successfully now!`）；EditMode 冒烟测试运行通过（`result="Passed" failed="0"`）。

- [x] **Step 4: Commit**

```bash
git add Assets/_Project
git commit -m "build: 搭建项目骨架(asmdef 三件套 + 冒烟测试)"
git push origin main
```

---

## Task 2: 事件中心 + 纯逻辑伤害工具（TDD）

**Files:**
- Create: `Assets/_Project/Scripts/Core/DamageUtilities.cs`
- Create: `Assets/_Project/Scripts/Core/GameEvents.cs`（不含 ShopOffer；见下方注记）
- Test: `Assets/_Project/Tests/EditMode/DamageUtilitiesTests.cs`
- Modify: `Assets/_Project/Tests/EditMode/SmokeTests.cs`（升级为引用 `GameEvents`）

- [x] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/DamageUtilitiesTests.cs`:
```csharp
using System;
using NUnit.Framework;
using Roguelite;

namespace Roguelite.Tests
{
    public class DamageUtilitiesTests
    {
        [Test]
        public void RollCrit_Chance1_AlwaysTrue_Chance0_AlwaysFalse()
        {
            var rng = new Random(123);
            Assert.That(DamageUtilities.RollCrit(1f, rng), Is.True);
            Assert.That(DamageUtilities.RollCrit(0f, rng), Is.False);
        }

        [Test]
        public void RollCrit_Deterministic_WithSameSeed()
        {
            var a = new Random(42);
            var b = new Random(42);
            for (int i = 0; i < 20; i++)
                Assert.That(DamageUtilities.RollCrit(0.5f, a), Is.EqualTo(DamageUtilities.RollCrit(0.5f, b)));
        }

        [Test]
        public void ComputeCrit_Multiplies_Only_When_Crit()
        {
            Assert.That(DamageUtilities.ComputeCrit(10f, true, 2f), Is.EqualTo(20f));
            Assert.That(DamageUtilities.ComputeCrit(10f, false, 2f), Is.EqualTo(10f));
        }
    }
}
```

- [x] **Step 2: 运行确认失败**

Run: EditMode 测试命令。
Expected: `DamageUtilities` 未定义 → 编译失败。
> 实测结果：`error CS0103: The name 'DamageUtilities' does not exist`（编译失败，符合预期红）。

- [x] **Step 3: 实现**

`Assets/_Project/Scripts/Core/DamageUtilities.cs`:
```csharp
using System;

namespace Roguelite
{
    /// <summary>纯逻辑伤害计算，便于单测。</summary>
    public static class DamageUtilities
    {
        public static bool RollCrit(float critChance, Random rng) =>
            rng != null && rng.NextDouble() < critChance;

        public static float ComputeCrit(float damage, bool isCrit, float critMultiplier) =>
            isCrit ? damage * critMultiplier : damage;
    }
}
```

`Assets/_Project/Scripts/Core/GameEvents.cs`:
```csharp
using System;

namespace Roguelite
{
    /// <summary>全局事件中心。状态变更广播，UI/系统订阅刷新。</summary>
    public static class GameEvents
    {
        public static event Action<float, float> HPChanged;     // current, max
        public static event Action<int> GoldChanged;            // current
        public static event Action<int, int> WaveChanged;       // currentIndex(1-based), total
        public static event Action<bool, int, int> GameEnded;   // victory, wavesCleared, kills

        public static void RaiseHP(float cur, float max) => HPChanged?.Invoke(cur, max);
        public static void RaiseGold(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseWave(int index, int total) => WaveChanged?.Invoke(index, total);
        public static void RaiseGameEnded(bool victory, int wavesCleared, int kills) =>
            GameEnded?.Invoke(victory, wavesCleared, kills);

        /// <summary>域重载/退出场景时清空，防止跨场景残留。</summary>
        public static void ClearAll()
        {
            HPChanged = null; GoldChanged = null; WaveChanged = null;
            GameEnded = null;
        }
    }
}
```
> 注（遵守 AGENTS.md「禁止提交编译错误代码」）：`ShopOffer` / `ShopOpened` 引用 Task 3 才创建的 `ShopItemData`，故 Task 2 不实现它们；**Task 3 创建 `ShopItemData.cs` 时必须补上**：在 `GameEvents` 中加入 `public static event Action<ShopOffer> ShopOpened;`、`public static void RaiseShop(ShopOffer offer) => ShopOpened?.Invoke(offer);`，并在 `ClearAll()` 中清零 `ShopOpened`。

- [x] **Step 4: 运行确认通过**

Run: EditMode 测试命令。
Expected: 全绿（`SmokeTests` + `DamageUtilitiesTests` = 4 例）。
> 实测结果：`result="Passed" total=4 passed=4 failed=0`。注意 CLI 返回码 1 不代表失败，以 `editmode.xml` 的 `result` 属性为准。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Core Assets/_Project/Tests/EditMode/DamageUtilitiesTests.cs
git commit -m "feat: 事件中心与纯逻辑伤害工具(含单测)"
git push origin main
```

---

## Task 3: 数据层 ScriptableObject + 运行时默认配置

**Files:**
- Create: `Assets/_Project/Scripts/Data/WeaponData.cs`
- Create: `Assets/_Project/Scripts/Data/EnemyData.cs`
- Create: `Assets/_Project/Scripts/Data/WaveConfig.cs`
- Create: `Assets/_Project/Scripts/Data/ShopItemData.cs`
- Create: `Assets/_Project/Scripts/Data/ShopOffer.cs`（独立文件，原计划置于 GameEvents.cs）
- Create: `Assets/_Project/Scripts/Data/GameConfig.cs`
- Modify: `Assets/_Project/Scripts/Core/GameEvents.cs`（补 `ShopOpened` 事件，见 Task 2 注记）

- [x] **Step 1: 实现四个 SO 与默认配置（纯数据落地，验收=编译通过）**

`Assets/_Project/Scripts/Data/WeaponData.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    public enum WeaponType { Ranged, Melee, AoE }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Roguelite/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "武器";
        public WeaponType type = WeaponType.Ranged;
        public float damage = 10f;          // 单次命中伤害
        public float attackInterval = 0.8f; // 两次开火间隔(秒)
        public float range = 6f;            // 远程=射程 / 近战=挥砍半径 / 范围=爆炸半径
        public float speed = 10f;           // 远程弹速
        public float halfAngle = 100f;      // 近战扇区半角(度)
        public Color color = Color.yellow;  // 弹丸占位色
    }
}
```

`Assets/_Project/Scripts/Data/EnemyData.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    public enum EnemyType { Chaser, Ranged, Tank }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Roguelite/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public EnemyType type = EnemyType.Chaser;
        public float maxHP = 20f;
        public float moveSpeed = 2.6f;
        public float contactDamage = 10f;
        public float attackInterval = 1.5f;   // 接触伤害/远程射击间隔
        public float keepDistance = 0f;       // 远程型保持距离
        public float projectileDamage = 0f;   // 远程型弹幕伤害
        public float projectileSpeed = 8f;
        public int goldMin = 1;
        public int goldMax = 2;
        public float scale = 1f;
        public Color color = Color.red;
    }
}
```

`Assets/_Project/Scripts/Data/WaveConfig.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    [Serializable]
    public class WaveBatch
    {
        public EnemyData enemy;
        public int count = 1;
        public float spawnInterval = 1f; // 本批每两只间的刷新间隔
    }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Roguelite/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public float prepareTime = 2f;
        public List<WaveBatch> wave1 = new List<WaveBatch>();
        public List<WaveBatch> wave2 = new List<WaveBatch>();
        public List<WaveBatch> wave3 = new List<WaveBatch>();
        public List<WaveBatch> wave4 = new List<WaveBatch>();
        public List<WaveBatch> wave5 = new List<WaveBatch>();

        public List<List<WaveBatch>> Waves()
        {
            var list = new List<List<WaveBatch>> { wave1, wave2, wave3, wave4, wave5 };
            list.RemoveAll(w => w.Count == 0);
            return list;
        }
    }
}
```

`Assets/_Project/Scripts/Data/ShopItemData.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    public enum StatType { Damage, AttackSpeed, MaxHP, MoveSpeed, Range, CritChance }

    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Roguelite/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string displayName = "增益";
        public StatType statType;
        public int basePrice = 10;
        public int priceStep = 5;   // 每购买一次价格递增
        public float addValue = 1f; // 每次加成(AttackSpeed 为乘积系数 0.85)
        public Color color = Color.white;
    }
}
```

`Assets/_Project/Scripts/Data/GameConfig.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>运行时默认配置(CreateInstance，不依赖 .asset；可被 Editor 落盘替换)。</summary>
    public class GameConfig
    {
        public WaveConfig waves;
        public List<WeaponData> weapons = new List<WeaponData>();
        public List<EnemyData> enemies = new List<EnemyData>();
        public List<ShopItemData> shopItems = new List<ShopItemData>();

        public static GameConfig Default()
        {
            var cfg = new GameConfig();

            // 武器 4 把
            cfg.weapons.Add(Weapon("手枪", WeaponType.Ranged, 10f, 0.6f, 8f, 10f, Color.yellow));
            cfg.weapons.Add(Weapon("冲锋枪", WeaponType.Ranged, 5f, 0.18f, 7f, 12f, Color.cyan));
            cfg.weapons.Add(Weapon("砍刀", WeaponType.Melee, 14f, 0.8f, 1.6f, 0f, Color.green));
            cfg.weapons.Add(Weapon("手雷", WeaponType.AoE, 8f, 2.0f, 3.5f, 0f, Color.red));

            // 敌人 3 种
            cfg.enemies.Add(Enemy(EnemyType.Chaser, 20f, 2.6f, 10f, 1.5f, 0f, 0f, 8f, 1, 2, 1f, new Color(0.9f, 0.3f, 0.3f)));
            cfg.enemies.Add(Enemy(EnemyType.Ranged, 15f, 1.8f, 0f, 2.5f, 4.5f, 8f, 8f, 1, 2, 1f, new Color(0.7f, 0.4f, 0.9f)));
            cfg.enemies.Add(Enemy(EnemyType.Tank, 60f, 1.2f, 20f, 1.5f, 0f, 0f, 8f, 3, 5, 1.6f, new Color(0.5f, 0.5f, 0.6f)));

            // 商店 6 项
            cfg.shopItems.Add(Shop("伤害+6", StatType.Damage, 15, 8, 6f));
            cfg.shopItems.Add(Shop("攻速×0.85", StatType.AttackSpeed, 15, 8, 0.85f));
            cfg.shopItems.Add(Shop("生命+20", StatType.MaxHP, 10, 5, 20f));
            cfg.shopItems.Add(Shop("移速+0.5", StatType.MoveSpeed, 10, 5, 0.5f));
            cfg.shopItems.Add(Shop("射程+1", StatType.Range, 10, 5, 1f));
            cfg.shopItems.Add(Shop("暴击+8%", StatType.CritChance, 20, 10, 0.08f));

            // 波次 5 波
            cfg.waves = ScriptableObject.CreateInstance<WaveConfig>();
            cfg.waves.prepareTime = 2f;
            cfg.waves.wave1 = new List<WaveBatch> { B(cfg.enemies[0], 6, 0.7f) };
            cfg.waves.wave2 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.6f), B(cfg.enemies[1], 3, 1.2f) };
            cfg.waves.wave3 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.5f), B(cfg.enemies[1], 4, 1.0f), B(cfg.enemies[2], 2, 2.0f) };
            cfg.waves.wave4 = new List<WaveBatch> { B(cfg.enemies[0], 10, 0.45f), B(cfg.enemies[1], 6, 0.9f), B(cfg.enemies[2], 3, 1.8f) };
            cfg.waves.wave5 = new List<WaveBatch> { B(cfg.enemies[0], 12, 0.4f), B(cfg.enemies[1], 8, 0.8f), B(cfg.enemies[2], 5, 1.5f) };
            return cfg;
        }

        static WeaponData Weapon(string name, WeaponType t, float dmg, float interval, float range, float speed, Color c)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.displayName = name; w.type = t; w.damage = dmg; w.attackInterval = interval;
            w.range = range; w.speed = speed; w.color = c;
            return w;
        }

        static EnemyData Enemy(EnemyType t, float hp, float spd, float cDmg, float interval, float keep, float pDmg, float pSpd, int gMin, int gMax, float scale, Color c)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.type = t; e.maxHP = hp; e.moveSpeed = spd; e.contactDamage = cDmg; e.attackInterval = interval;
            e.keepDistance = keep; e.projectileDamage = pDmg; e.projectileSpeed = pSpd;
            e.goldMin = gMin; e.goldMax = gMax; e.scale = scale; e.color = c;
            return e;
        }

        static ShopItemData Shop(string name, StatType s, int baseP, int step, float add)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.statType = s; i.basePrice = baseP; i.priceStep = step; i.addValue = add;
            return i;
        }

        static WaveBatch B(EnemyData e, int count, float interval) =>
            new WaveBatch { enemy = e, count = count, spawnInterval = interval };
    }
}
```

- [x] **Step 2: 编译检查**

Run: 编译命令。
Expected: 无 `error CS`。
> 实测结果：`error CS` 0 个，`Exiting batchmode successfully now!`（注意：批处理会重写 `ProjectSettings.asset` 的 bundle id，提交前请还原该文件）。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Data
git commit -m "feat: 数据层 ScriptableObject 与运行时默认配置(4武器/3敌人/6增益/5波)"
git push origin main
```

---

## Task 4: 玩家（PlayerStats + PlayerController）（TDD）

**Files:**
- Create: `Assets/_Project/Scripts/Entities/PlayerStats.cs`
- Create: `Assets/_Project/Scripts/Entities/PlayerController.cs`
- Test: `Assets/_Project/Tests/EditMode/PlayerStatsTests.cs`

- [ ] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/PlayerStatsTests.cs`:
```csharp
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class PlayerStatsTests
    {
        PlayerStats stats;
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("PlayerStats");
            stats = go.AddComponent<PlayerStats>();
            stats.ResetForRun();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
        }

        [Test]
        public void TakeDamage_Clamps_At_Zero_And_Raises()
        {
            bool raised = false;
            GameEvents.HPChanged += (c, m) => raised = true;
            stats.TakeDamage(9999f);
            Assert.That(stats.CurrentHP, Is.EqualTo(0f));
            Assert.That(raised, Is.True);
            GameEvents.HPChanged = null;
        }

        [Test]
        public void ApplyBonus_MaxHP_Increases_Max_And_Heals_Current()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.MaxHP;
            item.addValue = 20f;
            float before = stats.CurrentHP;
            stats.ApplyBonus(item);
            Assert.That(stats.maxHP, Is.EqualTo(120f));
            Assert.That(stats.CurrentHP, Is.EqualTo(before + 20f));
        }

        [Test]
        public void ApplyBonus_AttackSpeed_Multiplies_Interval()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.AttackSpeed;
            item.addValue = 0.85f;
            float before = stats.attackInterval;
            stats.ApplyBonus(item);
            Assert.That(stats.attackInterval, Is.EqualTo(before * 0.85f).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Expected: `PlayerStats` 未定义 → 编译失败。

- [ ] **Step 3: 实现**

`Assets/_Project/Scripts/Entities/PlayerStats.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>玩家属性与资源(HP/金币/击杀)，供商店增益与系统读取。</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; set; }

        public float maxHP = 100f;
        public float damage = 10f;
        public float attackInterval = 0.8f;
        public float range = 6f;
        public float moveSpeed = 3.5f;
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        public float pickupRadius = 2.5f;

        public float CurrentHP { get; private set; }
        public int Gold { get; private set; }
        public int Kills { get; private set; }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetForRun()
        {
            CurrentHP = maxHP;
            Gold = 0;
            Kills = 0;
            GameEvents.RaiseHP(CurrentHP, maxHP);
            GameEvents.RaiseGold(0);
        }

        public void TakeDamage(float dmg)
        {
            CurrentHP = Mathf.Max(0f, CurrentHP - dmg);
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GameEvents.RaiseGold(Gold);
        }

        public void NotifyKill() => Kills++;

        public void ApplyBonus(ShopItemData item)
        {
            switch (item.statType)
            {
                case StatType.Damage: damage += item.addValue; break;
                case StatType.AttackSpeed: attackInterval *= item.addValue; break;
                case StatType.MaxHP:
                    maxHP += item.addValue;
                    CurrentHP += item.addValue;
                    break;
                case StatType.MoveSpeed: moveSpeed += item.addValue; break;
                case StatType.Range: range += item.addValue; break;
                case StatType.CritChance: critChance += item.addValue; break;
                default: return;
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }
    }
}
```

`Assets/_Project/Scripts/Entities/PlayerController.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>WASD 移动；inputEnabled=false 表示商店/结算暂停态。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public PlayerStats Stats { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.right;
        [HideInInspector] public bool inputEnabled = true;

        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Stats = GetComponent<PlayerStats>();
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void FixedUpdate()
        {
            if (!inputEnabled) return;
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (move.sqrMagnitude > 1f) move = Vector2.ClampMagnitude(move, 1f);
            rb.MovePosition(rb.position + move * (Stats != null ? Stats.moveSpeed : 1f) * Time.fixedDeltaTime);
            if (move.sqrMagnitude > 0.0001f) Facing = move.normalized;
        }
    }
}
```

- [ ] **Step 4: 运行确认通过**

Expected: EditMode 全绿（`SmokeTests` 同步转绿，`DamageUtilitiesTests`/`PlayerStatsTests` 通过）。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Entities/PlayerStats.cs Assets/_Project/Scripts/Entities/PlayerController.cs Assets/_Project/Tests/EditMode/PlayerStatsTests.cs
git commit -m "feat: 玩家移动与属性(HP/金币/商店增益，含单测)"
git push origin main
```

---

## Task 5: 敌人注册表 + 子弹 + 金币拾取（TDD：注册表）

**Files:**
- Create: `Assets/_Project/Scripts/Utils/EnemyRegistry.cs`
- Create: `Assets/_Project/Scripts/Entities/Projectile.cs`
- Create: `Assets/_Project/Scripts/Entities/Pickup.cs`
- Create: `Assets/_Project/Scripts/Systems/DamageSystem.cs`（临时桩，Task 8 替换）
- Create: `Assets/_Project/Scripts/Utils/PoolManager.cs`（临时桩，Task 7 替换）
- Test: `Assets/_Project/Tests/EditMode/EnemyRegistryTests.cs`

- [ ] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/EnemyRegistryTests.cs`:
```csharp
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class EnemyRegistryTests
    {
        class FakeEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        [SetUp] public void SetUp() => EnemyRegistry.Clear();
        [TearDown] public void TearDown() => EnemyRegistry.Clear();

        [Test]
        public void Register_Unregister_Updates_AliveCount()
        {
            var go1 = new GameObject("e1");
            var go2 = new GameObject("e2");
            var e1 = go1.AddComponent<FakeEnemy>();
            var e2 = go2.AddComponent<FakeEnemy>();
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(2));

            EnemyRegistry.Unregister(e1);
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(1));
            EnemyRegistry.Unregister(e2);
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(0));
            Object.DestroyImmediate(go1);
            Object.DestroyImmediate(go2);
        }

        [Test]
        public void Nearest_Returns_Closest_Within_Range()
        {
            var near = new GameObject("near").AddComponent<FakeEnemy>();
            var far = new GameObject("far").AddComponent<FakeEnemy>();
            near.transform.position = new Vector3(1, 0, 0);
            far.transform.position = new Vector3(5, 0, 0);

            Assert.That(EnemyRegistry.Nearest(Vector2.zero, 2f), Is.SameAs(near));
            Assert.That(EnemyRegistry.Nearest(Vector2.zero, 0.5f), Is.Null);

            Object.DestroyImmediate(near.gameObject);
            Object.DestroyImmediate(far.gameObject);
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Expected: `Enemy`/`EnemyRegistry` 未定义 → 编译失败（`FakeEnemy` 派生自 `Enemy`，暗示 Task 5 必须同步提供 `Enemy` 的测试侧可变性 —— 见 Step 3 说明）。

> **依赖说明**：`FakeEnemy : Enemy` 要求 `Enemy` 基类在本任务存在。为遵守「一步一提交」，在 Task 5 你先落地 `Enemy` 基类（Task 6 会把它扩展为完整版：本任务只写注册/注销最小基类，Task 6 覆盖 Init/伤害/死亡）。合并实现以避免跨任务编译中断：

`Assets/_Project/Scripts/Entities/Enemy.cs`（Task 5 版 —— Task 6 将替换为完整版）:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>敌人基类（Task 5 最小版：仅注册/注销约束；Task 6 补全行为）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Enemy : MonoBehaviour
    {
        public EnemyData Data => null;

        void OnEnable() => EnemyRegistry.Register(this);
        void OnDisable() => EnemyRegistry.Unregister(this);

        protected abstract void Behavior(float dt, PlayerController player);
    }
}
```

- [ ] **Step 3: 实现**

`Assets/_Project/Scripts/Utils/EnemyRegistry.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>场上存活敌人注册表，供自动索敌使用。</summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> enemies = new List<Enemy>();

        public static IReadOnlyList<Enemy> All => enemies;
        public static int AliveCount => enemies.Count;

        public static void Register(Enemy e)
        {
            if (!enemies.Contains(e)) enemies.Add(e);
        }

        public static void Unregister(Enemy e) => enemies.Remove(e);

        public static Enemy Nearest(Vector2 pos, float maxDistance)
        {
            Enemy best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.Data == null) continue;
                float sq = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        public static void Clear() => enemies.Clear();
    }
}
```

> **注意**：`Nearest` 里 `e.Data == null`（最小版 Enemy.Data 恒为 null）会导致 Nearest 永远返回 null。Task 6 完善 `Data` 后逻辑自然生效。**本任务测试不覆盖 Nearest 命中场景，仅测注册表计数**（符合上面写好的测试）。

`Assets/_Project/Scripts/Entities/Projectile.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>子弹。team=0 打敌人，team=1 打玩家。</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        public int Team { get; private set; }

        float damage;
        float critChance;
        float life;
        Rigidbody2D rb;
        SpriteRenderer sr;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void Shoot(Vector2 dir, float speed, float dmg, int team, float maxRange, float critChanceValue, Color color)
        {
            Team = team;
            damage = dmg;
            critChance = critChanceValue;
            life = maxRange / Mathf.Max(0.01f, speed);
            rb.velocity = dir * speed;
            if (sr != null) sr.color = color;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) PoolManager.Return(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Team == 0)
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.Data != null)
                {
                    bool crit = DamageUtilities.RollCrit(critChance, DamageSystem.Rng);
                    enemy.TakeDamage(DamageUtilities.ComputeCrit(damage, crit, 2f), crit);
                    PoolManager.Return(gameObject);
                }
            }
            else if (other.GetComponentInParent<PlayerController>() != null)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(damage);
                PoolManager.Return(gameObject);
            }
        }
    }
}
```

`Assets/_Project/Scripts/Entities/Pickup.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>金币拾取物：触发即收，带磁吸。</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public int Gold { get; private set; }

        Rigidbody2D rb;
        SpriteRenderer sr;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void SetGold(int gold, Color color)
        {
            Gold = gold;
            if (sr != null) sr.color = color;
            if (rb != null) rb.velocity = Random.insideUnitCircle * 2.5f;
        }

        void Update()
        {
            PlayerController p = PlayerController.Instance;
            if (p == null) return;
            Vector2 delta = (Vector2)p.transform.position - (Vector2)transform.position;
            float radius = p.Stats != null ? p.Stats.pickupRadius : 2.5f;
            if (delta.sqrMagnitude <= radius * radius)
                transform.position += (Vector3)(delta.normalized * 8f * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.AddGold(Gold);
                PoolManager.Return(gameObject);
            }
        }
    }
}
```

`Assets/_Project/Scripts/Systems/DamageSystem.cs`（**临时桩**，Task 8 用完整版原样替换本文件）:
```csharp
using System;
using UnityEngine;

namespace Roguelite
{
    public static partial class DamageSystem
    {
        public static readonly Random Rng = new Random();

        /// <summary>桩：Task 8 完整实现。</summary>
        public static void HitEnemy(Enemy enemy, float damage, float critChance) { }
    }
}
```

`Assets/_Project/Scripts/Utils/PoolManager.cs`（**临时桩**，Task 7 用完整版原样替换本文件）:
```csharp
using UnityEngine;

namespace Roguelite
{
    public static partial class PoolManager
    {
        /// <summary>桩：Task 7 完整实现。</summary>
        public static GameObject Spawn(GameObject prototype, Vector3 pos, Quaternion rot) => null;

        /// <summary>桩：Task 7 完整实现。</summary>
        public static void Return(GameObject go) => UnityEngine.Object.Destroy(go);
    }
}
```

> Task 5 版 `Enemy.cs` 里 `TakeDamage`/`Init` 缺失，`Projectile` 引用了 `enemy.TakeDamage(...)` 会编译失败。→ **处理**：Task 5 暂不引用 Projectile/Pickup 的编译路径不可行。简化决策：**Task 5 提交时把下方 Task 6 的完整 Enemy 一并落地**（合并为一次提交「feat: 敌人基类+注册表+子弹+拾取」），TDD 测试只覆盖注册表。Task 6 因此只新增三个行为子类。若你希望严格分 Task 提交，也可把 Projectile/Pickup 的文件留到 Task 6 后再建 —— 但本计划默认**Task 5 包含完整 Enemy 基类**（见下方 Task 6 的 Enemy.cs 全文，Task 5 直接用它）。

- [ ] **Step 4: 运行确认通过**

Expected: EditMode 全绿（DamageUtilities/PlayerStats/注册表三项通过）。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Entities/Enemy.cs Assets/_Project/Scripts/Utils/EnemyRegistry.cs Assets/_Project/Scripts/Entities/Projectile.cs Assets/_Project/Scripts/Entities/Pickup.cs Assets/_Project/Scripts/Systems/DamageSystem.cs Assets/_Project/Scripts/Utils/PoolManager.cs Assets/_Project/Tests/EditMode/EnemyRegistryTests.cs
git commit -m "feat: 敌人基类/注册表/子弹/金币拾取(含单测) + 伤害与池建桩"
git push origin main
```

---

## Task 6: 敌人三种行为（替换/完善 Enemy 基类）

**Files:**
- Create: `Assets/_Project/Scripts/Entities/EnemyChaser.cs`
- Create: `Assets/_Project/Scripts/Entities/EnemyRanged.cs`
- Create: `Assets/_Project/Scripts/Entities/EnemyTank.cs`
- Modify: `Assets/_Project/Scripts/Entities/Enemy.cs`（覆盖为完整版）

- [ ] **Step 1: 完整 Enemy 基类（覆盖 Task 5 文件）**

`Assets/_Project/Scripts/Entities/Enemy.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>敌人抽象基类：注册/血量/接触伤害/死亡掉落回收。</summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Enemy : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public float Health { get; private set; }

        protected float attackTimer;
        SpriteRenderer sr;

        public void Init(EnemyData data)
        {
            Data = data;
            Health = data.maxHP;
            attackTimer = data.attackInterval;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = data.color;
                transform.localScale = Vector3.one * data.scale;
            }
        }

        void OnEnable() => EnemyRegistry.Register(this);
        void OnDisable() => EnemyRegistry.Unregister(this);

        void LateUpdate()
        {
            if (Data != null) EnemyUpdate(Time.deltaTime);
        }

        protected virtual void EnemyUpdate(float dt) => Behavior(dt, PlayerController.Instance);
        protected abstract void Behavior(float dt, PlayerController player);

        /// <summary>finalDamage 为最终伤害(暴击已算好)。</summary>
        public void TakeDamage(float finalDamage, bool wasCrit)
        {
            if (Data == null) return;
            Health -= finalDamage;
            if (Health <= 0f) Die();
        }

        protected virtual void Die()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.NotifyKill();
            int gold = Random.Range(Data.goldMin, Data.goldMax + 1);
            PickupFactory.Spawn(transform.position, gold);
            PoolManager.Return(gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (Data == null || Data.contactDamage <= 0f) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = Data.attackInterval;
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(Data.contactDamage);
            }
        }
    }
}
```

- [ ] **Step 2: 三种行为**

`Assets/_Project/Scripts/Entities/EnemyChaser.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>追逐型：笔直逼近玩家，接触伤害。</summary>
    public class EnemyChaser : Enemy
    {
        protected override void Behavior(float dt, PlayerController player)
        {
            if (player == null || Data == null) return;
            Vector3 dir = player.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.position += dir.normalized * (Data.moveSpeed * dt);
        }
    }
}
```

`Assets/_Project/Scripts/Entities/EnemyTank.cs`:
```csharp
namespace Roguelite
{
    /// <summary>坦克型：行为与追逐型一致，数值(血量/速/接触伤害)由数据驱动。</summary>
    public class EnemyTank : EnemyChaser { }
}
```

`Assets/_Project/Scripts/Entities/EnemyRanged.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>远程型：保持距离并发射弹幕。</summary>
    public class EnemyRanged : Enemy
    {
        protected override void Behavior(float dt, PlayerController player)
        {
            if (player == null || Data == null) return;
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;
            float keep = Mathf.Max(0.1f, Data.keepDistance);

            if (dist < keep * 0.9f) transform.position += (Vector3)(-dir * (Data.moveSpeed * dt));
            else if (dist > keep * 1.4f) transform.position += (Vector3)(dir * (Data.moveSpeed * dt));

            attackTimer -= dt;
            if (attackTimer <= 0f && dist < Data.range && dist > 0.001f)
            {
                attackTimer = Data.attackInterval;
                GameObject bullet = PoolManager.Spawn(ProjectileFactory.EnemyPrototype, transform.position, Quaternion.identity);
                bullet.GetComponent<Projectile>().Shoot(dir, Data.projectileSpeed, Data.projectileDamage, 1, Data.range, 0f, Color.magenta);
            }
        }
    }
}
```

- [ ] **Step 3: 编译检查**

Run: 编译命令。`ProjectileFactory.EnemyPrototype` 未定义（Task 7 实现）→ 允许中态编译失败；**如计划独立验证本 Task，先建临时桩**（Task 7 后移除）：
```csharp
using UnityEngine;
namespace Roguelite
{
    public static partial class ProjectileFactory
    {
        public static GameObject EnemyPrototype => null;
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Entities/Enemy*.cs
git commit -m "feat: 敌人三种行为(追逐/远程/坦克)"
git push origin main
```

---

## Task 7: 对象池 + 工厂（占位美术/原型构建）（TDD：池复用）

**Files:**
- Create: `Assets/_Project/Scripts/Utils/Poolable.cs`
- Modify: `Assets/_Project/Scripts/Utils/PoolManager.cs`（覆盖 Task 5 桩）
- Create: `Assets/_Project/Scripts/Utils/PlaceholderArt.cs`
- Create: `Assets/_Project/Scripts/Utils/EnemyFactory.cs`
- Create: `Assets/_Project/Scripts/Utils/ProjectileFactory.cs`
- Create: `Assets/_Project/Scripts/Utils/PickupFactory.cs`
- Test: `Assets/_Project/Tests/EditMode/PoolManagerTests.cs`

- [ ] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/PoolManagerTests.cs`:
```csharp
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class PoolManagerTests
    {
        [Test]
        public void Spawn_Returns_Active_And_Reuses_After_Return()
        {
            var proto = new GameObject("proto");
            proto.AddComponent<Poolable>();
            proto.SetActive(false);

            var a = PoolManager.Spawn(proto, Vector3.one, Quaternion.identity);
            Assert.That(a, Is.Not.SameAs(proto));
            Assert.That(a.activeSelf, Is.True);

            PoolManager.Return(a);
            Assert.That(a.activeSelf, Is.False);

            var b = PoolManager.Spawn(proto, Vector3.zero, Quaternion.identity);
            Assert.That(b, Is.SameAs(a), "回收后应复用同一实例");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(proto);
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Expected: `Spawn` 返回 null（桩）→ 测试失败。

- [ ] **Step 3: 实现**

`Assets/_Project/Scripts/Utils/Poolable.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>挂在池化原型上，携带原型 key；Return 时按 key 归池。</summary>
    public class Poolable : MonoBehaviour
    {
        public int Key { get; set; }
    }
}
```

`Assets/_Project/Scripts/Utils/PoolManager.cs`（覆盖 Task 5 桩）:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>静态对象池：Spawn(clone)→Return(归池)；未携带 Poolable 的直接销毁。</summary>
    public static partial class PoolManager
    {
        static readonly Dictionary<int, Queue<GameObject>> pool = new Dictionary<int, Queue<GameObject>>();

        public static GameObject Spawn(GameObject prototype, Vector3 pos, Quaternion rot)
        {
            if (prototype == null) return null;
            int key = prototype.GetInstanceID();
            if (!pool.TryGetValue(key, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pool[key] = queue;
            }

            GameObject go = queue.Count > 0 ? queue.Dequeue() : Object.Instantiate(prototype);
            var poolable = go.GetComponent<Poolable>();
            if (poolable != null) poolable.Key = key;
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public static void Return(GameObject go)
        {
            if (go == null) return;
            var poolable = go.GetComponent<Poolable>();
            if (poolable == null || poolable.Key == 0)
            {
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            if (go.TryGetComponent(out Rigidbody2D rb) && rb.bodyType == RigidbodyType2D.Dynamic)
                rb.velocity = Vector2.zero;
            if (pool.TryGetValue(poolable.Key, out Queue<GameObject> queue))
                queue.Enqueue(go);
            else
                pool[poolable.Key] = new Queue<GameObject>(new[] { go });
        }

        public static void ClearAll()
        {
            foreach (var pair in pool)
                foreach (var go in pair.Value)
                    if (go != null) Object.Destroy(go);
            pool.Clear();
        }
    }
}
```

`Assets/_Project/Scripts/Utils/PlaceholderArt.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>运行时生成白色圆形占位 Sprite（单位圆 world 半径 0.5，ppu=32，用 SpriteRenderer.color 上色）。</summary>
    public static class PlaceholderArt
    {
        const int Resolution = 32;
        const float PixelsPerUnit = 32f;

        public static Sprite Circle()
        {
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float c = (Resolution - 1) / 2f;
            var px = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
                for (int x = 0; x < Resolution; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(c - d + 0.5f);
                    px[y * Resolution + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
```

`Assets/_Project/Scripts/Utils/EnemyFactory.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>按 EnemyType 构建惰性原型(保持 inactive)，并负责从池生成敌人。</summary>
    public static class EnemyFactory
    {
        static readonly Dictionary<EnemyType, GameObject> prototypes = new Dictionary<EnemyType, GameObject>();

        public static GameObject Prototype(EnemyType type)
        {
            if (!prototypes.TryGetValue(type, out GameObject proto))
            {
                proto = Build(type);
                proto.SetActive(false);
                prototypes[type] = proto;
            }
            return proto;
        }

        static GameObject Build(EnemyType type)
        {
            GameObject go = new GameObject("Enemy_" + type,
                typeof(SpriteRenderer), typeof(Poolable), typeof(Rigidbody2D), typeof(CircleCollider2D));
            switch (type)
            {
                case EnemyType.Ranged: go.AddComponent<EnemyRanged>(); break;
                case EnemyType.Tank: go.AddComponent<EnemyTank>(); break;
                default: go.AddComponent<EnemyChaser>(); break;
            }
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 1;
            go.transform.localScale = Vector3.one * 0.8f; // 0.5*0.8=0.4 与碰撞体匹配
            return go;
        }

        public static Enemy Spawn(EnemyData data, Vector3 position)
        {
            GameObject go = PoolManager.Spawn(Prototype(data.type), position, Quaternion.identity);
            Enemy enemy = go.GetComponent<Enemy>();
            enemy.Init(data);
            return enemy;
        }
    }
}
```

`Assets/_Project/Scripts/Utils/ProjectileFactory.cs`（若建过 Task 6 临时桩则原样覆盖）:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>子弹原型(惰性构建，inactive 入池)。</summary>
    public static partial class ProjectileFactory
    {
        static GameObject friendly;
        static GameObject enemy;

        public static GameObject FriendlyPrototype => friendly ??= Build("Bullet_Friendly");
        public static GameObject EnemyPrototype => enemy ??= Build("Bullet_Enemy");

        static GameObject Build(string name)
        {
            GameObject go = new GameObject(name,
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Poolable), typeof(Projectile));
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.18f;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 2;
            go.transform.localScale = Vector3.one * 0.36f; // 0.5*0.36≈0.18
            go.SetActive(false);
            return go;
        }
    }
}
```

`Assets/_Project/Scripts/Utils/PickupFactory.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>金币拾取物工厂。</summary>
    public static class PickupFactory
    {
        static GameObject prototype;

        public static GameObject Prototype
        {
            get
            {
                if (prototype == null)
                {
                    prototype = new GameObject("Coin",
                        typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Poolable), typeof(Pickup));
                    Rigidbody2D rb = prototype.GetComponent<Rigidbody2D>();
                    rb.isKinematic = true;
                    rb.gravityScale = 0f;
                    CircleCollider2D col = prototype.GetComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.18f;
                    SpriteRenderer sr = prototype.GetComponent<SpriteRenderer>();
                    sr.sprite = PlaceholderArt.Circle();
                    sr.sortingOrder = 3;
                    prototype.transform.localScale = Vector3.one * 0.36f;
                    prototype.SetActive(false);
                }
                return prototype;
            }
        }

        public static Pickup Spawn(Vector3 position, int gold)
        {
            GameObject go = PoolManager.Spawn(Prototype, position, Quaternion.identity);
            Pickup pickup = go.GetComponent<Pickup>();
            pickup.SetGold(gold, Color.yellow);
            return pickup;
        }
    }
}
```

- [ ] **Step 4: 运行 EditMode 测试 + 编译检查**

Run: 两条命令。
Expected: 全绿；编译无 `error CS`；Task 6 的 `ProjectileFactory.EnemyPrototype` 引用此时解析，若建过临时桩记得删除。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Utils Assets/_Project/Tests/EditMode/PoolManagerTests.cs
git commit -m "feat: 对象池与工厂(占位美术/敌人/子弹/金币，含池单测)"
git push origin main
```

---

## Task 8: 伤害系统 + 自动索敌 + 三大武器（TDD：范围/扇区判定）

**Files:**
- Modify: `Assets/_Project/Scripts/Systems/DamageSystem.cs`（覆盖 Task 5 桩）
- Create: `Assets/_Project/Scripts/Systems/PlayerWeapon.cs`
- Create: `Assets/_Project/Scripts/Systems/RangedWeapon.cs`
- Create: `Assets/_Project/Scripts/Systems/MeleeWeapon.cs`
- Create: `Assets/_Project/Scripts/Systems/AoEWeapon.cs`
- Create: `Assets/_Project/Scripts/Systems/CombatSystem.cs`
- Test: `Assets/_Project/Tests/EditMode/DamageSystemTests.cs`

- [ ] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/DamageSystemTests.cs`:
```csharp
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class DamageSystemTests
    {
        class TestEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        [SetUp] public void SetUp() => EnemyRegistry.Clear();
        [TearDown] public void TearDown() => EnemyRegistry.Clear();

        Enemy MakeEnemy(Vector3 pos, float hp)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHP = hp;
            var go = new GameObject("e");
            go.transform.position = pos;
            var e = go.AddComponent<TestEnemy>();
            e.Init(data);
            return e;
        }

        [Test]
        public void RadiusHit_Damages_Only_Enemies_In_Radius()
        {
            MakeEnemy(new Vector3(2, 0, 0), 100f);
            MakeEnemy(new Vector3(5, 0, 0), 100f);

            DamageSystem.RadiusHit(Vector2.zero, 3f, 10f, 0f);

            var list = EnemyRegistry.All;
            Assert.That(list[0].Health, Is.EqualTo(90f));
            Assert.That(list[1].Health, Is.EqualTo(100f));
        }

        [Test]
        public void MeleeHit_Hits_Only_Enemies_Within_Arc()
        {
            MakeEnemy(new Vector3(0, 1, 0), 100f); // 正前方
            MakeEnemy(new Vector3(1, 0, 0), 100f); // 正侧方 90°

            DamageSystem.MeleeHit(Vector2.zero, Vector2.up, 2f, 45f, 10f, 0f);

            var list = EnemyRegistry.All;
            Assert.That(list[0].Health, Is.EqualTo(90f));
            Assert.That(list[1].Health, Is.EqualTo(100f));
        }
    }
}
```

> 注意：`EnemyRegistry.All` 顺序按注册先后。测试在 TearDown 里 `EnemyRegistry.Clear()`，但 GO 未销毁，OnDisable 不会触发（Clear 已清列表）。**补充**：请在 TearDown 中销毁相关 GO 避免泄漏（把两个 MakeEnemy 返回引用存列表再 DestroyImmediate）。为简洁，可接受本测试轻量泄漏（测试进程级），但建议执行时按下述处理：

```csharp
        readonly System.Collections.Generic.List<GameObject> objects = new System.Collections.Generic.List<GameObject>();
        // MakeEnemy 内加入 objects；TearDown 中：
        // foreach (var o in objects) Object.DestroyImmediate(o); objects.Clear();
```

- [ ] **Step 2: 运行确认失败**

Expected: `DamageSystem.RadiusHit` 未定义 → 编译失败。

- [ ] **Step 3: 实现**

`Assets/_Project/Scripts/Systems/DamageSystem.cs`（覆盖 Task 5 桩）:
```csharp
using System;
using UnityEngine;

namespace Roguelite
{
    /// <summary>战斗伤害统一入口。</summary>
    public static partial class DamageSystem
    {
        public static readonly Random Rng = new Random();

        public static void HitEnemy(Enemy enemy, float damage, float critChance)
        {
            if (enemy == null || enemy.Data == null) return;
            enemy.TakeDamage(DamageUtilities.ComputeCrit(damage, DamageUtilities.RollCrit(critChance, Rng), 2f), true);
        }

        public static void RadiusHit(Vector2 origin, float radius, float damage, float critChance)
        {
            float r2 = radius * radius;
            for (int i = EnemyRegistry.All.Count - 1; i >= 0; i--)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null) continue;
                if (((Vector2)e.transform.position - origin).sqrMagnitude <= r2)
                    HitEnemy(e, damage, critChance);
            }
        }

        public static void MeleeHit(Vector2 origin, Vector2 dir, float range, float halfAngleDeg, float damage, float critChance)
        {
            float r2 = range * range;
            float halfCos = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            for (int i = EnemyRegistry.All.Count - 1; i >= 0; i--)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null) continue;
                Vector2 to = (Vector2)e.transform.position - origin;
                if (to.sqrMagnitude > r2) continue;
                float dot = to.sqrMagnitude > 0.0001f ? Vector2.Dot(to.normalized, dir.normalized) : 1f;
                if (dot >= halfCos) HitEnemy(e, damage, critChance);
            }
        }
    }
}
```

`Assets/_Project/Scripts/Systems/PlayerWeapon.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>武器抽象基类：统一冷却，子类实现 Fire。</summary>
    public abstract class PlayerWeapon : MonoBehaviour
    {
        public WeaponData Data { get; private set; }

        float cooldown;

        public void Equip(WeaponData data)
        {
            Data = data;
            cooldown = 0f;
        }

        public void Tick(float dt, Vector2 origin, Vector2 dir, float statDamage, float statCritChance, float statRange)
        {
            if (Data == null) return;
            cooldown -= dt;
            if (cooldown > 0f) return;
            cooldown = Data.attackInterval;
            Fire(origin, dir, statDamage, statCritChance, statRange);
        }

        protected abstract void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange);
    }
}
```

`Assets/_Project/Scripts/Systems/RangedWeapon.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>远程：朝索敌方向发射弹丸。</summary>
    public class RangedWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange)
        {
            GameObject bullet = PoolManager.Spawn(ProjectileFactory.FriendlyPrototype, origin, Quaternion.identity);
            bullet.GetComponent<Projectile>().Shoot(dir, Data.speed, damage, 0, Data.range + statRange, critChance, Data.color);
        }
    }
}
```

`Assets/_Project/Scripts/Systems/MeleeWeapon.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>近战：面前扇形范围判定。</summary>
    public class MeleeWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange) =>
            DamageSystem.MeleeHit(origin, dir, Data.range, Data.halfAngle, damage, critChance);
    }
}
```

`Assets/_Project/Scripts/Systems/AoEWeapon.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>范围：以玩家为中心圆形爆炸。</summary>
    public class AoEWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange) =>
            DamageSystem.RadiusHit(origin, Data.range, damage, critChance);
    }
}
```

`Assets/_Project/Scripts/Systems/CombatSystem.cs`:
```csharp
using UnityEngine;

namespace Roguelite
{
    /// <summary>挂在玩家身上：自动索敌并驱动武器。商店/结算暂停。</summary>
    public class CombatSystem : MonoBehaviour
    {
        public PlayerStats Stats { get; private set; }

        PlayerWeapon weapon;

        void Awake() => Stats = GetComponent<PlayerStats>();

        public void Equip(WeaponData data)
        {
            if (data == null) return;
            weapon = data.type switch
            {
                WeaponType.Melee => gameObject.AddComponent<MeleeWeapon>(),
                WeaponType.AoE => gameObject.AddComponent<AoEWeapon>(),
                _ => gameObject.AddComponent<RangedWeapon>()
            };
            weapon.Equip(data);
        }

        void Update()
        {
            if (weapon == null) return;
            PlayerController pc = PlayerController.Instance;
            if (pc == null || !pc.inputEnabled) return;

            Vector2 origin = transform.position;
            float effRange = weapon.Data.range + Stats.range;
            Enemy target = EnemyRegistry.Nearest(origin, effRange);
            if (target == null) return;

            Vector2 to = (Vector2)target.transform.position - origin;
            Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
            weapon.Tick(Time.deltaTime, origin, dir, Stats.damage, Stats.critChance, Stats.range);
        }
    }
}
```

- [ ] **Step 4: 运行确认通过 + 编译检查**

Expected: EditMode 全绿（含 DamageSystem 新单测）；无 `error CS`。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Systems Assets/_Project/Tests/EditMode/DamageSystemTests.cs
git commit -m "feat: 伤害系统/自动索敌/三大类武器(含单测)"
git push origin main
```

---

## Task 9: 刷怪 + 波次状态机

**Files:**
- Create: `Assets/_Project/Scripts/Systems/EnemySpawner.cs`
- Create: `Assets/_Project/Scripts/Managers/WaveManager.cs`

- [ ] **Step 1: 实现刷怪器**

`Assets/_Project/Scripts/Systems/EnemySpawner.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>按波次计划分批刷怪；WaveComplete = 全部生成且场上无存活。</summary>
    public class EnemySpawner : MonoBehaviour
    {
        List<WaveBatch> batches;
        int batchIndex;
        WaveBatch current;
        int remainingInBatch;
        int pending;
        float spawnCd = 0.2f;

        public bool WaveComplete => pending == 0 && EnemyRegistry.AliveCount == 0;

        public void StartWave(List<WaveBatch> schedule)
        {
            batches = schedule;
            batchIndex = 0;
            current = null;
            remainingInBatch = 0;
            pending = 0;
            spawnCd = 0.2f;
            if (schedule == null) return;
            foreach (WaveBatch b in schedule) pending += b.count;
        }

        void Update()
        {
            if (pending <= 0) return;
            spawnCd -= Time.deltaTime;
            if (spawnCd > 0f) return;
            if (current == null || remainingInBatch <= 0)
            {
                if (batchIndex >= batches.Count) return;
                current = batches[batchIndex++];
                remainingInBatch = current.count;
            }
            EnemyFactory.Spawn(current.enemy, RandomOffScreenPosition());
            remainingInBatch--;
            pending--;
            spawnCd = current.spawnInterval;
        }

        static Vector2 RandomOffScreenPosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Random.insideUnitCircle * 5f;
            float halfH = cam.orthographicSize + 1.5f;
            float halfW = halfH * cam.aspect + 1.5f;
            int side = Random.Range(0, 4);
            float x = side == 0 ? -halfW : side == 1 ? halfW : Random.Range(-halfW, halfW);
            float y = side == 2 ? -halfH : side == 3 ? halfH : Random.Range(-halfH, halfH);
            return new Vector2(x, y);
        }
    }
}
```

- [ ] **Step 2: 实现波次状态机**

`Assets/_Project/Scripts/Managers/WaveManager.cs`:
```csharp
using System.Collections;
using UnityEngine;

namespace Roguelite
{
    public enum WaveState { Prepare, Combat, Interlude, Shop, GameOver }

    /// <summary>波次状态机：Prepare→Combat→Interlude→Shop→(循环)→GameOver。</summary>
    public class WaveManager : MonoBehaviour
    {
        WaveConfig config;
        EnemySpawner spawner;
        ShopSystem shop;
        PlayerStats stats;

        public WaveState State { get; private set; }
        public bool Victory { get; private set; }

        public void BeginRun(WaveConfig cfg, EnemySpawner spawn, ShopSystem shopSystem, PlayerStats playerStats)
        {
            config = cfg;
            spawner = spawn;
            shop = shopSystem;
            stats = playerStats;
            Victory = false;
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop()
        {
            System.Collections.Generic.List<System.Collections.Generic.List<WaveBatch>> waves = config.Waves();
            int waveIndex = 0;
            while (waveIndex < waves.Count)
            {
                State = WaveState.Prepare;
                SetInput(true);
                GameEvents.RaiseWave(waveIndex + 1, waves.Count);
                yield return new WaitForSeconds(config.prepareTime);
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Combat;
                spawner.StartWave(waves[waveIndex]);
                while (!spawner.WaveComplete)
                {
                    if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }
                    yield return null;
                }

                State = WaveState.Interlude;
                yield return new WaitForSeconds(1f);

                if (waveIndex == waves.Count - 1)
                {
                    EndGame(true, waveIndex + 1);
                    yield break;
                }

                State = WaveState.Shop;
                SetInput(false);
                shop.OpenOffer();
                yield return new WaitUntil(() => !shop.IsAwaitingChoice);
                SetInput(true);
                waveIndex++;
            }
            EndGame(true, waves.Count);
        }

        void EndGame(bool victory, int wavesCleared)
        {
            State = WaveState.GameOver;
            Victory = victory;
            SetInput(false);
            GameEvents.RaiseGameEnded(victory, wavesCleared, stats.Kills);
        }

        static void SetInput(bool enabled)
        {
            if (PlayerController.Instance != null)
                PlayerController.Instance.inputEnabled = enabled;
        }
    }
}
```

- [ ] **Step 3: 编译检查**

Expected: 无 `error CS`（`ShopSystem`/`OpenOffer` 未定义属已知中间态，Task 10 补齐；先建最小桩避免中断独立验证）：
```csharp
using UnityEngine;
namespace Roguelite
{
    public class ShopSystem : MonoBehaviour
    {
        public bool IsAwaitingChoice { get; private set; }
        public void OpenOffer() => IsAwaitingChoice = true;
    }
}
```
（Task 10 用完整版覆盖。）

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Systems/EnemySpawner.cs Assets/_Project/Scripts/Managers/WaveManager.cs
git commit -m "feat: 刷怪器与波次状态机(Prepare/Combat/Interlude/Shop/GameOver)"
git push origin main
```

---

## Task 10: 商店系统（TDD）

**Files:**
- Create: `Assets/_Project/Scripts/Systems/ShopSystem.cs`（覆盖 Task 9 桩）
- Test: `Assets/_Project/Tests/EditMode/ShopSystemTests.cs`

- [ ] **Step 1: 写失败测试**

`Assets/_Project/Tests/EditMode/ShopSystemTests.cs`:
```csharp
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class ShopSystemTests
    {
        PlayerStats stats;
        GameObject statsGo;
        ShopSystem shop;
        GameObject shopGo;
        ShopItemData[] pool;

        [SetUp]
        public void SetUp()
        {
            statsGo = new GameObject("stats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.AddGold(100);

            shopGo = new GameObject("shop");
            shop = shopGo.AddComponent<ShopSystem>();
            shop.stats = stats;

            pool = new ShopItemData[6];
            for (int i = 0; i < 6; i++)
            {
                var item = ScriptableObject.CreateInstance<ShopItemData>();
                item.displayName = "item" + i;
                item.basePrice = 10;
                item.priceStep = 5;
                item.addValue = 1f;
                pool[i] = item;
            }
            shop.pool = pool;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(shopGo);
            Object.DestroyImmediate(statsGo);
            foreach (var p in pool) Object.DestroyImmediate(p);
            GameEvents.ClearAll();
        }

        [Test]
        public void OpenOffer_Returns_Three_Distinct_Items()
        {
            var offer = shop.OpenOffer();
            Assert.That(offer.items.Count, Is.EqualTo(3));
            Assert.That(new System.Collections.Generic.HashSet<ShopItemData>(offer.items).Count, Is.EqualTo(3));
            Assert.That(shop.IsAwaitingChoice, Is.True);
        }

        [Test]
        public void PriceOf_Increases_After_Purchase()
        {
            int before = shop.PriceOf(pool[0]);
            shop.OpenOffer();
            var purchased = shop.TryPurchase(pool[0]);
            Assert.That(purchased, Is.True);
            Assert.That(shop.PriceOf(pool[0]), Is.EqualTo(before + 5));
        }

        [Test]
        public void TryPurchase_Fails_When_Gold_Short()
        {
            var broke = new GameObject("broke");
            var brokeStats = broke.AddComponent<PlayerStats>();
            var brokeShop = broke.AddComponent<ShopSystem>();
            brokeShop.stats = brokeStats;
            brokeShop.pool = new[] { pool[0] };
            brokeStats.AddGold(5);

            Assert.That(brokeShop.TryPurchase(pool[0]), Is.False);
            Assert.That(brokeStats.Gold, Is.EqualTo(5));
            Object.DestroyImmediate(broke);
        }

        [Test]
        public void TryPurchase_Closes_Offer()
        {
            shop.OpenOffer();
            shop.TryPurchase(pool[0]);
            Assert.That(shop.IsAwaitingChoice, Is.False);
        }
    }
}
```

> 说明：`openOffer` 测试中 `TryPurchase` 假设购买后不管成功与否都应关闭条件在 `TryPurchase_Closes_Offer` 里覆盖（成功场景）。金币不足时不关闭（可再选购别项），与本测试不冲突。

- [ ] **Step 2: 运行确认失败**

Expected: `ShopSystem.OpenOffer` 未定义 → 编译失败。

- [ ] **Step 3: 实现**

`Assets/_Project/Scripts/Systems/ShopSystem.cs`（覆盖 Task 9 桩）:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>波间商店：3 选 1，价格随购买次数递增。</summary>
    public class ShopSystem : MonoBehaviour
    {
        public ShopItemData[] pool;
        public PlayerStats stats;

        readonly Dictionary<ShopItemData, int> purchasedCount = new Dictionary<ShopItemData, int>();
        readonly System.Random rng = new System.Random();

        public bool IsAwaitingChoice { get; private set; }

        public int PriceOf(ShopItemData item)
        {
            int n = purchasedCount.TryGetValue(item, out int v) ? v : 0;
            return item.basePrice + item.priceStep * n;
        }

        public ShopOffer OpenOffer()
        {
            IsAwaitingChoice = true;
            var offer = new ShopOffer();
            var available = new List<ShopItemData>(pool);
            while (offer.items.Count < 3 && available.Count > 0)
            {
                int idx = rng.Next(available.Count);
                var item = available[idx];
                available.RemoveAt(idx);
                offer.items.Add(item);
                offer.prices.Add(PriceOf(item));
            }
            GameEvents.RaiseShop(offer);
            return offer;
        }

        public bool TryPurchase(ShopItemData item)
        {
            if (!IsAwaitingChoice || stats == null) return false;
            int price = PriceOf(item);
            if (stats.Gold < price) return false;
            stats.AddGold(-price);
            stats.ApplyBonus(item);
            purchasedCount[item] = purchasedCount.TryGetValue(item, out int n) ? n + 1 : 1;
            IsAwaitingChoice = false;
            return true;
        }
    }
}
```

> 说明：金币不足或未开店（`IsAwaitingChoice == false`）返回 false 且不关店，玩家可换选其他项，与 `TryPurchase_Fails_When_Gold_Short` 一致。

- [ ] **Step 4: 运行确认通过 + 编译检查**

Run: EditMode 测试 + 编译命令。
Expected: 全绿（`ShopSystemTests` 4 项通过）；无 `error CS`。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Systems/ShopSystem.cs Assets/_Project/Tests/EditMode/ShopSystemTests.cs
git commit -m "feat: 商店系统(3选1/价格递增/属性加成，含单测)"
git push origin main
```

---

## Task 11: UI 层（HUD / 商店 / 结算）

> 遵循计划约定：UGUI **legacy `UnityEngine.UI.Text`** + 内置字体（不用 TMP），UI 全部由 `UIBuilder` 运行时代码构建，无手写 prefab 依赖。

**Files:**
- Create: `Assets/_Project/Scripts/Utils/UIBuilder.cs`
- Create: `Assets/_Project/Scripts/UI/HudView.cs`
- Create: `Assets/_Project/Scripts/UI/ShopView.cs`
- Create: `Assets/_Project/Scripts/UI/GameOverView.cs`

- [ ] **Step 1: 实现 UIBuilder（Canvas/面板/文本/按钮/血条）**

`Assets/_Project/Scripts/Utils/UIBuilder.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>运行时构建 UGUI（legacy Text，内置字体），避免 TMP/图集资源依赖。</summary>
    public static class UIBuilder
    {
        static Font _font;

        public static Font Font()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        public static GameObject Canvas(string name = "UI_Canvas")
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            return go;
        }

        /// <summary>创建带背景色的面板节点，返回其 GameObject。</summary>
        public static GameObject Panel(string name, Transform parent, Color? color = null)
        {
            var go = Rect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color ?? new Color(0f, 0f, 0f, 0.45f);
            return go;
        }

        public static Text AddText(GameObject go, string content, int size, Color color, TextAnchor anchor)
        {
            var t = go.AddComponent<Text>();
            t.font = Font();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        public static Text Text(string name, Transform parent, string content, int size, Color color, TextAnchor anchor)
        {
            var go = Rect(name, parent);
            return AddText(go, content, size, color, anchor);
        }

        /// <summary>把 Image 设为水平填充条，用于血条。</summary>
        public static Image AddFilledBar(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.color = color;
            return img;
        }

        public static GameObject Button(string name, Transform parent, string label, Action onClick)
        {
            var go = Rect(name, parent);
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            colors.highlightedColor = new Color(0.45f, 0.65f, 1f, 1f);
            colors.pressedColor = new Color(0.2f, 0.35f, 0.7f, 1f);
            btn.colors = colors;
            AddText(go, label, 30, Color.white, TextAnchor.MiddleCenter);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return go;
        }

        static GameObject Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
```

- [ ] **Step 2: 实现 HudView**

`Assets/_Project/Scripts/UI/HudView.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>HUD：血条 / 金币 / 波次，订阅 GameEvents 自动刷新。</summary>
    public class HudView : MonoBehaviour
    {
        Image hpFill;
        Text hpText;
        Text goldText;
        Text waveText;

        public void Build(Transform parent)
        {
            var bar = UIBuilder.Panel("HP_Bar", parent);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.03f, 0.94f);
            barRt.anchorMax = new Vector2(0.42f, 0.985f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            hpFill = UIBuilder.AddFilledBar(bar, new Color(0.8f, 0.25f, 0.25f));
            hpText = UIBuilder.AddText(bar, "HP 100/100", 26, Color.white, TextAnchor.MiddleCenter);

            var goldGo = UIBuilder.Text("Gold", parent, "金币 0", 28, Color.yellow, TextAnchor.MiddleLeft);
            goldGo.rectTransform.anchorMin = new Vector2(0.03f, 0.88f);
            goldGo.rectTransform.anchorMax = new Vector2(0.42f, 0.94f);
            goldText = goldGo;

            var waveGo = UIBuilder.Text("Wave", parent, "第 1/5 波", 30, Color.white, TextAnchor.MiddleRight);
            waveGo.rectTransform.anchorMin = new Vector2(0.55f, 0.94f);
            waveGo.rectTransform.anchorMax = new Vector2(0.97f, 0.985f);
            waveText = waveGo;
        }

        #region Unity Lifecycle
        void OnEnable()
        {
            GameEvents.HPChanged += OnHPChanged;
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.WaveChanged += OnWaveChanged;
        }

        void OnDisable()
        {
            GameEvents.HPChanged -= OnHPChanged;
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
        }
        #endregion

        #region Event Handlers
        void OnHPChanged(float cur, float max)
        {
            hpFill.fillAmount = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            hpText.text = $"HP {(int)cur}/{(int)max}";
        }

        void OnGoldChanged(int gold) => goldText.text = "金币 " + gold;

        void OnWaveChanged(int index, int total) => waveText.text = $"第 {index}/{total} 波";
        #endregion
    }
}
```

- [ ] **Step 3: 实现 ShopView**

`Assets/_Project/Scripts/UI/ShopView.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>商店视图：渲染 3 张增益卡片，点击购买；无金币/未开店则不响应。</summary>
    public class ShopView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public ShopSystem shop;

        GameObject panel;
        readonly List<Card> cards = new List<Card>();

        sealed class Card
        {
            public GameObject root;
            public Text label;
            public Button button;
        }

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("Shop", parent);
            panel.SetActive(false);
            for (int i = 0; i < 3; i++) BuildCard(i);
        }

        void BuildCard(int i)
        {
            var root = UIBuilder.Button("Card_" + i, panel.transform, "", null);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f + 0.26f * i, 0.3f);
            rt.anchorMax = new Vector2(0.38f + 0.26f * i, 0.7f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            cards.Add(new Card { root = root, label = root.GetComponent<Text>(), button = root.GetComponent<Button>() });
        }

        #region Unity Lifecycle
        void OnEnable() => GameEvents.ShopOpened += OnShopOpened;

        void OnDisable() => GameEvents.ShopOpened -= OnShopOpened;
        #endregion

        #region Event Handlers
        void OnShopOpened(ShopOffer offer)
        {
            panel.SetActive(true);
            for (int i = 0; i < cards.Count; i++)
            {
                bool has = i < offer.items.Count;
                cards[i].root.SetActive(has);
                if (!has) continue;
                var item = offer.items[i];
                var price = offer.prices[i];
                cards[i].label.text = $"{item.displayName}\n{price} 金币";
                cards[i].button.onClick.RemoveAllListeners();
                int idx = i;
                cards[i].button.onClick.AddListener(() => TryBuy(cards[idx], item));
            }
        }

        void TryBuy(Card card, ShopItemData item)
        {
            if (shop == null || !shop.TryPurchase(item)) return;
            foreach (var c in cards) c.button.onClick.RemoveAllListeners();
            panel.SetActive(false);
        }
        #endregion
    }
}
```

- [ ] **Step 4: 实现 GameOverView**

`Assets/_Project/Scripts/UI/GameOverView.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>结算视图：胜/负 + 击杀/波数 + 重新开始（重载当前场景）。</summary>
    public class GameOverView : MonoBehaviour
    {
        GameObject panel;
        Text titleText;
        Text statsText;

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("GameOver", parent, new Color(0f, 0f, 0f, 0.75f));
            panel.SetActive(false);

            var titleGo = UIBuilder.Text("Title", panel.transform, "", 72, Color.white, TextAnchor.MiddleCenter);
            titleGo.rectTransform.anchorMin = new Vector2(0.2f, 0.6f);
            titleGo.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            titleGo.rectTransform.offsetMin = Vector2.zero;
            titleGo.rectTransform.offsetMax = Vector2.zero;
            titleText = titleGo;

            var statsGo = UIBuilder.Text("Stats", panel.transform, "", 38, Color.white, TextAnchor.MiddleCenter);
            statsGo.rectTransform.anchorMin = new Vector2(0.2f, 0.42f);
            statsGo.rectTransform.anchorMax = new Vector2(0.8f, 0.56f);
            statsGo.rectTransform.offsetMin = Vector2.zero;
            statsGo.rectTransform.offsetMax = Vector2.zero;
            statsText = statsGo;

            var btn = UIBuilder.Button("Restart", panel.transform, "重新开始", Restart);
            var btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.4f, 0.2f);
            btnRt.anchorMax = new Vector2(0.6f, 0.32f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
        }

        #region Unity Lifecycle
        void OnEnable() => GameEvents.GameEnded += OnGameEnded;

        void OnDisable() => GameEvents.GameEnded -= OnGameEnded;
        #endregion

        #region Event Handlers
        void OnGameEnded(bool victory, int wavesCleared, int kills)
        {
            panel.SetActive(true);
            titleText.text = victory ? "胜利！" : "失败";
            titleText.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            statsText.text = $"击杀：{kills}    波数：{wavesCleared}";
        }

        static void Restart()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
        #endregion
    }
}
```

- [ ] **Step 5: 编译检查**

Run: 编译命令。
Expected: 无 `error CS`。

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Utils/UIBuilder.cs Assets/_Project/Scripts/UI
git commit -m "feat: UI 层(HUD血条金币波次/商店3卡片/结算面板，运行时代码构建)"
git push origin main
```

---

## Task 12: 运行时装配（GameBootstrap + 多武器位 + Editor 工具）

> 架构约定：`GameBootstrap` 在任意场景挂载后即以代码构建相机/玩家/管理器/UI 并开跑；Editor 菜单 `RogueliteMenu` 用于生成 `Main.unity` 场景与落盘配置（可选，不影响运行时）。

**Files:**
- Modify: `Assets/_Project/Scripts/Systems/CombatSystem.cs`（单武器位 → 多武器位）
- Create: `Assets/_Project/Scripts/GameBootstrap.cs`
- Create: `Assets/_Project/Scripts/Editor/RogueliteMenu.cs`

- [ ] **Step 1: CombatSystem 支持多武器同时开火**

`Assets/_Project/Scripts/Systems/CombatSystem.cs`（整体替换 Task 8 版本）:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>挂在玩家身上：每把武器独立索敌并驱动冷却。商店/结算冻结输入时自动停火。</summary>
    public class CombatSystem : MonoBehaviour
    {
        public PlayerStats Stats { get; private set; }

        readonly List<PlayerWeapon> weapons = new List<PlayerWeapon>();

        void Awake() => Stats = GetComponent<PlayerStats>();

        public void ClearEquips()
        {
            foreach (var w in weapons)
                if (w != null) Destroy(w);
            weapons.Clear();
        }

        public void Equip(WeaponData data)
        {
            if (data == null) return;
            PlayerWeapon weapon = data.type switch
            {
                WeaponType.Melee => gameObject.AddComponent<MeleeWeapon>(),
                WeaponType.AoE => gameObject.AddComponent<AoEWeapon>(),
                _ => gameObject.AddComponent<RangedWeapon>()
            };
            weapon.Equip(data);
            weapons.Add(weapon);
        }

        void Update()
        {
            if (weapons.Count == 0) return;
            PlayerController pc = PlayerController.Instance;
            if (pc == null || !pc.inputEnabled) return;

            Vector2 origin = transform.position;
            for (int i = 0; i < weapons.Count; i++)
            {
                PlayerWeapon w = weapons[i];
                if (w == null || w.Data == null) continue;
                float effRange = w.Data.range + Stats.range;
                Enemy target = EnemyRegistry.Nearest(origin, effRange);
                if (target == null) continue;
                Vector2 to = (Vector2)target.transform.position - origin;
                Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
                w.Tick(Time.deltaTime, origin, dir, Stats.damage, Stats.critChance, Stats.range);
            }
        }
    }
}
```

- [ ] **Step 2: 实现 GameBootstrap（任意场景可玩）**

`Assets/_Project/Scripts/GameBootstrap.cs`:
```csharp
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Roguelite
{
    /// <summary>运行时装配入口：把 GameBootstrap 挂到任意场景的空物体上即可开玩。
    /// Awake 中以代码构建 相机/玩家/管理器/UI，随后开始第 1 波。</summary>
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureCamera();
            EnsureEventSystem();

            GameConfig cfg = GameConfig.Default();

            GameObject playerGo = EnsurePlayer();
            var stats = playerGo.GetComponent<PlayerStats>();
            var combat = playerGo.GetComponent<CombatSystem>();
            combat.ClearEquips();
            foreach (var w in cfg.weapons) combat.Equip(w);

            var spawner = gameObject.AddComponent<EnemySpawner>();
            var shop = gameObject.AddComponent<ShopSystem>();
            shop.stats = stats;
            shop.pool = cfg.shopItems.ToArray();
            var wave = gameObject.AddComponent<WaveManager>();

            BuildUI(shop);

            stats.ResetForRun();
            wave.BeginRun(cfg.waves, spawner, shop, stats);
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject EnsurePlayer()
        {
            var existing = FindFirstObjectByType<PlayerStats>(); // 仅在启动时查找一次
            if (existing != null) return existing.gameObject;

            var go = new GameObject("Player");
            go.transform.position = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.color = new Color(0.3f, 0.85f, 0.6f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<CombatSystem>();
            return go;
        }

        void BuildUI(ShopSystem shop)
        {
            var canvas = UIBuilder.Canvas();
            var hud = canvas.AddComponent<HudView>();
            hud.Build(canvas.transform);
            var shopView = canvas.AddComponent<ShopView>();
            shopView.shop = shop;
            shopView.Build(canvas.transform);
            var over = canvas.AddComponent<GameOverView>();
            over.Build(canvas.transform);
        }
    }
}
```

- [ ] **Step 3: 实现 Editor 工具（生成主场景 / 配置资产 / 占位精灵）**

`Assets/_Project/Scripts/Editor/RogueliteMenu.cs`:
```csharp
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
```

- [ ] **Step 4: 编译检查 + 冒烟运行**

Run: 编译命令 → Play Mode（或 `Roguelite/构造 Main 场景` 后 Enter Play）。
Expected: 无 `error CS`；进入运行后玩家可见、第一波 2 秒后开始刷怪。

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Systems/CombatSystem.cs Assets/_Project/Scripts/GameBootstrap.cs Assets/_Project/Scripts/Editor/RogueliteMenu.cs
git commit -m "feat: 运行时装配入口(任意场景可玩)与多武器自动索敌、Editor一键场景/配置/精灵生成"
git push origin main
```

---

## Task 13: 端到端验收 + 收尾

**Files:** 无新增（可选：调参改 `GameConfig.Default()` 或已生成的 .asset）。

- [ ] **Step 1: Play Mode 手动验收清单（对照设计文档 §7）**

| # | 验收项 | 通过标准 |
|---|---|---|
| 1 | 移动 | WASD/方向键移动流畅，朝向跟随移动方向 |
| 2 | 远程武器 | 手枪/冲锋枪自动朝最近敌人射击，命中扣血，子弹池回收 |
| 3 | 近战武器 | 砍刀对面前扇形内敌人造成伤害（转圈可见） |
| 4 | AoE 武器 | 手雷定时以玩家为中心圆形爆炸 |
| 5 | 五波增长 | 每波敌人数量/强度递增，波间有商店停顿 |
| 6 | 金币 | 击杀掉落散落金币，靠近自动拾取并累加 |
| 7 | 商店 | 3 选 1 不重复；购买扣除金币；再次出现价格递增；商店期间玩家冻结 |
| 8 | 结算-失败 | HP 归零进入结算，显示失败 + 波数 + 击杀 |
| 9 | 结算-胜利 | 通关第 5 波显示胜利 + 击杀 |
| 10 | 重新开始 | 点击按钮重载对局，状态清零 |
| 11 | 防泄漏 | 连续打完两局，Console 无异常/空引用 |

- [ ] **Step 2: 数值与手感微调**

数值集中在 `GameConfig.Default()`（或生成的 .asset）：武器伤害/射程、敌人血量/移速、波次批次、商店价格。逐项调整后重跑 Step 1 冒烟。
Expected: 单局时长 3–6 分钟，难度缓升无断层。

- [ ] **Step 3: 构建冒烟（可选，Play 通过后进行）**

在 Build Settings 添加 `Assets/_Project/Scenes/Main.unity`，目标平台选 PC/Mac/Linux 或 WebGL，执行构建。
Expected: 构建成功，产物运行可与编辑器行为一致。

- [ ] **Step 4: 收尾提交**

```bash
git add -A
git commit -m "feat: MVP 可玩闭环(5波/三武器/3敌人/波间商店/结算/对象池) 完成端到端验收"
git push origin main
```

（若 Step 2 有调参，提交信息改为概括「数值平衡调参 + 验收通过」即可。）

---

## 完成标准（Definition of Done）

- [ ] 单场完整战斗：5 波敌人全部可打完，波间商店正常弹出
- [ ] 三大类武器自动索敌命中 + 伤害/暴击正确
- [ ] 金币掉落拾取、购买扣减、价格递增正确
- [ ] 死亡与通关都能进结算，重新开始可用
- [ ] 全部 EditMode 单测通过，无 `error CS`
- [ ] Play Mode 手动验收 11 项全过，连续两局无泄漏
- [ ] 代码符合 AGENTS.md：无 Update 内 Find/GetComponent、事件名与 Tag 用常量/枚举、私有字段 `_camelCase`、`#region` 组织、SO 驱动数值