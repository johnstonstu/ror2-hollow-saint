<p align="center"><a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.md">English</a> | <b>简体中文</b> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.ru.md">Русский</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.pt-BR.md">Português (BR)</a></p>

> 本页由机器翻译，欢迎指正：[提交翻译修正](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)。

<p align="center">
  <img src="docs/media/banner.jpg" alt="空洞圣者，Risk of Rain 2 的风暴幸存者" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Thunderstore 版本"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT 许可证"></a>
</p>

为 Risk of Rain 2 制作的原创幸存者，围绕连锁闪电：在敌人之间跳跃的电矢，会钉住并爆开的闪电之矛，以及用雷击回应你的命中的风暴被动。

## 语言

空洞圣者跟随你在 Risk of Rain 2 中设置的语言。模组自带简体中文、俄文和巴西葡萄牙文；其他语言显示英文。模组选项菜单保持英文。这三种译文为机器翻译，欢迎在[翻译修正](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)中指正。见 [docs/TRANSLATING.md](docs/TRANSLATING.md)。

<p align="center"><img src="docs/media/gaze-hero.webp" alt="空洞凝视：圣者升起，以分叉的闪电光束扫过一群敌人" width="100%"></p>

<p align="center"><img src="docs/media/crown.webp" alt="开路：冠冕张开，并打击圣者周围的每一个敌人" width="100%"></p>

**玩家：** 完整介绍（每个技能的录像、风暴、皮肤、安装、选项）是 Thunderstore 上的 README：[HollowSaintMod/Package/README.zh-CN.md](HollowSaintMod/Package/README.zh-CN.md)。更新说明在 [CHANGELOG.md](HollowSaintMod/Package/CHANGELOG.md)。

| 槽位 | 技能 |
|---|---|
| 被动 | **应许之祷**：命中积蓄蓄电；蓄电满时电殛；每 5 次电殛召下一次雷击 |
| 主技能 | **弧光矢**：100% 的电矢，最多连锁到另外 3 名敌人 |
| 副技能 | **风暴矛**：按住蓄力，400% 到 1600%；钉住目标，然后对周围爆开 |
| 效用 | **弧光步**：两层充能，可向任意方向闪现，空中也可以 |
| 特殊 | **空洞凝视**：升起并射出持续 4 秒的分叉光束 |
| 特殊（变体） | **开路**：持续 10 秒的冠冕，打击 8 米内的一切 |

![五套皮肤](docs/media/skin-lineup.png)

**JohnstonStu 的其他作品：** [AH64](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/)，阿帕奇武装直升机幸存者。

## 仓库

| 路径 | 内容 |
|---|---|
| `HollowSaintMod/` | BepInEx 插件（C#，netstandard2.1）。`Language/HollowSaint.language` 是游戏内文字。`Package/` 放 Thunderstore 清单、README、更新日志和图标 |
| `HollowSaintUnityProject/` | 构建 `hollowsaintassets` 资源包的 Unity 2021.3.33f1 工程（当前世代：`GameFoundation11`–`15`，片段来自 `GameFoundation10r1`） |
| `art/audio/` | Wwise 工程和生成的 `HollowSaint.bnk`（Pixabay 采样留在本地，见 `.gitignore`） |
| `tools/dev-profile/` | 把构建放进 `Hollow Saint Dev` 的 r2modman 配置，以及脚本化的自动试玩 |
| `tools/release/` | 打包、干净配置的安装测试、README 录像 |
| `tools/tests/` | 表现、技能数学和语言文件的离线检查（`Check-Language.ps1`） |
| `docs/` | 设计与架构文档；`docs/media/` 是 README 媒体，`docs/dev/` 是试玩记录和待办 |

更早的模型世代、概念图和 Blender 源文件不在这个仓库里，以便克隆保持小巧。

## 构建与测试

Risk Of Options 不在 NuGet 上。构建会依次查找 `HollowSaintMod/lib/` 里的 `RiskOfOptions.dll`（被 git 忽略），以及 `Hollow Saint Dev` 的 r2modman 配置；也可以传入 `-p:RiskOfOptionsDll=<path>`。

```
dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore
powershell -ExecutionPolicy Bypass -File tools\dev-profile\Stage-Build.ps1 -SkipBuild
```

然后从 r2modman 启动 `Hollow Saint Dev` 配置。`Stage-Build.ps1` 会把 `HollowSaint.language` 复制到插件文件夹、放在 DLL 旁边，并运行 `Check-Access.ps1`：如果 DLL 通过公开化引用碰到了游戏的私有成员，部署就会失败。

```
powershell -ExecutionPolicy Bypass -File tools\tests\Check-Language.ps1
```

脚本化试玩（主持一场单人游戏，按固定技能脚本操作，把截图和记录写到 `artifacts/<name>`）：

```
powershell -ExecutionPolicy Bypass -File tools\dev-profile\Run-Autopilot.ps1 -Name autopilot01
```

先设置 `HS_SEGMENTS` 才能跑其中一段脚本（`items`、`storm`、`gaze`、`polish`、`showcase` 等）。只有启动器设置了 `HS_AUTOPILOT` 时自动试玩才会运行；玩家看不到它。

## 发布

1. 同时提高 `Plugin.Version` 和 `Package/manifest.json`，并写一条 `CHANGELOG.md`。
2. `tools\release\Make-Package.ps1` 用 Release DLL 和已试玩的资源包生成 `artifacts\release\JohnstonStu-Hollow_Saint-<version>.zip`。
3. `tools\release\New-CleanProfile.ps1 -Package artifacts\release\JohnstonStu-Hollow_Saint-<version>` 会建一个只有声明依赖、没有配置的 `Hollow Saint Clean` 配置（`-NoRiskOfOptions` 用来测软依赖路径）；玩一次，以便发现缺失的依赖或默认配置问题。
4. README 录像：`tools\release\Record-Showcase.ps1 -Name showcaseNN` 只录游戏窗口里的展示脚本，然后 `tools\release\Make-ReadmeMedia.ps1 -Name showcaseNN` 把动态 WebP 和皮肤一览写到 `docs/media`（展示会藏起 HUD，并从正面拍摄皮肤和主视觉）。Thunderstore 的 README 从 GitHub 的 `main` 加载这些文件，所以上传前要先推送。

## 许可

代码与原创美术：[MIT](LICENSE)。`HollowSaint.bnk` 里的音效由 Pixabay 采样制作（Pixabay Content License）；采样本身不在这个仓库里。

## 日志

正常一局只会在 BepInEx 日志里写一行（「Hollow Saint <版本> loaded.」），外加真正的警告和错误。配置 `6. Misc` > `Verbose log` 会为报告问题重新打开加载诊断；`Event log` 会记下每种玩法事件的前几次出现。
