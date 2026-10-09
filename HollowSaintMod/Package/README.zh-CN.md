<p align="center"><a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.md">English</a> | <b>简体中文</b> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.ru.md">Русский</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.pt-BR.md">Português (BR)</a></p>

> 本页由机器翻译，欢迎指正：[提交翻译修正](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)。

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/banner.jpg" alt="空洞圣者，Risk of Rain 2 的风暴幸存者" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Thunderstore 版本"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Thunderstore 下载量"></a>
  <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT 许可证"></a>
</p>

<h3 align="center">一尊只回应风暴的裂痕圣像。</h3>

<p align="center">让闪电连锁穿过整群敌人，用一支闪电之矛钉住最大的威胁，并把每一次电殛都存成一层静电充能。<br>然后花掉这笔储备：用一道雷击搭上你的下一支矛，或者用一次带着爆发开场的充能凝视。</p>

<p align="center"><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><b>安装</b></a> · <a href="#13-版本新内容"><b>1.3 版本新内容</b></a> · <a href="#技能"><b>技能</b></a> · <a href="#风暴如何运作"><b>风暴</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/issues"><b>反馈</b></a></p>

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze-hero.webp" alt="空洞凝视：充能盘旋汇入冠冕，随后光束以一次爆发开场" width="100%"></p>

## 1.3 版本新内容

**已适配 2026 年 10 月的游戏更新。**请在模组管理器中同时更新核心依赖（BepInExPack、R2API、HookGenPatcher）和 Hollow Saint。

新增次要技能 **空洞电球** 和第三个特殊技能 **雷云**。电球无需充能：短按保留储存的充能，按住超过 0.5 秒后逐个聚集充能来增强电球。雷云同样可免费施放：轻点得到一朵小雷云，按住则注入充能。提前松开保留剩余充能，通用技能可取消蓄力并衔接电弧步。

无充能电球仍有 0.6 米直径、297% 单次命中伤害和三次命中，并有更明显的身体及手臂电流注入。开路期间电球在头顶聚集并投出，主要技能仍可攻击。**开路**现在需要按住特殊技能注入至少一层充能；1/3/5 层充能使范围脉冲密度达到 1/1.5/2 倍，电弧也更多。额外脉冲不会加快静电生成，半径、持续时间及单次伤害保持原值。它的冠冕现在是一条闭路：见下方的风暴循环说明。

- **空洞电球：**双手聚集大电球并向前投出。免费轻点每次命中伤害 297%，共 3 次命中；每层聚集的充能增加伤害（5 层时每次命中最高 657%）并多一次命中（最多 8 次），直径 0.6–1 米。射程 70 米，弹跳距离随充能为 18–36 米；优先新目标，同一敌人每次重复命中保留上次伤害的 75%。周围没有其他敌人时，电球会附着在目标上，在其身上打完剩余命中；耗尽时在小范围内爆开（无充能 3 米，每层充能 +0.8 米），造成命中伤害的 60%。每次命中都会预充静电。开路期间在头顶聚集并投出。
- **雷云：**持续存在的风暴，充能为空时也可免费施放：轻点得到一朵小雷云，按住则注入充能。它迅速升起，无充能时持续 3 秒，每层充能 +1 秒（5 层时 8 秒）。每 0.75 秒对其下方半径内（无充能 12 米，1 层 16 米，5 层 30 米）的每个敌人落下一道闪电，无充能造成 81% 伤害，5 层时每次最高 162%，并使目标触电、预充静电。瞄准范围 80 米，也可放在空地上。

电球冷却 7 秒，雷云 12 秒，均从施放结束后开始。原有风暴之矛、凝视与开路仍可选择。配置菜单可调整新技能，游戏说明随配置更新。多人联机和实体手柄验收仍待完成。

**雷云与弹跳优化：**雷云现在是持续存在的风暴，持续期间（3–8秒）每0.75秒落雷一次，每个目标伴随分叉回击闪光。电球弹跳距离随聚集充能增加：无充能/一层18米，三层27米，五层36米。

> **抢先体验：**空洞圣者仍在调整，平衡会改，偶尔也会有错误。你的反馈会直接决定下一次补丁。游戏内的技能说明始终显示你当前设置下的数值。

**前期伤害：**弧光矢单次直接伤害由108%提高至144%，射速及触发系数不变。只迁移精确的旧默认数值，自定义伤害保留。

**附着电球：**周围没有其他敌人时，空洞电球会附着在目标上，在其身上打完剩余命中；耗尽时在小范围内爆开（无充能3米，每层充能+0.8米），造成命中伤害的60%。优先新敌人的规则不变。

