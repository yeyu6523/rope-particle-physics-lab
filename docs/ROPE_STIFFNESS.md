# v1.0.1 绳索拉伸修复

用户反馈：绳索弹性太大。修复日期：2026-11-23。

## 实际原因与修改

原版每 1/120 秒仅积分一次，然后用局部距离约束传播张力。长绳在高分段和低迭代下无法及时收敛；鼠标还可以把节点直接拖到锚点绳段长度以外，形成无法满足的几何条件。

现在默认每物理步划分 8 个子步，高分段或低迭代时自动增加。阻尼从每个 1/120 秒约 0.997 改为约 0.99，按子步时长换算，减少持续振荡。拖拽限制在相连锚点的可达区域内，并把鼠标造成的几何位移分配给连通绳段，保留其已有速度；断口会解除相应限制。粒子算法与视觉样式未修改。

## 同场景前后对比

以下都是实际数值模拟结果。指标为活动绳段的最大正向拉伸 `(当前长度/静止长度 - 1)`，不把初始压缩算成弹性拉长。每次 Step 为 1/120 秒；普通受力场景连续运行 1200 步（模拟 10 秒），记录全程峰值。

| 场景 | 修复前峰值 | 修复后峰值 |
| --- | ---: | ---: |
| 悬索桥，默认参数 | 2.79% | 0.32% |
| 单端摆，默认参数 | 4.73% | 0.81% |
| 悬索桥，100 段 / 8 迭代 / 重力 1800 / 风力 800 | 40.57% | 0.65% |
| 单端摆，同上极限参数 | 118.95% | 1.30% |
| 中点缓慢拖向不可达位置 | 68.55% | 0.61% |
| 中点瞬间拖向不可达位置 | 243.06% | 1.94% |

拖拽场景先运行 600 步，再将中点拖向 `(600,670)`；缓慢拖动为 120 步线性插值，瞬间拖动为第一步直接切换目标，随后保持，共 120 步。该场景关闭圆形碰撞以隔离绳长问题。原版拖住时总长约 1341.8；修复后约 1000.5–1000.7，静止总长为 1000。修复后可达位置约 `(600,410.15)`，而不是强行跟随不可达的鼠标位置。

默认悬索桥与单端摆在 10 秒末的最大绳段拉伸分别约 0.014% 和 0.037%。以上是指定场景测量，不是所有交互与参数的绝对误差保证。绳子保留自然下垂与摆动；有限迭代和相互冲突的障碍物约束仍可能产生误差。

原始数据：[修复前](verification/rope-stretch-before.json)、[修复后](verification/rope-stretch-after.json)。`elapsedMs` 是这次无界面的测试耗时，不是窗口 FPS。

## 复现与回归

```powershell
dotnet run --project tests/PhysicsLab.Tests -c Release -- --rope-probe artifacts/rope-stretch.json
dotnet run --project tests/PhysicsLab.Tests -c Release -- --report artifacts/core-tests.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build.ps1
```

修复前版本为 Git 提交 `6e7ab70e15661bcf2f767d4e95bc5dc08924a196`，其 `RopeSimulation.cs` SHA256 为 `FF36A4794AA46ED8D1AB441C9A930926B2D1FCB1154D0A8E0307EE57BACF11F1`。实际在修改前保存源码和桌面 exe 副本，并先运行同一测量逻辑取得基线。

另在独立临时工程中，用原版绳索源码和当前回归测试运行对照：原版 15/21 通过，6 项拉伸、可达范围、变步长等检查失败；修复版 21/21 通过。对照失败报告见 `verification/rope-baseline-regressions.json`。

## 本地恢复

旧桌面 exe 保存在 `artifacts/rope-stiffness/before/绳索与粒子实验室.exe`，SHA256 为 `B82B6BC7CA11A11D5DBDEA3D0EDAFC9CCC25412DA1C06D647890F1714292CB88`。退出正在运行的实验室后，将该文件复制回桌面即可恢复原版。恢复副本在额外临时目录中校验，发布的新版本会保留。GitHub 的 v1.0.0 Release 也保留原版成品。
