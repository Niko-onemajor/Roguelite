---
name: unity-debugging
description: 用于在Unity项目中系统性地定位和修复Bug的工作流。当用户要求调试、定位问题或修复Bug时使用。
---

# Unity 调试与问题排查技能

## 工作流步骤
1.  **复现与描述**: 要求用户提供清晰的问题复现步骤和预期行为。
2.  **日志分析**: 检查 `Player.log` 文件或在代码中添加 `Debug.Log` 以追踪关键变量。
3.  **定位代码**: 根据问题描述，在 `Assets/_Project/Scripts/` 目录下定位相关代码文件。
4.  **使用调试器**: 建议在Visual Studio中设置断点，特别是 `Awake()`, `Start()`, `Update()` 等生命周期方法。
5.  **提出修复方案**: 分析根本原因后，提供具体的代码修改方案。
6.  **验证**: 建议在修改后，在Unity编辑器中进入Play模式进行测试。

## 常见问题检查清单
- [ ] 检查NullReferenceException: 是否所有GameObject引用都已正确赋值？
- [ ] 检查逻辑错误: 条件判断是否正确？循环是否可能无限？
- [ ] 检查性能: Update()中是否有高开销操作？