<p align="center"><a href="README.md">English</a> | <b>简体中文</b> | <a href="README.ru.md">Русский</a> | <a href="README.pt-BR.md">Português (BR)</a></p>

> 本页由机器翻译，欢迎指正：[提交翻译修正](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)。

<p align="center">
  <img src="docs/media/banner.jpg" alt="空洞圣者，Risk of Rain 2 的风暴幸存者" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Thunderstore 版本"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Thunderstore 下载量"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT 许可证"></a>
</p>

<h3 align="center">一尊只回应风暴的裂痕圣像。</h3>

<p align="center">让闪电连锁穿过整群敌人，用一支闪电之矛钉住最大的威胁，并把每一次电殛都存成一层静电充能。<br>然后把储备花在空洞电球、雷云、凝视光束或开路冠冕上。</p>

<p align="center"><a href="#技能"><b>技能</b></a> · <a href="#风暴如何运作"><b>风暴</b></a> · <a href="#流派搭配"><b>流派</b></a> · <a href="#安装与选项"><b>安装</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/issues"><b>反馈</b></a> · <a href="HollowSaintMod/Package/CHANGELOG.md"><b>更新日志</b></a></p>

<p align="center"><img src="docs/media/thundercloud-return-13.webp" alt="Thundercloud 1.3: crown release and rolling lightning" width="100%"></p>

<p align="center"><img src="docs/media/hollowed-orb-13.webp" alt="Hollowed Orb 1.3" width="49%"> <img src="docs/media/circuit-orb-13.webp" alt="Open Circuit and Hollowed Orb 1.3" width="49%"></p>

> **抢先体验：**空洞圣者仍在调整，平衡会改，偶尔也会有错误。游戏内的技能说明始终显示你当前设置下的数值。

## 技能

| | 技能 | 栏位 | 操作 | 效果 | 关键数值 |
|:-:|---|---|---|---|---|
| <img src="docs/media/icon-discharge.png" width="40" alt=""> | **应许之祷** | 被动 | 自动 | 命中在敌人身上积累静电。静电满时电殛，电弧跳向邻近的敌人。每次电殛储存一层静电充能。 | 储备最多五层。充能不会自行消散。 |
| <img src="docs/media/icon-arc_bolt.png" width="40" alt=""> | **弧光矢** | 主要技能 | 主要技能 | 快速的闪电矢，在敌群中连锁。是你稳定的压制手段，也是散布静电最快的方法。 | 每次命中171%。每发最多命中4名敌人。每0.5秒一发。触发系数1.0。 |
| <img src="docs/media/icon-conduit_spear.png" width="40" alt=""> | **风暴矛** | 次要技能 | 按住蓄力，松开投出 | 钉在命中的目标上，然后爆发成一座闪电穹顶。对已预充的敌人积累双倍静电。 | 2秒蓄满。爆发范围3米至10米。冷却5秒。 |
| <img src="docs/media/icon-arc_step.png" width="40" alt=""> | **弧光步** | 通用技能 | 通用技能 | 向任意方向闪现一小段距离，空中也可以。向上看即可攀升，从闪现中跳出可以保住冲势。 | 2次充能。5秒恢复。 |
| <img src="docs/media/icon-gaze.png" width="40" alt=""> | **空洞凝视** | 特殊技能 | 按住汇集充能，松开开始。轻点可跳过蓄能。 | 升空，引导一道穿透光束。光束开启时，会把你汇集的一切在一次强力爆发中打出，随后对锁住的目标积累聚焦。 | 光束7秒，射程90米。开场爆发每层充能400%，宽6米，每层充能+2米（最宽20米）。聚焦三秒内最高+100%伤害。冷却12秒。 |
| <img src="docs/media/icon-open_circuit.png" width="40" alt=""> | **开路** | 特殊技能，变体 | 按住特殊技能注入至少一层充能，松开 | 一顶边战斗边打击周围敌人的冠冕。 | 持续10秒，半径8米。每0.5秒一次脉冲，伤害72%。1、3、5层充能使脉冲密度达到1、1.5、2倍。冷却8秒，从冠冕关闭后开始。 |
| | **空洞电球** | 次要技能变体 | 轻点，或按住超过0.5秒聚集充能 | 大型闪电球，在敌群中来回弹跳。可免费施放。 | 免费：每次命中378%，3次命中，0.9米。5层充能：738%，8次命中，1.5米。射程70米，弹跳距离18米至36米。冷却6秒。 |
| | **雷云** | 第三个特殊技能 | 轻点，或按住特殊技能注入充能，在目标上方松开 | 持续存在的风暴，电击其下的所有敌人。可免费施放。 | 每0.75秒落雷，持续4秒（5层充能时9秒）。每次149%至261%。半径12米至30米。瞄准范围80米。冷却10秒。 |