**闪电球：**蓄力及飞行时球面持续闪烁电弧；命中带有分叉闪电、火花、冲击环及电击音效，随皮肤变色。

**风暴循环：**每个消耗静电充能的技能现在都会为命中的目标预充静电（空洞电球、雷云、凝视的爆发与涌流、雷击的溅射），最高95%但绝不会充满，雷云还会使目标触电。收尾技能（弧光矢、风暴矛、凝视光束和开路）会把已预充的敌人推入电殛，并将充能回收入储备。风暴矛对已预充的敌人积累双倍静电，满储备现在只会用于完全蓄满的投掷。开路是一条**闭路**：12米内的电殛会返还你注入的充能，关闭时仍留在冠冕中的充能会化为冠冕新星爆发而出（12米，每层剩余充能150%伤害，并为命中的目标预充静电）。轻量的收入限制（每秒2层充能）防止后期大群敌人撑爆储备。聚集充能现在每0.25秒一层。

## 技能

| | 技能 | 栏位 | 简述 |
|:-:|---|---|---|
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-discharge.png" width="40" alt=""> | **应许之祷** | 被动 | 命中积累静电；电殛储存静电充能。 |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_bolt.png" width="40" alt=""> | **弧光矢** | 主要技能 | 快速的闪电矢，在敌群中连锁。 |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-conduit_spear.png" width="40" alt=""> | **风暴矛** | 次要技能 | 按住，投出，钉入，爆发。 |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_step.png" width="40" alt=""> | **弧光步** | 通用技能 | 两次闪现，地面或空中皆可。 |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-gaze.png" width="40" alt=""> | **空洞凝视** | 特殊技能 | 蓄能，悬浮，在他们之中烧出一条线。 |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-open_circuit.png" width="40" alt=""> | **开路** | 特殊技能，变体 | 一顶边战斗边打击的冠冕。 |
| | **空洞电球** | 次要技能变体 | 免费投出，可用充能增强；优先新目标，周围无其他敌人时附着在目标上。 |
| | **雷云** | 第三个特殊技能 | 持续存在的免费风暴，持续电击其下的所有敌人。 |

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-discharge.png" width="40" alt=""> 应许之祷 <sub>被动</sub></h3>

你的命中会在敌人身上积累**静电**。静电满时**电殛**：敌人触电，电弧跳向邻近的敌人。每次电殛都会储存一层**静电充能**（最多五层）。充能满时，你的下一次风暴矛投掷会化为**雷击**，凝视则把充能花在爆发和涌流上。充能不会自行消散。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm.webp" alt="电殛填满储备，随后是一道雷击" width="70%"></p>

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_bolt.png" width="40" alt=""> 弧光矢 <sub>主要技能</sub></h3>

射出一道闪电矢，最多再连锁至三名敌人。它是你稳定的压制手段，也是把静电散布到整群敌人身上最快的方法。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-bolt.webp" alt="弧光矢在敌群中连锁" width="70%"></p>

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-conduit_spear.png" width="40" alt=""> 风暴矛 <sub>次要技能</sub></h3>

按住凝聚出一支闪电之矛，松开投出。它会钉在命中的目标上，然后在周围爆发成一座闪电穹顶，范围随蓄力增大。蓄满力时还会召来一道落雷；静电储备满时，完全蓄满的投掷会化为雷击，为周围的敌群预充静电。矛的命中对已预充的敌人积累双倍静电，因此它是用来收尾你的特殊技能所铺垫目标的次要技能。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/stormspear.webp" alt="蓄满力的风暴矛钉入目标并爆发" width="70%"></p>

### 空洞电球 — 次要技能变体

短按并松开：无需充能，0.6米电球，每次命中伤害297%，共三次敌人命中。按住超过0.5秒逐个聚集储存的充能；每层充能增加伤害（5层时每次命中最高657%）并多一次命中（最多8次），直径最大1米。射程70米、弹跳距离随充能从18米增至36米。重复命中同一敌人保留上次伤害的75%，新敌人受到完整首次伤害。瞄准辅助优先准星并遵守墙体阻挡。每次命中都会预充静电，因此免费轻点让它成为为你的特殊技能和弧光矢铺垫的次要技能。

优先新敌人；周围没有其他敌人时，电球会附着在目标上，在其身上打完剩余命中，让免费电球也能持续打击近处的单一首领，而不消耗你为特殊技能攒下的充能。耗尽时在小范围内爆开（无充能3米，每层充能+0.8米），造成命中伤害的60%。通用技能取消聚集并归还充能与技能次数。开路期间电球在头顶聚集和投出，主要技能保持可用。施放结束后冷却7秒。

