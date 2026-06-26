# Elemental Loop TD — C# MonoGame 原生 Windows 桌面版设计文档

## 概述

将 `elemental_loop_td.html`（3086 行 Canvas 2D 塔防游戏）重写为 C# + MonoGame 的 Windows 原生桌面应用。原版使用 HTML/CSS/Canvas 2D 渲染与 DOM UI，新版使用 MonoGame 框架 + `SpriteBatch` 硬件加速渲染 + 程序化纹理生成。

## 技术栈

- **语言**: C# 12 (.NET 8.0)
- **框架**: MonoGame 3.8+ (继承自 Microsoft.XNA)
- **渲染**: SpriteBatch + RenderTarget2D (离屏缓存)
- **存档**: System.Text.Json → 本地文件 (`%AppData%/ElementalLoopTD/save.json`)
- **发布**: dotnet publish -r win-x64 (单文件 exe)

## 项目结构

```
ElementalLoopTD/
├── ElementalLoopTD.csproj
├── Program.cs                     # 入口
├── Game1.cs                       # 主 Game 类 (Update/Draw 循环)
├── Core/
│   ├── Config.cs                  # 所有常量定义
│   ├── GameManager.cs             # 核心游戏状态 & 逻辑
│   ├── WaveManager.cs             # 波次生成 & 出怪
│   ├── ElementSystem.cs           # 元素反应系统
│   └── SaveManager.cs             # 文件存档
├── Entities/
│   ├── Tower.cs                   # 塔
│   ├── Monster.cs                 # 怪物
│   └── Projectile.cs              # 投射物
├── Particles/
│   ├── Particle.cs                # 基类
│   ├── DamageParticle.cs          # 伤害飘字
│   ├── ReactionParticle.cs        # 元素反应飘字
│   ├── TrailParticle.cs           # 迁塔轨迹
│   └── MergeBurst.cs              # 升级扩散环
├── Rendering/
│   ├── TextureGenerator.cs        # 程序化纹理生成
│   ├── PathRenderer.cs            # 路径离屏缓存 (RenderTarget2D)
│   └── GrassRenderer.cs           # 草地离屏缓存 (RenderTarget2D)
├── UI/
│   ├── HUD.cs                     # 状态栏
│   ├── InfoPanel.cs               # 塔属性面板
│   ├── BuildBar.cs                # 建造栏
│   ├── TowerSlots.cs              # 塔槽位
│   └── Overlays.cs                # 暂停/GameOver遮罩
└── Utils/
    ├── SafeMath.cs                # 数值安全工具
    └── Extensions.cs              # 辅助方法
```

## 架构设计

### 游戏循环

原版 `requestAnimationFrame` → MonoGame `Game` 类的 `Update(GameTime)` + `Draw(GameTime)`。

- `Update`: 波次管理、出怪、塔射击、怪物移动、投射物碰撞、粒子更新、清理
- `Draw` : SpriteBatch 批量渲染（背景缓存 → 实体 → 粒子 → UI 覆盖层）

### 输入系统

| 原版 (DOM 事件) | MonoGame |
|---|---|
| click / touchstart | `Mouse.GetState().LeftButton == ButtonState.Pressed` + 防抖 |
| contextmenu | `Mouse.GetState().RightButton == ButtonState.Pressed` |
| mousemove | `Mouse.GetState().Position` |
| keydown (ESC / P / 空格) | `Keyboard.GetState().IsKeyDown()` |
| 建造按钮点击 | UI 矩形区域点击检测 (或键盘快捷键 1/2/3) |

### 纹理生成策略

由于原版所有图形都是 Canvas 2D 即时绘制（塔身渐变、火焰水球冰晶形状、怪物触角眼睛、石板青苔纹路），新版在**初始化阶段**将所有图形预渲染到 `Texture2D` 纹理。

生成方式：
1. 创建 `RenderTarget2D` (例如 64x64)
2. 用 `SpriteBatch` 把 Canvas 2D 的绘图命令翻译为 MonoGame 的绘制调用
3. 保存为 `Texture2D`，运行时只做 `SpriteBatch.Draw(texture, pos, color)`

需要生成的纹理：
- **塔** (3 种 × 约 4 个等级区间 × 基座+塔身) ≈ 12 个纹理
- **怪物** (普通/精英 × 2 帧) ≈ 4 个纹理
- **投射物** (普攻/暴击) ≈ 2 个纹理
- **传送门** × 1
- **石板** (4-6 种变体) ≈ 6 个纹理
- **UI 元素** (按钮/面板/图标) ≈ 10 个纹理
- **粒子** (各类所需小圆形/闪点) ≈ 5 个纹理

### 离屏缓存

原版 `_pathCache` / `_grassCache` 用离屏 canvas 缓存静态绘图层。
新版用 `RenderTarget2D` 等价实现，在 `OnResize()` 或地图尺寸变化时重建。

```csharp
// PathRenderer.cs
public class PathRenderer {
    private RenderTarget2D _cache;
    public void BuildCache(GraphicsDevice gd, Waypoint[] waypoints, int width, int height);
    public void Draw(SpriteBatch sb); // 每帧 drawImage 等价
}
```

### UI 系统

原版用了 HTML + CSS + DOM 操作（状态栏、建造栏、infoPanel、暂停遮罩等）。
新版全部用 `SpriteBatch` 绘制 `Texture2D` 和 `SpriteFont` 文字实现。

**HUD 状态栏**: 绘制在 Canvas 顶部区域，使用纹理背景 + 文字
**建造栏**: 底部 3 个按钮（火/水/冰），矩形区域点击检测
**InfoPanel**: 塔选中时在塔旁弹出的半透明浮层，显示属性 + 升级按钮
**遮罩**: 暂停/GameOver/存档恢复弹窗，全屏半透明覆盖层 + 中间面板

