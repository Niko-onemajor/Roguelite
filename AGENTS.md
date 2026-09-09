# Unity Roguelike 项目 AI 代理指令

## 🎯 项目概述
这是一个使用 **Unity 2022.3 LTS (2022.3.62f3c1)** 和 **C#** 开发的2D俯视角Roguelite游戏，核心玩法类似《土豆兄弟》(Brotato)。

## 🛠 技术栈与规范
- **引擎**: Unity 2022.3 LTS (URP渲染管线)
- **语言**: C# (.NET Standard 2.1)
- **架构**: 基于组件的开发，优先使用ScriptableObject管理数据
- **命名规范**:
  - 公共字段/属性: `PascalCase`
  - 私有字段: `_camelCase`
  - 方法/类: `PascalCase`
  - 游戏对象: 清晰描述功能，如 `Player`, `Enemy_Spawner`
- **代码风格**:
  - 使用 `#region` 组织代码块 (如: `#region Properties`, `#region Unity Lifecycle`)
  - 禁止使用 `Update()` 进行高频操作，优先考虑 `Coroutine` 或 `Timer`
  - 所有可序列化的字段都添加 `[SerializeField]` 标签，保持私有

## 🚫 禁止事项 (Never 规则)
- **禁止** 在 `Update()` 中使用 `Find()` 或 `GetComponent()` 方法
- **禁止** 使用硬编码的字符串作为事件名或Tag，必须使用常量或枚举
- **禁止** 提交包含编译错误的代码
- **禁止** 直接修改Unity Engine源码或Editor文件

## 📂 关键目录结构
- `Assets/_Project/` - 所有项目资源
  - `Scripts/` - C# 脚本
  - `Scenes/` - 游戏场景
  - `Prefabs/` - 预制体
  - `ScriptableObjects/` - 游戏数据配置
- `Assets/_Project/Scripts/Managers/` - 游戏管理器 (GameManager, PoolManager)
- `Assets/_Project/Scripts/Entities/` - 玩家、敌人、物品等实体
- `Assets/_Project/Scripts/Systems/` - 独立的游戏系统 (战斗系统, 掉落系统)