### 雷云 — 第三个特殊技能

充能为空时也可免费施放：轻点释放一朵小雷云，或按住特殊技能向冠冕注入充能，松开在瞄准区域释放。冠冕迅速升起，风暴无充能时持续3秒，每层充能+1秒（5层时8秒）。每0.75秒对其下方半径内（无充能12米，1层16米，5层30米）的每个敌人落下一道闪电，无充能造成81%伤害，5层时每次最高162%，并使目标触电、预充静电。瞄准范围80米，也可放在空地上；通用技能取消聚集。风暴结束后冷却12秒。

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_step.png" width="40" alt=""> 弧光步 <sub>通用技能</sub></h3>

向任意方向闪现一小段距离，空中也可以。向上看即可攀升，从闪现中跳出可以保住冲势。两次充能：一次用来找角度，一次用来脱身。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-step.webp" alt="弧光步：向左、向右、向上" width="70%"></p>

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-gaze.png" width="40" alt=""> 空洞凝视 <sub>特殊技能</sub></h3>

按住，把你的静电充能汇入冠冕，然后升空，引导一道持续七秒的穿透光束。光束开启时，会把你汇集的一切在一次爆发中打出（每层充能400%）。光束燃烧期间，按住并松开主要技能发射涌流，每次涌流都带一道锁定8米内最近敌人的闪电（每层充能100%）。蓄能和引导期间你受到的伤害更低。轻点特殊技能可跳过蓄能；特殊技能或界面取消可提前结束光束，通用技能则直接退出并接上你的弧光步。开场爆发、涌流和锁定闪电会为命中的目标预充静电，光束随后将其收尾。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze.webp" alt="空洞凝视以一次涌流扫过敌群" width="70%"></p>

<h3><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-open_circuit.png" width="40" alt=""> 开路 <sub>特殊技能，变体</sub></h3>

按住特殊技能向冠冕注入至少一层静电充能，松开后开启冠冕，持续十秒攻击周围敌人。更多充能增加脉冲密度。风暴矛蓄力加快，满静电储备使其落雷升级。空洞电球在头顶聚集和投出，主要技能保持可用；敌人在范围内停留三秒会额外受到一次电击。**闭路：**12米内每次电殛返还一层注入的充能；冠冕关闭时仍留在其中的充能会化为一次12米的新星从冠冕爆发而出，每层剩余充能造成150%伤害并为命中的目标预充静电，新星内无敌人则返还你的储备。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/crown.webp" alt="开路打击一圈敌人" width="70%"></p>

## 风暴如何运作

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm-loop.png" alt="风暴循环：收尾、电殛、储备、消耗、预充" width="100%"></p>

空洞圣者依靠一个循环运转：**收尾技能**赚取静电充能，**消耗技能**花费它们，而消耗技能命中的一切都会被**预充**，等待下一次收尾。

1. **静电。**可积累静电的命中会为敌人充能。更重的命中、暴击和高触发系数的物品充能更快。停止攻击后它会消退。
2. **电殛。**静电满时，敌人会受到震颤（首领除外）并进入**触电**，短时间内受到额外伤害。电弧会跳向附近的敌人，并为它们充能。每次电殛储存一层**静电充能**，最多五层。
3. **消耗与预充。**每个消耗充能的技能同时会为命中的目标预充，使其静电最高达95%。它自己永远不会把静电条充满，所以消耗技能无法自己回本。
4. **收尾。**弧光矢、风暴矛、凝视光束和开路的脉冲会把预充的敌人推过临界点。预充的敌群一两下就会电殛，你的储备随之补满。

| 角色 | 技能 | 在循环中 |
|---|---|---|
| 收尾 | 弧光矢、风暴矛、凝视光束、开路脉冲 | 积累静电、触发电殛、回收充能。风暴矛对已预充的敌人积累双倍静电。 |
| 消耗 | 空洞电球（按住）、雷云、凝视爆发与涌流、满储备雷击 | 把充能转化为伤害，并让目标保持预充。雷云还会使目标触电。 |
| 免费预充 | 空洞电球（轻点）、雷云（免费） | 二者都不消耗任何东西就能预充：启动循环最简单的方式。 |
| 回收 | 开路 | 注入的充能会从12米内的电殛中返还；剩余的在关闭时化为冠冕新星爆发。 |

除非你为某个技能按住蓄力，否则没有任何东西会拿走你的充能：快速投矛和快速轻点电球都不会碰储备。轻量的收入限制（每秒2层充能，可调）防止后期的大群敌人撑爆储备。

## 流派搭配