**按住技能键：**凝视或雷云聚集充能时，持续按住主要技能可继续发射弧光矢。这些技能只聚集开始时已有的充能；期间新获得的充能会留作下次使用。风暴矛蓄力时会暂停弧光矢，投出后只要仍按住主要技能就会恢复射击。

### 技能说明

- **风暴矛：**蓄满力时还会召来一道落雷。静电储备满时，完全蓄满的投掷会化为**雷击**，为周围的敌群预充静电。快速投矛从不碰储备。
- **空洞电球：**优先新敌人，其次才是重复命中，同一敌人每次重复命中保留上次伤害的75%。瞄准辅助优先准星并遵守墙体阻挡。每个备用弹夹再多一次命中。周围没有其他敌人时，电球会附着在目标上，在其身上打完剩余命中；耗尽时在小范围内爆开（3米，每层充能+0.8米），造成命中伤害的60%。每次命中都会预充静电。通用技能取消聚集并归还充能。开路期间电球在头顶聚集和投出，主要技能保持可用。
- **雷云：**半径无充能12米，1层16米，5层30米。也可放在空地上，飞行敌人和高台上的敌人同样会被击中。落雷会使目标触电并预充静电，攻击速度越高落雷越快（最多两倍频率）。通用技能取消聚集。降雨期间再次按下特殊技能可提前结束风暴，并按剩余时间返还最多一半冷却。冷却从风暴结束后开始。
- **空洞凝视：**蓄能和引导期间你受到的伤害更低，并且不会被击退。偏离目标半秒以内聚焦仍会保持，之后逐渐消退。特殊技能或界面取消可提前结束光束，通用技能则直接退出并接上你的弧光步。开场爆发会为命中的目标预充静电，光束随后将其收尾。
- **开路：**开启冠冕至少需要一层充能。额外脉冲不会加快静电生成。敌人在范围内停留三秒会额外受到一次电击。**闭路：**12米内每次电殛返还一层注入的充能；冠冕关闭时仍留在其中的充能会化为一次12米的新星从冠冕爆发而出，每层剩余充能造成150%伤害并为命中的目标预充静电，新星内无敌人则返还你的储备。

## 风暴如何运作

<p align="center"><img src="docs/media/storm-loop.png" alt="风暴循环：收尾、电殛、储备、消耗、预充" width="100%"></p>

**收尾技能**赚取静电充能，**消耗技能**花费它们，而消耗技能命中的一切都会被**预充**，等待下一次收尾。

1. **静电。**可积累静电的命中会为敌人充能。更重的命中、暴击和高触发系数的物品充能更快。停止攻击后它会消退。
2. **电殛。**静电满时，敌人会受到震颤（首领除外）并进入**触电**，短时间内受到额外伤害。电弧会为附近的敌人充能。每次电殛储存一层**静电充能**，最多五层。
3. **消耗与预充。**每个消耗充能的技能同时会为命中的目标预充，使其静电最高达95%，但绝不会充满，所以消耗技能无法自己回本。
4. **收尾。**弧光矢、风暴矛、凝视光束和开路的脉冲会把预充的敌人推过临界点。预充的敌群一两下就会电殛，你的储备随之补满。

- **收尾：**弧光矢、风暴矛、凝视光束、开路脉冲。
- **消耗：**空洞电球（按住）、雷云、凝视开场爆发、满储备雷击。凝视、空洞电球、雷云和开路花费你聚集的数量。
- **免费预充：**空洞电球（轻点）和免费雷云不消耗任何东西就能预充：启动循环最简单的方式。
- **回收：**开路的充能会从12米内的电殛中返还。

除非你为某个技能按住蓄力，否则没有任何东西会拿走你的充能。轻量的收入限制（每秒2层充能，可调）防止后期的大群敌人撑爆储备。

## 流派搭配

次要技能决定你如何开启和收尾循环；特殊技能决定你如何消耗它。

<p align="center"><img src="docs/media/storm-builds.png" alt="六种搭配：风暴矛或空洞电球，配凝视、开路或雷云" width="100%"></p>