### 数据流

```
用户输入 (鼠标/键盘)
  → GameManager.HandleTap(x, y)
    → 选中塔 / 建塔 / 取消
  → GameManager 修改状态 (towers/monsters/gold/wave)
  → 自动存档 (防抖 2s)

帧循环:
  Update(dt):
    WaveManager.Update(dt)        → 出怪 / 倒计时
    foreach Tower.Update + Shoot  → new Projectile
    foreach Monster.Update        → 移动 + 元素计时
    foreach Projectile.Update     → 碰撞 → ElementSystem.Resolve → TakeDamage
    foreach Particle.Update
    filterInPlace (RemoveAll)
    CheckGameOver

  Draw():
    sb.Begin()
    GrassRenderer.Draw(sb)        → 离屏缓存
    PathRenderer.Draw(sb)         → 离屏缓存
    Portal.Draw(sb)
    foreach Tower.Draw(sb)
    foreach Monster.Draw(sb)
    foreach Projectile.Draw(sb)
    foreach Particle.Draw(sb)
    HUD.Draw(sb)
    BuildBar.Draw(sb)
    InfoPanel.Draw(sb)
    Overlays.Draw(sb)
    sb.End()
```

## 实体设计

### Tower

```csharp
public class Tower {
    public string Type;              // "fire" | "water" | "ice"
    public TowerDef Def;             // 静态配置
    public Vector2 Position;
    public int Level;
    public float Cooldown;
    public float CritRate;
    public float CritDamage;
    public float BonusRangeRatio;
    public float BonusSpeed;
    public List<string> Items;
}
```

攻击逻辑: `FindTarget()` 遍历 `Monster` 集合，用距离平方比较 + `pathProgress` 最大的前进最远的怪。

溅射: 水塔击中后在半径 `splash` 内找最多 `splashMax` 个目标，溅射伤害 = 50% 主伤害。

减速: 冰塔击中后调用 `Monster.ApplySlow(rate, duration)`。

### Monster

```csharp
public class Monster {
    public float Hp, MaxHp, Resist, Speed;
    public bool IsElite;
    public bool Alive;
    public Vector2 Position;
    public int WpIndex;
    public float PathProgress;
    public string Element;           // null | "fire" | "water" | "ice"
    public float ElementTimer;
    public float FrozenTimer;
    public float SlowTimer, SlowRate;
    public Tower KillerTower;
}
```

移动: 沿 `waypoints[]` 循环折线移动。冻结时速度为 0。

元素附着: `TakeDamage` 中判定 `existingElement + incomingElement`，查 `ELEMENT_REACTIONS` 表得到倍率/冻结效果。

增加防御: `resist = MIN(0.85, 0.45 * (1 - e^(-wave/50)) + (精英?0.10:0))`。

### ElementSystem

```csharp
public static class ElementSystem {
    private static readonly Dictionary<(string, string), (float Mul, string Label, bool Freeze)> Reactions = new() {
        { ("fire", "water"), (2.0f, "蒸发", false) },
        { ("water", "fire"), (2.0f, "蒸发", false) },
        { ("water", "ice"),  (1.5f, "冻结", true)  },
        { ("ice", "water"),  (1.5f, "冻结", true)  },
        { ("fire", "ice"),   (1.5f, "融化", false)  },
        { ("ice", "fire"),   (1.5f, "融化", false)  },
    };
}
```

## 性能目标

| 指标 | 原版 (Canvas 2D) | 新版 (MonoGame) |
|---|---|---|
| 渲染方式 | CPU 即时模式绘制 | GPU 纹理批处理 |
| FPS (Wave 100, 200怪) | 30-45 | 60 稳定 |
| 内存分配 | 每帧数组 GC 压力 | 对象池 + RemoveAll |
| 粒子上限 | 200 | 500+ (GPU 批量) |

## 文件存档

- 路径: `%AppData%/ElementalLoopTD/save.json`
- 格式: JSON 序列化 (gold, wave, towers 数组)
- 时机: 每次关键操作后防抖 2s 写入
- 恢复: 启动时检查存档 → 弹窗询问恢复/新游戏

## 实现步骤

1. 创建项目 & 安装 MonoGame NuGet
2. 实现 Config.cs (常量迁移)
3. 实现 TextureGenerator.cs (程序化生成所有纹理)
4. 实现 SafeMath.cs / Extensions.cs
5. 实现 Towers / Monsters / Projectiles / Particles
6. 实现 ElementSystem
7. 实现 WaveManager (波次生成 & 出怪)
8. 实现 GameManager (核心状态 + 逻辑)
9. 实现 PathRenderer / GrassRenderer (离屏缓存)
10. 实现 UI 层 (HUD / InfoPanel / BuildBar / Overlays)
11. 实现 SaveManager
12. 组装 Game1.cs (Update/Draw 循环)
13. 调试 & 性能调优

## 与原版的差异

| 方面 | 原版 | 新版 |
|---|---|---|
| 运行环境 | 浏览器 | Windows 桌面 |
| 渲染 | Canvas 2D (CPU) | SpriteBatch (GPU) |
| UI | HTML + CSS + DOM | SpriteBatch 绘制的纹理/字体 |
| 输入 | DOM 事件 | MonoGame Mouse/Keyboard API |
| 存档 | localStorage | 本地 JSON 文件 |
| 字体 | 系统字体 | SpriteFont / BitmapFont |
| 音频 | 无 | MonoGame SoundEffect (可选添加) |
| 触摸支持 | 有 (响应式) | 无 (桌面专版) |