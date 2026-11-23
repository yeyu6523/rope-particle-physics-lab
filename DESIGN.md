---
name: "绳索与粒子实验室"
description: "以深蓝模拟画布、浅色原生参数区和直接操控为核心的 Windows 物理实验界面"
colors:
  header: "#182B41"
  canvas: "#0C192A"
  panel: "#F2F6FA"
  ink: "#1C3046"
  muted: "#51677B"
  white: "#F0F6FC"
  copper: "#F7B26C"
  teal: "#68DED3"
  caption: "#A4B9CD"
  control-white: "#FFFFFF"
  control-border: "#C3D0DB"
  primary-hover: "#2F4B65"
  primary-pressed: "#3F5E78"
  secondary-hover: "#E0EAF3"
  secondary-pressed: "#CEDEEB"
  status-surface: "#E2EAF2"
typography:
  display:
    fontFamily: "Microsoft YaHei UI"
    fontSize: "19pt"
    fontWeight: 700
  headline:
    fontFamily: "Microsoft YaHei UI"
    fontSize: "15pt"
    fontWeight: 700
  title:
    fontFamily: "Microsoft YaHei UI"
    fontSize: "12pt"
    fontWeight: 700
  body:
    fontFamily: "Microsoft YaHei UI"
    fontSize: "9pt"
    fontWeight: 400
  numeric:
    fontFamily: "Consolas"
    fontSize: "9pt"
    fontWeight: 400
rounded:
  square: "0px"
spacing:
  control-block: "8px"
  action-gap: "10px"
  field-gap: "16px"
  panel-inset: "20px"
  shell-inset: "24px"
components:
  button-primary:
    backgroundColor: "{colors.header}"
    textColor: "{colors.control-white}"
    typography: "{typography.body}"
    rounded: "{rounded.square}"
    height: "32px"
    width: "112px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
    textColor: "{colors.control-white}"
  button-primary-pressed:
    backgroundColor: "{colors.primary-pressed}"
    textColor: "{colors.control-white}"
  button-secondary:
    backgroundColor: "{colors.control-white}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.square}"
    height: "32px"
    width: "94px"
  button-secondary-hover:
    backgroundColor: "{colors.secondary-hover}"
    textColor: "{colors.ink}"
  button-secondary-pressed:
    backgroundColor: "{colors.secondary-pressed}"
    textColor: "{colors.ink}"
  tab-rope-selected:
    backgroundColor: "{colors.copper}"
    textColor: "{colors.ink}"
    height: "32px"
    width: "132px"
  tab-particles-selected:
    backgroundColor: "{colors.teal}"
    textColor: "{colors.ink}"
    height: "32px"
    width: "132px"
  tab-inactive:
    backgroundColor: "{colors.header}"
    textColor: "{colors.white}"
    height: "32px"
    width: "132px"
  sidebar:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    padding: "16px 12px 20px 20px"
    width: "292px"
  simulation-canvas:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.white}"
---

# Design System: 绳索与粒子实验室

## Overview

**Creative North Star: "可直接操作的物理实验台"**

这是一套从现有 Windows 成品提取的视觉系统，而非新设定的品牌偏好。深蓝绘图区承载运动、碰撞与约束，浅色原生参数区承载精确控制；中文标签、清楚的状态与持续反馈服务于现场演示。

界面保持紧凑、平整、可辨读。铜色与青色区分实验身份，参数与按钮不抢占模拟的视觉中心。这里的“实验台”是对已实现界面的描述性命名，未代表用户另行确认的品牌主张。

本文依据 `src/PhysicsLab.App/Theme.cs`、`MainForm.cs`、`SimulationCanvas.cs` 及成品截图记录当前实现。适用平台为 C# / .NET 10 WinForms；前置 token 为规范数据，代码仍是运行时事实来源。侧车中的 HTML/CSS 是供面板查看的近似样片，不是替换原生控件的实现要求。

**Key Characteristics:**

- 深蓝模拟画布与浅色原生控制区形成明确分工。
- 铜色标记绳索，青色标记粒子页面与关键物理标记。
- 中文采用 Microsoft YaHei UI；Consolas 仅用于工具栏性能数值。
- 矩形控件、细边框与色阶建立层次，不依赖装饰阴影。
- 桌面缩放时保留可操作侧栏，由画布吸收窗口尺寸变化。

## Colors

低明度的蓝色底色组织工作空间，暖铜与清青提供实验身份，轻蓝白色区域保证表单阅读清楚；具体值统一见前置 token。

### Primary

- **Copper / 铜色绳索**（`copper`）：绳索连线及绳索页面选中态。画布中的暖色运动链与冷色背景形成清楚区分。
- **Header / 深蓝操作色**（`header`）：标题条、主要播放按钮及未选中页面底色，不把实验身份色误用于所有主操作。

### Secondary

- **Teal / 清青标记**（`teal`）：粒子页面选中态、绳索固定锚点实心标记、粒子发射器定位环。

### Neutral