| 流派 | 配置 | 玩法 | 注意 |
|---|---|---|---|
| **唤风者** | 风暴矛 + 凝视 | 弧光矢让储备充满。凝视的开场爆发负责预充，聚焦的光束和你的矛负责收尾。 | 矛和凝视共用一个储备；只有完全蓄满的投掷才会消耗它。 |
| **持矛者** | 风暴矛 + 开路 | 冠冕之下风暴矛蓄力快得多。向预充的敌人投矛，同时脉冲处理其余敌人。 | 留在战斗12米内才能获得返还。 |
| **围城** | 风暴矛 + 雷云 | 在远处敌群上空落下一朵云，为其预充并使其触电，再用矛和弧光矢连锁从远处收尾。 | 免费雷云同样能预充；把充能留给大雷云或完全蓄满的矛。 |
| **先知** | 空洞电球 + 凝视 | 免费电球预充敌群，再由凝视的开场爆发和光束兑现。 | 电球自己不会收尾；光束和弧光矢才会。 |
| **导体** | 空洞电球 + 开路 | 免费电球负责预充，向冠冕注入充能并在其中战斗。脉冲收尾，返还让储备保持充盈。最能自给自足的流派。 | 冠冕期间头顶电球让主要技能保持空闲。 |
| **暴风雨** | 空洞电球 + 雷云 | 云和电球点亮一切，全靠弧光矢连锁完成收尾。高风险，高回报。 | 免费雷云维持循环；充能雷云会迅速清空储备。 |

## 皮肤

六款皮肤，各自有不同的光环和闪电颜色。**猩红誓约**是精通皮肤：以空洞圣者在季风难度下通关或湮灭即可解锁。

<p align="center"><img src="docs/media/skin-lineup.png" alt="六款空洞圣者皮肤：裂痕圣像、黑曜圣者、铜绿圣遗、日曜晚祷、暗影唱诗与猩红誓约" width="100%"></p>

## 安装与选项

- **模组管理器（推荐）：**用 [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) 或 Thunderstore Mod Manager 安装，依赖会一并装好。游戏更新后，请同时更新 BepInExPack、R2API、HookGenPatcher 和 Hollow Saint。
- **手动：**安装本页列出的依赖，然后把包内的 `plugins/HollowSaint` 文件夹复制到 `BepInEx/plugins/`。请让 `HollowSaint.dll`、`hollowsaintassets` 和 `HollowSaint.language` 保持在同一个文件夹里。
- **选项：**每一项平衡数值都在 **设置 → 模组选项 → Hollow Saint**（随模组安装的 [Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/)）以及 `BepInEx/config/com.johnstonstu.hollowsaint.cfg` 中，另外还有表现开关：持矛的手、物品显示、手臂动作和受击手感。
- **语言：**模组会跟随你在 Risk of Rain 2 中设置的语言。已包含简体中文、俄语和巴西葡萄牙语（机器翻译；欢迎通过[翻译问题](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)提交修正）。其他语言显示英文。模组选项菜单保持英文。

## 反馈与已知限制

欢迎在 [GitHub issues](https://github.com/johnstonstu/ror2-hollow-saint/issues) 提交漏洞和平衡意见。报告漏洞时，打开 **Verbose log**（模组选项 → 6. Misc），重现问题，并附上 `BepInEx/LogOutput.log`，写明关卡和你当时在做什么。

- **多人模式还没有经过真正的试玩。**技能由服务器裁定并联网同步，但难免有粗糙之处。所有玩家都应使用相同的版本和配置。
- **实体手柄的适配尚未验证。**提交输入相关的报告时，请注明你的手柄和输入设置。
- 物品显示借用了突击兵的位置，所以少数物品会略微偏位。

## JohnstonStu 的其他作品

<table>
  <tr>
    <td><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="docs/media/ah64-icon.png" alt="AH64" width="96"></a></td>
    <td><b><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/">AH64</a></b>：阿帕奇武装直升机幸存者。它悬停、从不降落，配备机炮、九头蛇火箭、地狱火与长弓导弹，以及规避横滚。</td>
  </tr>
</table>

## 致谢、许可与更新日志

- 由 JohnstonStu 制作：设计、代码、模型、动画和特效。
- 雷击音效基于 Pixabay 采样制作（Pixabay Content License）。其余音效为原创。
- 使用 BepInEx、R2API 与 Risk Of Options 构建。
- [完整更新日志](HollowSaintMod/Package/CHANGELOG.md)。

[MIT](LICENSE) © 2026 JohnstonStu.

## 面向开发者

完整的开发文档目前只有英文版：见[英文 README 的“Building from source”部分](README.md#building-from-source)。
