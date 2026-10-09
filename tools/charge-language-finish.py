"""Apply the reviewed 1.3 controls and overhead interaction text in all locales."""
import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'HollowSaintMod/Language/HollowSaint.language'
data = json.loads(path.read_text(encoding='utf-8-sig'))
additions = {
    'strings': (' Hollowed Orb gathers and launches above your head.',
                ' Hold Secondary, then release. Range {orbRange}m; bounce reach {orbBounceRange}m. Open Circuit casts it overhead. Utility cancels; unused charges are kept.',
                ' Hold Special, then release; Utility cancels. Unused charges are kept. One rolling sequence, then the cloud fades.'),
    'zh-CN': (' 空洞电球会在头顶聚集并投出。',
              ' 按住次要技能，松开投出。射程{orbRange}米，弹跳距离{orbBounceRange}米。开路期间从头顶施放。通用技能取消；未使用的充能保留。',
              ' 按住特殊技能，松开施放；通用技能取消。未使用的充能保留。雷击依次落下，随后雷云消散。'),
    'ru': (' Полая сфера собирается и запускается над головой.',
           ' Удерживайте вторичный навык и отпустите для броска. Дальность {orbRange}м, радиус отскока {orbBounceRange}м. При Размыкании запускается над головой. Вспомогательный навык отменяет зарядку; неиспользованные заряды сохраняются.',
           ' Удерживайте особый навык и отпустите; вспомогательный навык отменяет зарядку. Неиспользованные заряды сохраняются. Одна серия ударов, затем туча исчезает.'),
    'pt-BR': (' O Orbe Oco se reúne e é lançado acima da cabeça.',
              ' Segure a Secundária e solte para lançar. Alcance {orbRange}m; saltos de até {orbBounceRange}m. Circuito Aberto o lança acima da cabeça. A Utilidade cancela; cargas não usadas são mantidas.',
              ' Segure a Especial e solte; a Utilidade cancela. Cargas não usadas são mantidas. Uma sequência de raios, depois a nuvem desaparece.'),
}
for locale, values in additions.items():
    for token, addition in zip(('HS_SKILL_CIRCUIT_DESC', 'HS_SKILL_HOLLOWED_ORB_DESC', 'HS_SKILL_THUNDERCLOUD_DESC'), values):
        if addition not in data[locale][token]:
            data[locale][token] += addition
resource_text = {
    'strings': ' Hold Hollowed Orb or Thundercloud to gather stored charges one at a time; release to spend only what you gathered.',
    'zh-CN': ' 按住空洞电球或雷云，逐个聚集储存的充能；松开时只消耗已聚集的充能。',
    'ru': ' Удерживайте Полую сферу или Грозовую тучу, чтобы собрать запасённые заряды по одному; отпускание расходует только собранные заряды.',
    'pt-BR': ' Segure Orbe Oco ou Nuvem Trovejante para reunir cargas armazenadas uma a uma; soltar gasta apenas as cargas reunidas.',
}
for locale, addition in resource_text.items():
    for token in ('HS_DESCRIPTION', 'HS_DESCRIPTION_RELEASE', 'HS_PASSIVE_STORM_DESC', 'HS_PASSIVE_STORM_DESC_RELEASE'):
        if addition not in data[locale][token]:
            data[locale][token] += addition
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
