"""Synchronize reviewed 1.3 player copy; keep new media local until publication is approved."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
media_url = 'https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/'
data = {
 '': {
  'heading': "What's new in 1.3", 'old': "What's new in 1.2", 'kit': 'The kit',
  'early': '> **Early access:', 'utility': '<h3><img src="'+media_url+'icon-arc_step.png',
  'balance': '**Early-game pressure:** Arc Bolt now deals 144% damage per direct hit (was 108%), at the same fire rate and proc coefficients. Only the exact previous default migrates; custom damage settings stay yours.',
  'relay': '**Player bounce:** with one or two reachable enemies remaining, Hollowed Orb can bounce harmlessly through you within 8 m of the last enemy. Fresh enemies still come first. Player contacts add no damage, procs, Static or enemy hits; the cast keeps its original finite budget.',
  'fx': '**Lightning ball:** animated crackling bolts wrap the orb while gathering and flying. Impacts add branching lightning, sparks, shock rings and electrical hit sounds in your skin’s colors.',
  'exceptions': 'Direct Hollowed Orb, Thundercloud, Gaze opening blasts and surges do not build Static. Use Arc Bolt, Stormspear, the Gaze core and Open Circuit to build fuel, then spend it deliberately.',
  'sections': '''### Hollowed Orb — alternate Secondary

Tap and release for a free two-handed lightning ball: 157.5% first-hit damage, two enemy hits and a substantial 0.6 m ball. Holding past 0.5 seconds gathers one stored charge at a time; one/three/five charges give 225/360/495% first-hit damage and 3/5/7 enemy hits. Diameter grows to 1 m, launch range is 70 m and bounce reach is 18 m. Each repeat on the same enemy keeps 65% of that enemy’s previous hit damage; a new enemy receives the full first-hit value. Aim assistance favors the crosshair and respects walls.

When one or two reachable enemies remain, stay within 8 m for a harmless player relay back to an enemy. This makes the free orb useful against a nearby lone boss, without spending the charges you are saving for an ultimate. Utility cancels gathering and returns its fuel and stock. During Open Circuit, the orb gathers and launches overhead, leaving Primary available. Base recharge: 7 seconds after the cast ends.

### Thundercloud — third Special

Requires at least one stored charge. Hold Special to feed charges into the crown, then release over your aimed group. One/three/five charges give a 16/23/30 m radius and 270/495/720% damage per enemy. The crown rises into a broad thundercloud, lightning rolls across its enemies once, then the cloud fades. Aim range is 80 m; an empty area refunds fuel and stock. Utility cancels gathering. Base recharge: 12 seconds after the cast ends.

''',
  'orbrow': '| | **Hollowed Orb** | Alt. secondary | Free throw; optional charges; fresh-first bounces and a harmless nearby player relay. |',
  'cloudrow': '| | **Thundercloud** | Third special | Send the charged crown into a broad rolling storm. |',
  'oldstatic': 'Every hit charges the enemy.', 'newstatic': 'Eligible hits charge the enemy.',
  'introold': 'Then spend the bank: on a Thunderbolt that rides your next spear, or on a charged Gaze that opens with a blast.',
  'intronew': 'Throw a crackling Hollowed Orb, feed the crown for Gaze or Open Circuit, or send a Thundercloud across a broad aimed area.',
 },
 'zh-CN': {
  'heading':'1.3 版本新内容', 'old':'1.2 版本新内容', 'kit':'技能', 'early':'> **抢先体验：',
  'utility':'<h3><img src="'+media_url+'icon-arc_step.png',
  'balance':'**前期伤害：**弧光矢单次直接伤害由108%提高至144%，射速及触发系数不变。只迁移精确的旧默认数值，自定义伤害保留。',
  'relay':'**玩家弹跳：**仅剩一至两个可弹向的敌人时，空洞电球可经最后敌人8米内的你无伤返回敌人。优先新敌人；接触玩家不造成伤害、不触发物品、不产生静电，也不增加敌人命中次数。总命中上限不变。',
  'fx':'**闪电球：**蓄力及飞行时球面持续闪烁电弧；命中带有分叉闪电、火花、冲击环及电击音效，随皮肤变色。',
  'exceptions':'空洞电球、雷云、凝视开场爆发与涌流的直接命中不会积累静电。弧光矢、风暴之矛、凝视核心与开路用于产生充能，再由其他技能消耗。',
  'sections':'''### 空洞电球 — 次要技能变体

短按并松开：无需充能，0.6米电球，157.5%首次伤害，共两次敌人命中。按住超过0.5秒逐个聚集储存的充能；1/3/5层为225/360/495%首次伤害，3/5/7次敌人命中，直径最大1米。射程70米、弹跳18米。重复命中同一敌人保留上次伤害的65%，新敌人受到完整首次伤害。瞄准辅助优先准星并遵守墙体阻挡。

仅剩一至两个可弹向的敌人时，可经8米内的你无伤弹回敌人，让免费电球也能反复打击近处的单一首领。通用技能取消聚集并归还充能与技能次数。开路期间电球在头顶聚集和投出，主要技能保持可用。施放结束后冷却7秒。

### 雷云 — 第三个特殊技能

至少需要一层充能。按住特殊技能向冠冕注入充能，松开在瞄准区域释放雷云。1/3/5层对应16/23/30米半径及每个敌人270/495/720%伤害。冠冕升起化云，依次攻击每个敌人一次后消散。瞄准范围80米，空区域归还充能与技能次数；通用技能取消聚集。施放结束后冷却12秒。

''',
  'orbrow':'| | **空洞电球** | 次要技能变体 | 免费投出，可用充能增强；优先新目标，可经附近玩家无伤弹回。 |',
  'cloudrow':'| | **雷云** | 第三个特殊技能 | 冠冕升起，对大范围瞄准区域依次落雷。 |',
  'oldstatic':'每次命中都会为敌人充能。','newstatic':'可积累静电的命中会为敌人充能。'
 },
 'ru': {
  'heading':'Что нового в 1.3','old':'Что нового в 1.2','kit':'Набор','early':'> **Ранний доступ:',
  'utility':'<h3><img src="'+media_url+'icon-arc_step.png',
  'balance':'**Начальный урон:** прямое попадание Дуговой стрелы теперь наносит144% вместо108%; частота и коэффициенты срабатывания предметов прежние. Миграция меняет только точное старое значение по умолчанию, сохраняя ваши настройки.',
  'relay':'**Отскок от игрока:** когда доступны один или два врага, Полая сфера может безвредно отскочить от вас в пределах8м от последнего врага и вернуться. Новые враги приоритетны. Касание игрока не наносит урон, не активирует предметы, не копит Статику и не добавляет попаданий по врагам.',
  'fx':'**Шар молнии:** разветвлённые дуги обвивают сферу при сборе и полёте. Попадания создают искры, ударные кольца и электрические звуки в цветах облика.',
  'exceptions':'Прямые попадания Полой сферы, Грозовой тучи, начального взрыва и волн Взгляда не копят Статику. Дуговая стрела, Грозовое копьё, ядро Взгляда и Размыкание создают запас для других навыков.',
  'sections':'''### Полая сфера — альтернативный вторичный навык

Короткий бросок бесплатен: диаметр0,6м,157,5% урона при первом попадании, два попадания по врагам. Удержание дольше0,5с собирает сохранённые заряды по одному;1/3/5 зарядов дают225/360/495% начального урона,3/5/7 попаданий и диаметр до1м. Дальность70м, отскок18м. Каждое повторное попадание по тому же врагу сохраняет65% его предыдущего урона; новая цель получает полный начальный урон. Прицел помогает выбрать цель и учитывает стены.

При одном или двух доступных врагах сфера может безвредно вернуться через вас в пределах8м и снова ударить врага, включая одинокого близкого босса. Вспомогательный навык отменяет сбор и возвращает заряды и использование. При Размыкании бросок идёт над головой, основной навык остаётся доступным. Перезарядка7с после завершения броска.

### Грозовая туча — третий особый навык

Требует хотя бы один заряд. Удерживайте особый навык для сбора зарядов в корону, затем отпустите над выбранной областью.1/3/5 зарядов дают радиус16/23/30м и270/495/720% урона каждому врагу. Корона поднимается в тучу, молнии один раз последовательно поражают врагов и исчезают. Дальность прицеливания80м; пустая область возвращает заряды и использование. Вспомогательный навык отменяет сбор. Перезарядка12с после завершения.

''',
  'orbrow':'| | **Полая сфера** | Альт. вторичный | Бесплатный бросок, усиление зарядами и безвредный отскок от игрока. |',
  'cloudrow':'| | **Грозовая туча** | Третий особый | Широкая прицельная область последовательных молний. |',
  'oldstatic':'Каждое попадание заряжает врага.','newstatic':'Подходящие попадания заряжают врага.'
 },
 'pt-BR': {
  'heading':'Novidades da 1.3','old':'Novidades da 1.2','kit':'O kit','early':'> **Acesso antecipado:',
  'utility':'<h3><img src="'+media_url+'icon-arc_step.png',
  'balance':'**Dano inicial:** a Seta em Arco causa144% por acerto direto, antes108%, com a mesma cadência e coeficientes de proc. Apenas o valor anterior exato é migrado; ajustes pessoais são preservados.',
  'relay':'**Salto pelo jogador:** com um ou dois inimigos alcançáveis, o Orbe Oco pode passar por você sem dano a até8m do último inimigo e voltar a atacar. Novos inimigos têm prioridade. O contato não causa dano, procs, Estática ou acertos extras; o limite original permanece.',
  'fx':'**Bola de relâmpagos:** arcos se movem pela superfície ao carregar e voar. Impactos ganham raios ramificados, faíscas, anéis de choque e sons elétricos nas cores da aparência.',
  'exceptions':'Acertos diretos do Orbe Oco, Nuvem Trovejante, explosão inicial e surtos do Olhar não acumulam Estática. Seta em Arco, Lança da Tempestade, núcleo do Olhar e Circuito Aberto geram o estoque para outros poderes.',
  'sections':'''### Orbe Oco — Secundária alternativa

Toque e solte para lançar sem cargas: bola de0,6m,157,5% de dano inicial e dois acertos inimigos. Segurar por mais de0,5s reúne cargas uma a uma;1/3/5 cargas dão225/360/495% de dano inicial e3/5/7 acertos, com diâmetro até1m. Alcance70m, saltos18m. Cada repetição no mesmo inimigo mantém65% do dano anterior naquele alvo; um alvo novo recebe o dano inicial completo. A assistência favorece a mira e respeita paredes.

Com um ou dois inimigos alcançáveis, fique a até8m para o orbe passar por você sem dano e voltar ao inimigo, inclusive um chefe sozinho. A Utilidade cancela a reunião e devolve cargas e uso. Circuito Aberto lança acima da cabeça, mantendo a Primária disponível. Recarga7s após o fim do lançamento.

### Nuvem Trovejante — terceira Especial

Exige ao menos uma carga. Segure a Especial para alimentar a coroa e solte sobre a área visada.1/3/5 cargas dão raio16/23/30m e270/495/720% de dano por inimigo. A coroa sobe, vira uma nuvem e ataca cada inimigo uma vez em sequência antes de sumir. Mira até80m; área vazia devolve cargas e uso. A Utilidade cancela a reunião. Recarga12s após o fim do lançamento.

''',
  'orbrow':'| | **Orbe Oco** | Secundária alternativa | Gratuito; cargas opcionais; novos alvos primeiro e salto inofensivo pelo jogador. |',
  'cloudrow':'| | **Nuvem Trovejante** | Terceira Especial | Coroa vira uma tempestade ampla na área visada. |',
  'oldstatic':'Cada acerto carrega o inimigo.','newstatic':'Acertos elegíveis carregam o inimigo.'
 }
}

for language, copy in data.items():
    filename = 'README' + ('.' + language if language else '') + '.md'
    package = root / 'HollowSaintMod/Package' / filename
    repo_readme = root / filename
    original = repo_readme.read_text(encoding='utf-8-sig')
    s = package.read_text(encoding='utf-8-sig')
    # Remove the now-redundant long 1.2 introduction; existing per-skill media remains.
    old_at = s.find('## ' + copy['old'])
    if old_at >= 0:
        end = s.index('\n> **', old_at)
        s = s[:old_at] + s[end + 1:]
    if copy['balance'] not in s:
        at = s.index('## ' + copy['kit'])
        s = s[:at] + '\n'.join((copy['balance'], '', copy['relay'], '', copy['fx'], '', copy['exceptions'], '')) + '\n' + s[at:]
    s = s.replace(copy['oldstatic'], copy['newstatic'])
    if language == '':
        s = s.replace(copy['introold'], copy['intronew'])
        s = s.replace('A fully charged crown spear calls a Thunderbolt.', 'A fully charged crown spear calls its ordinary lightning strike; a full Static bank upgrades it to a Thunderbolt.')
        s = s.replace('and a fully charged crown spear calls a Thunderbolt.', 'and a full Static bank upgrades its lightning strike to a Thunderbolt.')
        s = s.replace('Hollowed Orb and Thundercloud gather charges individually', 'Hollowed Orb, Thundercloud and Open Circuit gather charges individually')
    # Explicit standalone sections for the two new selections.
    if copy['sections'].splitlines()[0] not in s:
        at = s.index(copy['utility']); s = s[:at] + copy['sections'] + s[at:]
    table_at = s.index('## ' + copy['kit'])
    table_end = s.index('<h3>', table_at)
    table = s[table_at:table_end]
    for row, marker in ((copy['orbrow'], '**' + ('Hollowed Orb' if not language else {'zh-CN':'空洞电球','ru':'Полая сфера','pt-BR':'Orbe Oco'}[language]) + '**'),
                        (copy['cloudrow'], '**' + ('Thundercloud' if not language else {'zh-CN':'雷云','ru':'Грозовая туча','pt-BR':'Nuvem Trovejante'}[language]) + '**')):
        if marker not in table: table = table.rstrip() + '\n' + row + '\n\n'
    s = s[:table_at] + table + s[table_end:]
    package.write_text(s, encoding='utf-8')
    local = s.replace(media_url, 'docs/media/')
    local = local.replace('https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README', 'README')
    local = local.replace('https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/CHANGELOG.md', 'HollowSaintMod/Package/CHANGELOG.md')
    local = local.replace('https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE', 'LICENSE')
    at = local.index('## ' + copy['heading'])
    local = local[:at] + '<p align="center"><img src="docs/media/hollowed-orb-13.webp" alt="Hollowed Orb 1.3" width="49%"> <img src="docs/media/thundercloud-13.webp" alt="Thundercloud 1.3" width="49%"></p>\n\n' + local[at:]
    at = local.index(copy['sections'].splitlines()[0])
    local = local[:at] + '<p align="center"><img src="docs/media/circuit-orb-13.webp" alt="Open Circuit and overhead Hollowed Orb 1.3" width="70%"></p>\n\n' + local[at:]
    if language == '' and '## For developers' in original:
        local += '\n' + original[original.index('## For developers'):]
    elif language and original.rfind('\n## ') > original.rfind('© 2026'):
        local += original[original.rfind('\n## '):]
    repo_readme.write_text(local, encoding='utf-8')
print('Updated eight 1.3 README files; new footage remains repository-local.')