- **Canvas / 蓝墨画布**（`canvas`）：模拟区域的固定底色。
- **Panel / 蓝白参数面**（`panel`）：参数侧栏与窗体基础背景。
- **Ink / 深色正文**（`ink`）：浅底标签、参数标题、普通按钮及选中页面文字。
- **Muted / 次级蓝灰**（`muted`）：参数读数、说明、工具栏指标与状态文字。
- **White / 柔白画布文字**（`white`）：标题条、画布标题及暗底正文。它与纯白控件背景是不同 token。
- **Caption / 浅蓝说明**（`caption`）：标题条副标题、画布交互说明、图例和画布焦点轮廓。
- **Control White / 纯白控件面**（`control-white`）：工具栏、普通按钮与原生白色输入表面。
- **Control Border / 控件边框**（`control-border`）：普通平面按钮的细描边。
- **Status Surface / 状态栏面**（`status-surface`）：底部状态条，与参数区形成轻微明度分层。
- **Interaction Steps / 按钮状态阶**：`primary-hover`、`primary-pressed` 与 `secondary-hover`、`secondary-pressed` 分别服务于深色和浅色按钮的悬停、按下反馈。

粒子颜色由 48 档 HSV 色谱生成（饱和度 0.52、明度 1），透明度以四档随寿命衰减。喷泉、飘雪与旋涡各取对应色段，烟花使用全色谱；这些是数据编码，不是额外品牌色。参考网格、地板、障碍物使用画布内的局部蓝色层次，不提升为通用界面语义色。

侧车为每个颜色提供八阶 OKLCH 色带，色带是面板预览所需的派生数据，不是已实现的运行时色阶；应用仍只使用源代码中明确给出的颜色与状态值。

**The Simulation First Rule.** 让运动对象成为最醒目的内容；新增控制沿用浅色侧栏或工具栏，不在模拟区叠加装饰卡片。

## Typography

**Display Font:** Microsoft YaHei UI。
**Body Font:** Microsoft YaHei UI。
**Label/Mono Font:** Consolas，仅工具栏性能读数使用。

**Character:** 清晰、克制的中文桌面文字。用字号和粗细建立层级，不以装饰字体制造实验室气氛。字体对象没有声明备用字体栈，字体不可用时由 Windows 处理替换。

### Hierarchy

- **Display**：粗体产品标题，位于深蓝标题条，对应前置 `display`。
- **Headline**：粗体实验名称，绘制于画布左上，对应 `headline`。
- **Title**：粗体“实验设置”等侧栏标题，对应 `title`。
- **Body**：窗体继承的正文、按钮、表单标签、画布说明与状态栏，对应 `body`。
- **Numeric**：右对齐的 FPS、对象数量与固定步长，对应 `numeric`。侧栏参数读数仍继承中文正文字体，不能误记为等宽字体。

字号单位为 WinForms `Font` 的点（pt），不是网页 px。原生控件行高由 Windows 布局与 DPI 缩放决定；项目未建立独立行高或字距 token。

**The Numeric Type Rule.** 中文标题、说明与参数标签沿用比例中文字体；固定字宽只用于当前已经采用该字体的性能读数，不把整套界面改成等宽字。

## Layout

桌面壳采用纵向四段结构：标题/页签区（82 基准像素）、播放工具栏（48）、伸缩工作区、底部状态栏（28）。默认客户区为 1320 × 840；窗体 `MinimumSize` 为 1050 × 740，此属性包含窗口边框，不等同于客户区。现有紧凑截图另以 1050 × 740 客户区验证。

工作区是“可伸缩画布 + 固定参数侧栏”的双栏结构。侧栏基准宽度（292），内部常用字段宽度（246），保持纵向排列并开启 AutoScroll；缩小时出现原生垂直滚动条，控件不会改排成移动端抽屉。标题条右侧页签容器基准宽度（284），两个页签横排。工具栏左侧操作区基准宽度（460），右侧性能读数右对齐并占剩余空间。

窗体采用 `AutoScaleMode.Dpi`。上列几何值为代码的桌面基准布局值，不是跨平台硬性 CSS 尺寸。壳层常用左右内距沿用 `shell-inset`，侧栏沿用 `panel-inset`；操作之间使用 `action-gap`，字段组之间使用 `field-gap`，控件下方常见 `control-block`。不把某一处特殊偏移扩充为完整间距量表。

模拟世界独立于窗口布局：以 1200 × 760 的世界坐标等比缩放，并在画布中居中，避免横纵拉伸。世界网格间距（60 世界单位），不等同于桌面布局间距。画布标题、说明、底部图例和暂停标记使用画布客户坐标叠加绘制，不随物理世界缩放。

## Elevation & Depth

项目未实现装饰投影。层级来自深蓝标题、近黑画布、浅色工具栏与参数区之间的色面关系，以及细边框、参考网格和物理对象的对比。障碍物以实色圆和细轮廓表现体积位置；不把它们解释为卡片或材质阴影。原生 ComboBox、TrackBar、CheckBox 的边缘与禁用态由 Windows 主题绘制，不另行建立固定阴影 token。

