# Roguelite MVP 设计文档（类土豆兄弟）

> 日期：2026-09-09　状态：已获用户确认

## 1. 背景与目标

在空白的 Unity 2022.3 LTS（URP 2D）项目中，从零搭建一个类似《土豆兄弟》(Brotato) 的 2D 俯视角肉鸽游戏。首版目标是**单场完整战斗**：玩家自动攻击、专注走位，熬过 5 波敌人，期间通过波间商店购买增益变强，通关或死亡后结算。

核心验证点：**核心战斗手感 + 完整肉鸽循环**能否跑通，为后续扩展（更多波次、武器、角色）打底。

## 2. 范围定义

### In（首版包含）
- 单人、单局、无存档、无局外成长
- 纯自动攻击，玩家只用键盘移动
- 三大类武器：远程射弹 / 近战挥砍 / 范围AOE，共 3-4 把
- 3 种敌人（追逐 / 远程 / 坦克）
- 5 波体验局，难度随波数增长
- 波间商店：随机 3 选 1 属性增益，击杀掉落金币
- HUD（血量/金币/波数）、商店面板、结算面板（胜利/失败）
- 代码生成占位美术（运行时生成简单图形精灵）

### Out（首版不做）
- 局外成长、角色解锁、难度解锁
- 复杂词条/宝石/合成等深度构筑
- 手柄/移动端输入
- 存档与设置菜单
- 音效与正式美术

## 3. 玩法设计

### 3.1 核心循环
```
进入战斗波 → 敌人从屏幕外刷出并攻击玩家 → 玩家自动索敌反击
→ 清完一波（无剩余敌人且不再刷新）→ 掉落结算 + 商店 3 选 1
→ 下一波（更强）→ …… → 活过第 5 波 = 胜利；HP 归零 = 失败
```

### 3.2 玩家
- 键盘 WASD/方向键移动（Rigidbody2D），移动方向为玩家朝向
- 属性由 `PlayerStats` 管理：`MaxHP / MoveSpeed / AttackSpeed / Damage / Range / CritChance / CritMultiplier / PickupRadius`
- 增益只通过商店加成，局内不升级属性（Brotato 风格，数值变化集中于商店）

### 3.3 武器与自动索敌
- 玩家身上挂 1 个武器位（首版单手武器），由 `CombatSystem` 托管
- 索敌规则：保存最近敌人的引用，目标死亡/离场则重新索敌；射程外不攻击
- 攻速节奏：冷却计时器驱动，与帧率无关（DeltaTime 累计），**禁在 Update 中高频创建对象**

三类武器行为（各一个组件，共享 `WeaponData`）：
| 类型 | 行为 | 实现要点 |
|---|---|---|
| Ranged 远程 | 朝目标发射子弹 | Projectile 走对象池，命中/到达射程/超时回收 |
| Melee 近战 | 贴身扇区判定 | 冷却结束在面前扇形范围做一次 Damage 判定 |
| AoE 范围 | 定时半径爆炸 | 冷却结束以玩家为中心圆形范围判定 |

### 3.4 敌人
- 全部由 `EnemyData`(SO) 驱动：`MaxHP / MoveSpeed / Damage / AttackInterval / DamageType / 颜色`
- 3 种行为：**追逐型**（移向玩家+接触伤害）、**远程型**（保持距离+发射弹幕）、**坦克型**（慢速高血量、接触伤害）
- 刷新：从视口外随机边缘生成，波内分批刷新，数量随波数线性增长
- 死亡掉落金币（掉落物走对象池），眨眼回收而非销毁

### 3.5 波次状态机（WaveManager）
| 状态 | 触发 | 行为 |
|---|---|---|
| Prepare | 开局/商店确认 | 短倒计时，播报「第 N 波」，刷新管理器就绪 |
| Combat | 倒计时结束 | 敌人按计划分批刷新；当**全部批次已生成且场上无存活敌人**时 → Interlude |
| Interlude | 全部批次已生成且场上无存活敌人 | 停止刷新，掉落结算，展示商店面板 |
| Shop | 玩家选完增益 | 应用增益 → Prepare 下一波 / 结算 |
| GameOver | HP ≤ 0 / 通关第 5 波 | 冻结输入，展示结算面板 |

玩家死亡立即进入 GameOver；状态切换用事件中心广播，UI 订阅刷新。

