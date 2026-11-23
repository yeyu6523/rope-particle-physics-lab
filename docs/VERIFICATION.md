# 本次交付验证

验证日期：2026-11-23（Asia/Shanghai）。本次从空 Git 工程新建程序，无需修改或覆盖用户原有源码。

## 结果与命令

| 检查 | 实际执行方式 | 结果 |
| --- | --- | --- |
| Release 编译 | `dotnet build PhysicsLab.sln -c Release --nologo` | 0 警告、0 错误 |
| 物理测试 | `dotnet run --project tests/PhysicsLab.Tests -c Release -- --report artifacts/core-tests.json` | 15 / 15 通过 |
| 原生界面集成检查 | `powershell -ExecutionPolicy Bypass -File scripts/verify.ps1` | 25 / 25 通过 |
| 单文件发布 | `powershell -ExecutionPolicy Bypass -File scripts/build.ps1` | Windows x64、自包含单文件 exe |
| 发布 exe 本体检查 | `PhysicsLab.exe --verify-ui <report-directory>` | 退出码 0，25 项检查通过 |
| 桌面交付 | 将发布的 `PhysicsLab.exe` 复制为桌面 `绳索与粒子实验室.exe` | 用 SHA256 核对发布文件与桌面文件完全相同 |

运行环境：Windows x64，.NET SDK 10.0.201；发布目录仅需一个 `PhysicsLab.exe`。发布 exe 大小和 SHA256 见 `verification/release-manifest.json`。对应源码/构建输入 SHA256 见 `verification/source-sha256.json`。物理和界面检查的实际输出见 `verification/core-tests.json` 与 `verification/ui-verification.json`。

另已实际通过 Windows PowerShell 的 `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` 复核，退出码 0。PowerShell 脚本使用带 BOM 的 UTF-8 编码，兼容 Windows PowerShell 对中文文件名的读取。

GitHub Actions 也已对源码提交 `b75915f` 完成 Windows 云端构建、物理测试与自包含发布，运行 `45122892876` 的结论为 `success`。此项验证不包含需要交互式桌面的 UI 截图检查。

## 测试范围

物理检查覆盖固定锚点、稳定后的连接长度误差、圆形碰撞、剪断拓扑和真实分离、拖拽释放、所有绳索预设极限参数有限性、碰撞开关、粒子发射速率、寿命回收、四种粒子预设连续 30 秒数值模拟、烟花二次发射、发射器移动、地面反弹、容量上限、确定性重置和非法步长拒绝。

界面集成检查在应用主线程操作真实 WinForms 控件事件，并检查模型变化、暂停/单步、剪断/重置、页签、所有预设、拖尾可用状态、紧凑窗口以及工具栏内容是否在容器内。原生 `PrintWindow` 导出应用自身客户区，核对了 1320×840 和 1050×740 两种窗口尺寸；截图不包含其他应用窗口。

15 项物理测试与 25 项界面检查采用不同层级，不将它们合并称为 40 项鼠标端到端测试。桌面自动化通道实际返回 native pipe 不可用，因此没有声称执行了系统鼠标自动化。程序截图中的 FPS 为 0 是验证器主动停止帧计时器所致；截图前会同步真实节点/粒子数量。未进行持续帧率基准测量或跨机器兼容性实测。

## 修复与复核

首次编译的 WinForms 分析器要求为自定义属性声明默认序列化值，已使用 `DefaultValue` 解决。后续构建一度被正在预览的本程序占用程序集，关闭本次启动的预览进程后重建通过。

独立界面审阅提出两项实质问题：未选中页签悬停/按下时文字对比度不足，以及飘雪预设保留了无效果的拖尾开关。已分别设置匹配的深色交互背景，并在飘雪中禁用且标注不适用的拖尾开关；相关预设状态已纳入集成检查。另外修复工具栏容器高度、按钮下缘裁切和截图前统计同步。

## 重新验证与恢复

运行 `scripts/verify.ps1` 可以重新产生物理和 UI 报告。运行 `scripts/build.ps1 -CopyToDesktop` 可以从源码重建并交付到桌面；存在旧桌面文件时会先保存带时间戳的备份。

这是新建独立工程，不安装服务、不修改注册表、不改变系统设置、不覆盖其他项目。源码版本由 Git 保存；需要旧版本时检出相应 Git 提交后重新构建。删除本程序及其文件即可移除交付内容；运行时缓存由 .NET 管理。错误日志仅在发生未处理异常时写入当前用户的 `LocalApplicationData/PhysicsLab/error.log`。
