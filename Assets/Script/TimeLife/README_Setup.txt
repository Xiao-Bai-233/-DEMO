╔══════════════════════════════════════════════════════════════╗
║           时间生命系统 · 完整配置向导                        ║
╚══════════════════════════════════════════════════════════════╝

============================================================
第一步：创建 Phase 配置资产（共4个）
============================================================

1. 在 Project 窗口,右键 → Create → TimeLife → WeekPhaseConfig
2. 命名为：WeekPhase_0  （幼儿期）
3. 再次创建,分别命名为 WeekPhase_1, WeekPhase_2, WeekPhase_3
4. 选中4个文件,拖到 Assets/Resources/TimeLife/ 文件夹下
   （没有这个文件夹就自己建一个 Resources 文件夹再建 TimeLife）
5. 分别点开4个文件,在 Inspector 中配置：

┌─────────────────────────────────────────────────────┐
│ WeekPhase_0 (幼儿期)                                 │
├─────────────────────────────────────────────────────┤
│ phaseName = "幼儿期"                                  │
│ maxMoveSpeed = 5          ← 走得慢                    │
│ jumpHeight = 3             ← 跳得低                    │
│ wallJumpEnabled = false    ← 不会墙跳                   │
│ grapplingHookEnabled = false                          │
│ timeFieldEnabled = false                              │
│ playerSprite = [拖入幼儿版精灵]                         │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ WeekPhase_1 (成长期)                                  │
├─────────────────────────────────────────────────────┤
│ phaseName = "成长期"                                  │
│ maxMoveSpeed = 7                                     │
│ jumpHeight = 4                                       │
│ wallJumpEnabled = true     ← 学会爬墙了                │
│ grapplingHookEnabled = true ← 获得钩锁                │
│ timeFieldEnabled = false                              │
│ playerSprite = [拖入少年版精灵]                         │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ WeekPhase_2 (成熟期)                                  │
├─────────────────────────────────────────────────────┤
│ phaseName = "成熟期"                                  │
│ maxMoveSpeed = 10        ← 巅峰状态                   │
│ jumpHeight = 5                                        │
│ wallJumpEnabled = true                                 │
│ grapplingHookEnabled = true                            │
│ timeFieldEnabled = true  ← 觉醒时间力场                │
│ playerSprite = [拖入成年版精灵]                         │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ WeekPhase_3 (衰退期)                                  │
├─────────────────────────────────────────────────────┤
│ phaseName = "衰退期"                                  │
│ maxMoveSpeed = 6         ← 越来越慢                    │
│ jumpHeight = 3                                        │
│ wallJumpEnabled = false  ← 失去能力...                 │
│ grapplingHookEnabled = false                          │
│ timeFieldEnabled = false                               │
│ playerSprite = [拖入老年版精灵]                         │
└─────────────────────────────────────────────────────┘

============================================================
第二步：准备4套精灵图（如何换外观）
============================================================

【方法A：单张精灵（最简单）】
每阶段只用一张静态图（无动画）：
  → WeekPhaseConfig.playerSprite 里拖入 Sprite 即可
  → 运行时自动替换 SpriteRenderer.sprite

【方法B：完整动画换装（推荐）】
1. 准备4套 Sprite Sheet（各包含 Idle/Run/Jump/Fall 帧）
2. 导入 Unity，切成 Sprite 序列
3. 创建 4 个 Animator Override Controller：
   - 在 Project 窗口右键 → Create → Animator Override Controller
   - 命名为：Anim_幼儿期、Anim_成长期、Anim_成熟期、Anim_衰退期
4. 每个 Override Controller 把基类动画的 Sprite 换成对应年龄的
5. 在 WeekPhaseConfig.animatorController 里拖入对应的 Override Controller
  → 运行时自动替换 Animator.runtimeAnimatorController

============================================================
第三步：调整速度曲线
============================================================

TimeLifeManager.speedCurve 控制全过程的平滑速度变化：
  X轴: 28天归一化(0~1), 1=满血, 0=快死了
  Y轴: 速度倍率

推荐曲线形状（在 Inspector 点开曲线编辑器调整）：
  ┌───┬───┬───┬───┐
  │ 0 │ 1 │ 2 │ 3 │  ← 周
  ├───┼───┼───┼───┤
  │0.6│0.9│1.2│0.5│  ← 速度倍率

  • 幼儿期(28~22天): 0.6x  → 0.8x
  • 成长期(21~15天): 0.8x  → 1.0x
  • 成熟期(14~8天):  1.0x  → 1.2x  (巅峰)
  • 衰退期(7~1天):   1.0x  → 0.4x  (越来越慢)

============================================================
第四步：放置时间条 UI（只在需要的场景）
============================================================

1. 打开需要的场景（如 Level_1）
2. Hierarchy 中：右键 → UI → Canvas（如果已有则复用）
3. Canvas 下创建空物体，命名为 "TimeUI"
4. 给 TimeUI 添加组件：Add Component → 搜索 TimeUI → 添加
5. 创建格子预制体：
   a. 在 Canvas 下创建一个 Image → 命名为 "DayCell"
   b. 拖到 Project 窗口成为预制体
   c. 从 Canvas 删除这个 Image
   d. 在 TimeUI 组件中，把 DayCell 预制体拖入 cellPrefab 字段
6. （可选）创建两个 Text/TMP 显示周名和剩余天数：
   - 拖入 TimeUI.weekLabel
   - 拖入 TimeUI.dayCountLabel

============================================================
第五步：运行测试
============================================================

1. 按 Play 运行
2. 按 Tab → 打开暂停面板
3. 跳陷阱死亡 → 观察右上角天数减少
4. 每过7天 → 观察角色外观变化、能力变化、移动速度变化

在 Console 窗口可以看到日志：
  💀 死亡！剩余天数: 27, 当前阶段: 幼儿期
  ⏰ 进入新阶段: 成长期 (第2周)
  SkillManager: 技能状态更新
