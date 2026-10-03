<p align="center"><a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.md">English</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.zh-CN.md">简体中文</a> | <b>Русский</b> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/README.pt-BR.md">Português (BR)</a></p>

> Этот текст переведён машиной. Поправки приветствуются: [сообщение о переводе](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md).

<p align="center">
  <img src="docs/media/banner.jpg" alt="Полый Святой, выживший бури для Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Версия на Thunderstore"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="Лицензия MIT"></a>
</p>

Оригинальный выживший для Risk of Rain 2, построенный вокруг цепной молнии: стрелы прыгают между врагами, копьё молнии вонзается и взрывается, а пассив бури отвечает на попадания Грозовым ударом.

> **Ранний доступ:** Полый Святой ещё настраивается, поэтому ждите изменений баланса и редких ошибок. Ваши отзывы напрямую определяют следующий патч баланса.
>
> **[Сообщить об ошибке или о балансе](https://github.com/johnstonstu/ror2-hollow-saint/issues)**

## Языки

Полый Святой следует языку, выбранному в Risk of Rain 2. С модом идут упрощённый китайский, русский и бразильский португальский; на любом другом языке текст английский. Меню параметров мода остаётся на английском. Эти три перевода сделаны машиной; поправки можно прислать [сообщением о переводе](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md). См. [docs/TRANSLATING.md](docs/TRANSLATING.md).

<p align="center"><img src="docs/media/gaze-hero.webp" alt="Взор Полого: Святой поднимается и ведёт ветвящийся луч молнии по стае" width="100%"></p>

<p align="center"><img src="docs/media/crown.webp" alt="Размыкание: корона раскрывается и бьёт каждого врага вокруг Святого" width="100%"></p>

**Игрокам:** полное описание (каждый навык с записью, буря, облики, установка, параметры) — это README на Thunderstore: [HollowSaintMod/Package/README.ru.md](HollowSaintMod/Package/README.ru.md). Заметки к версиям — в [CHANGELOG.md](HollowSaintMod/Package/CHANGELOG.md).

| Слот | Навык |
|---|---|
| Пассив | **Услышанная молитва**: попадания копят Статику; полная Статика вызывает Электрошок; каждые 5 Электрошоков зовут Грозовой удар |
| Основной | **Дуговая стрела**: 100% и перескок ещё максимум на 3 врагов |
| Вторичный | **Громовое копьё**: удержание заряжает, 400–1600%; вонзается и взрывается вокруг цели |
| Умение | **Дуговой шаг**: два заряда, рывок в любую сторону, даже в воздухе |
| Особый | **Взор Полого**: подъём и ветвящийся луч на 4 с |
| Особый (вариант) | **Размыкание**: корона на 10 с бьёт всё в пределах 8 м |

![Пять обликов](docs/media/skin-lineup.png)

**Ещё от JohnstonStu:** [AH64](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/), выживший — ударный вертолёт Apache.

## Репозиторий

| Путь | Содержимое |
|---|---|
| `HollowSaintMod/` | Плагин BepInEx (C#, netstandard2.1). `Language/HollowSaint.language` — текст в игре. В `Package/` лежат манифест Thunderstore, README, список изменений и значок |
| `HollowSaintUnityProject/` | Проект Unity 2021.3.33f1, который собирает бандл `hollowsaintassets` (текущее поколение: `GameFoundation11`–`15`, клипы из `GameFoundation10r1`) |
| `art/audio/` | Проект Wwise и собранный `HollowSaint.bnk` (сэмплы Pixabay остаются локально, см. `.gitignore`) |
| `tools/dev-profile/` | Выкладка сборки в профиль r2modman `Hollow Saint Dev` и скриптовый автопилот |
| `tools/release/` | Упаковка, проверка чистого профиля, запись для README |
| `tools/tests/` | Офлайн-проверки вида, математики набора и языкового файла (`Check-Language.ps1`) |
| `docs/` | Документы по замыслу и устройству; `docs/media/` — медиа README, `docs/dev/` — журнал проб и список дел |

Старые поколения модели, концепты и исходники Blender в этот репозиторий не входят, чтобы клон оставался небольшим.

## Сборка и проверка

Risk Of Options нет на NuGet. Сборка ищет `RiskOfOptions.dll` в `HollowSaintMod/lib/` (папка в gitignore), затем в профиле r2modman `Hollow Saint Dev`; либо передайте `-p:RiskOfOptionsDll=<path>`.

```
dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore
powershell -ExecutionPolicy Bypass -File tools\dev-profile\Stage-Build.ps1 -SkipBuild
```

Затем запустите профиль `Hollow Saint Dev` из r2modman. `Stage-Build.ps1` копирует `HollowSaint.language` в папку плагина рядом с DLL и запускает `Check-Access.ps1`: выкладка срывается, если DLL трогает закрытый член игры через публичную ссылку.

```
powershell -ExecutionPolicy Bypass -File tools\tests\Check-Language.ps1
```

Скриптовая проба (одиночный забег, фиксированный сценарий навыков, снимки и след в `artifacts/<name>`):

```
powershell -ExecutionPolicy Bypass -File tools\dev-profile\Run-Autopilot.ps1 -Name autopilot01
```

Сначала задайте `HS_SEGMENTS`, чтобы прогнать один сценарий (`items`, `storm`, `gaze`, `polish`, `showcase`, …). Автопилот работает только если лаунчер выставил `HS_AUTOPILOT`; игроки его не видят.

## Выпуск

1. Поднимите вместе `Plugin.Version` и `Package/manifest.json` и добавьте запись в `CHANGELOG.md`.
2. `tools\release\Make-Package.ps1` собирает `artifacts\release\JohnstonStu-Hollow_Saint-<version>.zip` из Release DLL и проверенного бандла.
3. `tools\release\New-CleanProfile.ps1 -Package artifacts\release\JohnstonStu-Hollow_Saint-<version>` собирает профиль `Hollow Saint Clean` только с объявленными зависимостями и без конфига (`-NoRiskOfOptions` проверяет мягкую зависимость); пройдите его один раз, чтобы поймать недостающие зависимости или проблемы настроек по умолчанию.
4. Запись для README: `tools\release\Record-Showcase.ps1 -Name showcaseNN` пишет только окно игры, затем `tools\release\Make-ReadmeMedia.ps1 -Name showcaseNN` кладёт анимированные WebP и ряд обликов в `docs/media` (показ прячет HUD и снимает облики и главный кадр спереди). README на Thunderstore берёт их из `main` на GitHub, поэтому перед загрузкой нужен push.

## Лицензия

Код и оригинальная графика: [MIT](LICENSE). Звуки в `HollowSaint.bnk` собраны из сэмплов Pixabay (Pixabay Content License); сами сэмплы в репозиторий не входят.

## Журнал

Обычный заход пишет в журнал BepInEx одну строку («Hollow Saint <версия> loaded.») плюс настоящие предупреждения и ошибки. Параметр `6. Misc` > `Verbose log` снова включает диагностику загрузки для отчётов; `Event log` добавляет первые появления каждого игрового события.
