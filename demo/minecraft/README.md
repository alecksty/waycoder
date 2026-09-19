# ⛏️ 方块世界 — 我的世界风格体素建造游戏

一个「我的世界」风格的第一人称 3D 体素建造游戏，**纯前端、无构建、无依赖**（仅 CDN 加载 Three.js，零 npm/nuget 包）。可放置/破坏方块、程序化生成无限地形、昼夜循环、存档。

## ✨ 功能

- **程序化无限地形**：2D Simplex 噪声 + fbm 分形生成连绵山丘、湖泊；草地/泥土/石头/沙子/水/树
- **方块类型**：草地、泥土、石头、沙子、木材、树叶、煤矿、铁矿、基岩（9 种，各面不同色）
- **建造/挖掘**：左键破坏、右键放置；射线命中方块高亮框；可选 8 种快捷栏方块（数字键/滚轮切换）
- **昼夜循环**：太阳移动、天色/雾色随时间渐变（白天蓝 → 黄昏橙 → 夜晚深蓝）
- **第一人称移动**：WASD + 跳跃 + 疾跑（Ctrl）、鼠标视角、体素 AABB 碰撞
- **存档/读档**：localStorage 保存玩家位置与世界编辑，重开继续
- **程序化音效**：Web Audio API 合成破坏/放置/跳跃等音效（无音频文件）

## 🎮 操作

| 按键 | 功能 |
|---|---|
| `W A S D` | 移动 |
| `鼠标` | 转动视角（点击画面锁定） |
| `空格` | 跳跃 |
| `Ctrl` | 疾跑 |
| `左键` | 破坏方块 |
| `右键` | 放置方块 |
| `1-9` / `滚轮` | 选择快捷栏方块 |
| `Esc` | 暂停/返回菜单 |

## 🚀 运行

```bash
cd demo/minecraft
python3 -m http.server 8000
# 打开 http://localhost:8000
```

或直接用浏览器打开 `index.html`（Chrome/Edge/Firefox/Safari）。

## 📂 结构

```
minecraft/
├── index.html       # 菜单/HUD/快捷栏/准星 + Three.js importmap
├── css/style.css    # 夜色主题 UI
└── js/
    ├── main.js      # 游戏主循环、场景灯、昼夜、交互、UI、存档
    ├── world.js     # 无限区块加载/卸载、跨区块 get/set、地形+树生成
    ├── chunk.js     # 体素块存储 + 只渲染暴露面的 BufferGeometry
    ├── player.js    # 第一人称控制器 + 体素 AABB 碰撞
    ├── raycast.js   # 3D DDA 体素射线检测
    ├── noise.js     # Simplex 噪声 + fbm 分形（确定性）
    ├── input.js     # 键盘/鼠标输入 + 指针锁定
    ├── audio.js     # Web Audio 程序化音效
    └── storage.js   # localStorage 存档
```

## 🛠 技术

- **Three.js 0.160**（CDN importmap，ES modules）
- **纯 ES6 modules** — 无构建步骤
- **零第三方包** — 全手写算法（Simplex/DDA/碰撞）
- **顶点色材质** — 单材质多色，渲染高效

## 📜 License

MIT — 自由学习、复刻、再创作。