### 3.6 商店与金币
- 击杀金币掉落，掉落分散在死亡点周围
- Interlude 弹出商店：从增益池随机抽 3 个不重复项，玩家点选 1 个，购买消耗金币
- 增益池（首版）：`+伤害` `+攻速` `+最大生命` `+移速` `+射程` `+暴击率`；增益可叠加购买，价格递增
- 商店无刷新/跳过功能（体验局简化）

### 3.7 结算
- 胜利：通关第 5 波，显示「胜利」+ 击杀数 + 波数
- 失败：显示「失败」+ 存活波数 + 击杀数
- 提供「重新开始」按钮（Scene 重载）

## 4. 技术架构

### 4.1 目录结构（对齐 AGENTS.md）
```
Assets/_Project/
├── Scripts/
│   ├── Core/         # Const、Enums、GameEvents（事件中心）
│   ├── Data/         # WeaponData / EnemyData / WaveConfig / ShopItemData (SO)
│   ├── Managers/     # GameManager、PoolManager、WaveManager
│   ├── Entities/     # Player、PlayerController、PlayerStats、Enemy(基类+3子类或行为组件)、Projectile、Pickup
│   ├── Systems/      # CombatSystem、DamageSystem、ShopSystem、EnemySpawner
│   ├── UI/           # HudView、ShopView、GameOverView
│   └── Utils/        # ObjectPool（泛型）
├── Scenes/           # 主场景（改造 SampleScene 或新建 Main）
├── Prefabs/          # 占位精灵/预制体
└── ScriptableObjects/  # 生成的武器/敌人/波次/商店配置资产
```

### 4.2 模块职责
| 模块 | 职责 | 关键依赖 |
|---|---|---|
| GameManager | 全局状态机、游戏状态广播（引用游戏配置） | WaveManager、Player、UI |
| WaveManager | 波次状态机、波次数据驱动刷新计划 | EnemySpawner、GameEvents |
| PoolManager | 统一对象池（敌人/子弹/掉落），按 prefab 分池 | — |
| CombatSystem | 挂载玩家身上：索敌 + 驱动武器组件冷却 | Enemy 集合、PlayerStats |
| DamageSystem | 统一伤害结算：`DealDamage(target, value, crit)`，处理暴击、死亡事件 | GameEvents |
| ShopSystem | 抽 3 项增益、扣金币、应用增益 | PlayerStats、BonusPool |
| EnemySpawner | 按 WaveConfig 从池中激活敌人、视口外定位 | PoolManager |
| UI (HUD/Shop/GameOver) | 订阅事件刷新显示；ShopView 点击回调 ShopSystem | GameEvents |

### 4.3 数据流
```
Enemy 死亡 → DamageSystem 发 EnemyDied(金币)
→ 击杀数/金币累计到 PlayerStats → 波敌人清空 → WaveManager → Interlude
→ ShopSystem 抽 3 项 → 玩家点击 → 扣金币/改 PlayerStats → Prepare 下一波
```

## 5. 数据设计（ScriptableObject）

- `WeaponData`：类型、伤害、攻速、射程/范围、弹速（远程）、暴击率加成、名称、颜色
- `EnemyData`：类型、MaxHP、移速、接触/远程伤害、攻击间隔、掉落金币区间、预制体引用、颜色
- `WaveConfig`：波数列表，每波 = `{ 敌人组(类型,数量), 分批参数 }` + 建议间隔
- `ShopItemData`：增益类型、叠加规则、基础价、增幅（供推导递增价）

工具脚本生成首版配置（代码内默认值 + Editor 菜单一键生成）。

## 6. 边界与错误处理

- 对象池双保险：回收时校验「已被激活」，取用时校验「池空则扩容」，防止空引用与泄漏
- 敌人/子弹目标失效：目标销毁/出射程 → 立即重新索敌，避免空引用
- 波次状态机防重入：状态切换只允许直线迁移（含 GameOver 单向终态）
- 商店期间冻结玩家移动与攻击
- 播放模式退出时清空静态事件引用，防止跨场景残留

## 7. 测试与验收

- 纯逻辑方法（伤害点数/暴击判定/金币区间）抽成静态可测方法预留单测入口；首版以手动验收为主
- Play Mode 手动验收清单：移动顺畅；三类武器都能自动命中并造成伤害；敌人按波增长；击杀掉金币；商店 3 选 1 生效且金币正确扣减；死亡/通关都正确进入结算；连续两局无泄漏报错（Console 无异常）

## 8. 后续扩展（本阶段不做，仅备忘）

- 波数延长与难度曲线调参（数据层已解耦）
- 多武器位/切换武器、更多武器类型
- 局外成长（角色、解锁、统计）、手柄/移动输入、正式美术与音效