**The Native State Rule.** 保留 Windows 控件的键盘焦点、禁用态与系统绘制；新控件沿用现有原生行为，不用静态图形代替可操作控件。

## Shapes

界面容器与平面按钮以直角矩形为主，沿用 `square`，没有统一的圆角卡片体系。普通按钮保留细描边；原生表单控件的边缘由系统绘制。

圆形、圆点与方块在画布里各有含义：固定锚点为青色实心方块加外框，可运动节点为小圆点，碰撞障碍物为圆形加十字中心，粒子发射器为虚线环与十字定位线。绳索连线使用圆形端帽（4.2 世界单位线宽），这些几何身份不应挪作普通界面装饰。

## Components

### Buttons

紧凑、明确的原生平面操作按钮。

- **Shape:** 直角、细边框，基准高度沿用前置按钮 token；标准次要按钮与播放主按钮分别使用各自宽度。
- **Primary:** 暂停/继续使用深蓝底与纯白字。文字包含 Space 快捷键，并随运行状态切换。
- **Secondary:** 单步、重置、保存画面使用纯白底、深色字和浅蓝灰边框；“在发射器处爆发”为侧栏宽按钮（246 基准像素）。
- **Hover / Pressed:** 使用已提取的按钮状态阶，无自定义缓动或位移动画。
- **Focus / Disabled:** 保留原生键盘焦点；单步在运行时禁用，暂停时启用。不要只改变颜色而保留错误的可用状态。

### Inputs / Fields

参数操作保持 Windows 原生控件习惯。

- **Preset:** 只读选择式 ComboBox（DropDownList），不接受任意输入；标签位于上方。
- **Parameter:** 标签左置，格式化数值与单位右置；下方为无刻度 TrackBar。数值在拖动时即时更新。
- **Checkbox:** 使用原生 CheckBox，标签可直接说明功能。飘雪预设禁用不适用的拖尾选项，并在标签中说明“不适用”。
- **Focus / Disabled:** 继承系统原生绘制与可访问名称；不从截图中的系统蓝色滑块推导品牌色。
- 本项目没有自定义文本输入、错误输入框、chip 或通用卡片组件，文档不补造这些变体。

### Navigation

两枚矩形页签位于标题条右侧，以“绳索模拟 [1] / 粒子系统 [2]”显示身份与快捷键。未选中为深蓝底、浅字与蓝灰描边；选中绳索使用铜色，选中粒子使用青色，两者均改为深色文字。悬停与按下态由对应页面色阶实现；切换页面同步替换画布与参数区，避免仅改变页签外观。

### Parameter Sidebar

浅蓝白色纵向参数区，不使用卡片包裹每个字段。标题、预设、滑块、复选框和简短说明共享同一背景。通过字段组间距与文字层级分组；内容溢出时允许原生滚动。

### Simulation Canvas

系统的签名组件，是直接操作的绘图控件。深蓝背景上以低对比蓝色显示网格与地板，铜色绳索和粒子数据色保持突出；画布标题、交互说明与图例提供就地解释。

- 绳索节点附近的悬停提示为细浅色圆环；固定锚点、节点和障碍物可拖拽，右键可剪断绳索。
- 粒子发射器使用青色虚线环；左键移动，右键爆发。飘雪不绘制发射器环，与其顶部持续发射行为一致。
- 画布使用十字光标；获得键盘焦点时在内侧绘制系统焦点矩形。
- 暂停时右上显示矩形状态标记“已暂停 · Space 继续”，播放按钮同步显示继续。
- 绘制采用抗锯齿与双缓冲。16 ms 计时器是重绘节奏，物理固定步长为 1/120 s；这不是 UI 过渡动画规范。

## Do's and Don'ts

### Do:

- **Do** 以 Theme.cs、MainForm.cs 与 SimulationCanvas.cs 的现有实现为 token 来源；本文件记录视觉约定，不代替运行代码。
- **Do** 为新实验继续使用深色画布、浅色参数区以及明确的页面身份色。
- **Do** 保留数值单位、暂停提示、可见焦点和与预设能力一致的禁用状态。
- **Do** 使用原生布局与 DPI 缩放；在桌面紧凑尺寸验证工具栏、指标和侧栏滚动。
- **Do** 在画布内使用真实模拟几何与数据颜色，保持图例与交互说明可读。

### Don't:

- **Don't** 给现有平面控件增加统一圆角卡片、悬浮阴影或网页式大面积留白。
- **Don't** 把铜色或青色当作所有操作的通用主按钮填色；主播放按钮仍使用深蓝。
- **Don't** 将粒子按 HSV 生成的分组颜色扩展为界面导航或表单主题色。
- **Don't** 把原生 TrackBar、ComboBox、CheckBox 的系统主题颜色误记为固定的品牌 token。
- **Don't** 将 WinForms 字号的 pt、桌面布局像素或模拟世界单位互相混用，也不要套用移动端断点。