次要技能决定你如何开启和收尾循环；特殊技能决定你如何消耗它。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm-builds.png" alt="六种搭配：风暴矛或空洞电球，配凝视、开路或雷云" width="100%"></p>

| 流派 | 配置 | 循环方式 | 注意 |
|---|---|---|---|
| **唤风者** | 风暴矛 + 凝视 | 弧光矢散布静电，储备随之充满。对敌群开启凝视：爆发和涌流负责预充，光束和你的矛负责收尾。 | 矛和凝视共用一个储备；只有完全蓄满的投掷才会消耗它。 |
| **持矛者** | 风暴矛 + 开路 | 冠冕之下风暴矛蓄力快得多。向预充的敌人投矛，同时脉冲处理其余敌人；附近每次电殛都会返还冠冕。 | 留在战斗12米内才能获得返还。 |
| **围城** | 风暴矛 + 雷云 | 在远处敌群上空落下一朵云，为其预充并使其触电，再用矛和弧光矢连锁从远处收尾。 | 免费雷云同样能预充；把充能留给大雷云或完全蓄满的矛。 |
| **先知** | 空洞电球 + 凝视 | 免费电球预充敌群，再由凝视爆发和光束兑现。 | 电球自己不会收尾；光束和弧光矢才会。 |
| **导体** | 空洞电球 + 开路 | 免费电球负责预充，向冠冕注入充能并在其中战斗。脉冲收尾预充的敌人，返还让储备保持充盈，剩余充能则化为冠冕新星爆发。最能自给自足的流派。 | 冠冕期间头顶电球让主要技能保持空闲。 |
| **暴风雨** | 空洞电球 + 雷云 | 一切都在预充：云和电球点亮敌群，全靠弧光矢连锁完成收尾。高风险，高回报。 | 免费雷云维持循环；充能雷云会迅速清空储备。 |

## 皮肤

六款皮肤，各自有不同的光环和闪电颜色。**猩红誓约**是精通皮肤：以空洞圣者在季风难度下通关或湮灭即可解锁。

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/skin-lineup.png" alt="六款空洞圣者皮肤：裂痕圣像、黑曜圣者、铜绿圣遗、日曜晚祷、暗影唱诗与猩红誓约" width="100%"></p>

## 安装

**模组管理器（推荐）：**用 [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) 或 Thunderstore Mod Manager 安装，依赖会一并装好。

**手动：**安装本页列出的依赖，然后把包内的 `plugins/HollowSaint` 文件夹复制到 `BepInEx/plugins/`。请让 `HollowSaint.dll`、`hollowsaintassets` 和 `HollowSaint.language` 保持在同一个文件夹里。

## 选项

每一项平衡数值都在 **设置 → 模组选项 → Hollow Saint**（随模组安装的 [Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/)）以及 `BepInEx/config/com.johnstonstu.hollowsaint.cfg` 中，另外还有表现开关：持矛的手、物品显示、手臂动作和受击手感。

## 语言

模组会跟随你在 Risk of Rain 2 中设置的语言。已包含简体中文、俄语和巴西葡萄牙语（机器翻译；欢迎通过[翻译问题](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)提交修正）。其他语言显示英文。模组选项菜单保持英文。

## 反馈与已知限制

欢迎在 [GitHub issues](https://github.com/johnstonstu/ror2-hollow-saint/issues) 提交漏洞和平衡意见。报告漏洞时，打开 **Verbose log**（模组选项 → 6. Misc），重现问题，并附上 `BepInEx/LogOutput.log`，写明关卡和你当时在做什么。

- **多人模式还没有经过真正的试玩。**技能由服务器裁定并联网同步，但难免有粗糙之处。所有玩家都应使用相同的版本和配置。
- **实体手柄的适配尚未验证。**提交输入相关的报告时，请注明你的手柄和输入设置。
- 物品显示借用了突击兵的位置，所以少数物品会略微偏位。

## JohnstonStu 的其他作品

<table>
  <tr>
    <td><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/ah64-icon.png" alt="AH64" width="96"></a></td>
    <td><b><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/">AH64</a></b>：阿帕奇武装直升机幸存者。它悬停、从不降落，配备机炮、九头蛇火箭、地狱火与长弓导弹，以及规避横滚。</td>
  </tr>
</table>

## 致谢与许可

- 由 JohnstonStu 制作：设计、代码、模型、动画和特效。
- 雷击音效基于 Pixabay 采样制作（Pixabay Content License）。其余音效为原创。
- 使用 BepInEx、R2API 与 Risk Of Options 构建。

[MIT](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE) © 2026 JohnstonStu.
