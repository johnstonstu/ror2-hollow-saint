"""Update optional Orb fuel and overhead controls after the first 1.3 playtest."""
import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'HollowSaintMod/Language/HollowSaint.language'
data = json.loads(path.read_text(encoding='utf-8-sig'))
orb = {
    'strings': 'Release Secondary to throw a two-handed orb for <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} damage</style> with <style=cIsUtility>{orbMinHits}-{orbMaxHits} total hits</style>. No Static Charge required. Short casts preserve charges; hold past 0.5s to gather stored charges one at a time for more power. Prefers enemies near your aim, then new bounce targets before reduced-damage revisits. Range {orbRange}m; bounce reach {orbBounceRange}m. Open Circuit gathers and launches it overhead while Primary remains available. Utility cancels; unused charges are kept.',
    'zh-CN': '松开次要技能，双手投出电球，造成<style=cIsDamage>{orbMinDamage}-{orbMaxDamage}伤害</style>，最多命中<style=cIsUtility>{orbMinHits}-{orbMaxHits}次</style>。无需静电充能。短按保留充能；按住超过0.5秒后逐个聚集储存的充能来增强电球。优先瞄准附近的敌人，再优先弹向新目标；重复命中伤害降低。射程{orbRange}米，弹跳距离{orbBounceRange}米。开路期间在头顶聚集并投出，仍可使用主要技能。通用技能取消；未使用的充能保留。',
    'ru': 'Отпустите вторичный навык, чтобы бросить сферу двумя руками: <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} урона</style>, <style=cIsUtility>{orbMinHits}-{orbMaxHits} попаданий</style>. Заряд Статики не требуется. Короткий бросок сохраняет заряды; удержание дольше 0.5с собирает их по одному для усиления. Предпочитает врагов около прицела, затем новые цели отскока; повторный урон снижается. Дальность {orbRange}м, отскок {orbBounceRange}м. При Размыкании сфера собирается и запускается над головой, основной навык доступен. Вспомогательный навык отменяет; неиспользованные заряды сохраняются.',
    'pt-BR': 'Solte a Secundária para lançar um orbe com as duas mãos: <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} de dano</style> e <style=cIsUtility>{orbMinHits}-{orbMaxHits} acertos totais</style>. Não exige Carga Estática. Lançamentos curtos preservam cargas; segure por mais de 0.5s para reunir cargas guardadas uma a uma e fortalecer o orbe. Prefere inimigos perto da mira, depois novos alvos; repetições causam menos dano. Alcance {orbRange}m; saltos de {orbBounceRange}m. Circuito Aberto reúne e lança o orbe acima da cabeça, mantendo a Primária disponível. A Utilidade cancela; cargas não usadas são mantidas.',
}
old_resource = {
    'strings': ' Hold Hollowed Orb or Thundercloud to gather stored charges one at a time; release to spend only what you gathered.',
    'zh-CN': ' 按住空洞电球或雷云，逐个聚集储存的充能；松开时只消耗已聚集的充能。',
    'ru': ' Удерживайте Полую сферу или Грозовую тучу, чтобы собрать запасённые заряды по одному; отпускание расходует только собранные заряды.',
    'pt-BR': ' Segure Orbe Oco ou Nuvem Trovejante para reunir cargas armazenadas uma a uma; soltar gasta apenas as cargas reunidas.',
}
resource = {
    'strings': ' Hollowed Orb works without charges: short casts preserve your bank; longer holds gather charges for more power. Thundercloud requires a charge and gathers stored charges one at a time. Release spends only gathered charges.',
    'zh-CN': ' 空洞电球无需充能：短按保留储存的充能，长按聚集充能来增强电球。雷云需要充能并逐个聚集储存的充能。松开时只消耗已聚集的充能。',
    'ru': ' Полая сфера работает без зарядов: короткий бросок сохраняет запас, долгое удержание собирает заряды для усиления. Грозовая туча требует заряд и собирает их по одному. Отпускание расходует только собранные заряды.',
    'pt-BR': ' Orbe Oco funciona sem cargas: lançamentos curtos preservam o estoque; segurar por mais tempo reúne cargas para fortalecer. Nuvem Trovejante exige uma carga e reúne as guardadas uma a uma. Soltar gasta apenas as cargas reunidas.',
}
circuit = {
    'strings': ' Requires a Static Charge. Hold Special to feed stored charges into the crown, then release. More gathered charges increase area lightning: pulse interval {circuitMinInterval}-{circuitMaxInterval}s, with more crown arcs. Extra pulses do not accelerate Static generation. Utility cancels; unused charges are kept. Primary remains available while gathering the overhead Orb.',
    'zh-CN': ' 需要静电充能。按住特殊技能，将储存的充能注入冠冕后松开。聚集更多充能可增加范围内的闪电：脉冲间隔{circuitMinInterval}-{circuitMaxInterval}秒，并增加冠冕电弧。额外脉冲不会加快静电生成。通用技能取消；未使用的充能保留。在头顶聚集电球时仍可使用主要技能。',
    'ru': ' Требует заряд Статики. Удерживайте особый навык, чтобы подать заряды в корону, затем отпустите. Больше зарядов усиливают молнии в области: интервал импульсов {circuitMinInterval}-{circuitMaxInterval}с и больше дуг. Дополнительные импульсы не ускоряют накопление Статики. Вспомогательный навык отменяет; остаток зарядов сохраняется. Основной навык доступен при сборе сферы над головой.',
    'pt-BR': ' Exige uma Carga Estática. Segure a Especial para alimentar a coroa com cargas guardadas e solte. Mais cargas aumentam os raios na área: intervalo {circuitMinInterval}-{circuitMaxInterval}s e mais arcos da coroa. Pulsos extras não aceleram a geração de Estática. A Utilidade cancela; cargas não usadas são mantidas. A Primária continua disponível ao reunir o orbe acima da cabeça.',
}
for locale in orb:
    data[locale]['HS_SKILL_HOLLOWED_ORB_DESC'] = orb[locale]
    for token in ('HS_DESCRIPTION', 'HS_DESCRIPTION_RELEASE', 'HS_PASSIVE_STORM_DESC', 'HS_PASSIVE_STORM_DESC_RELEASE'):
        current = data[locale][token]
        if old_resource[locale] not in current and resource[locale] not in current:
            raise ValueError(f'{locale} {token}: expected resource sentence missing')
        data[locale][token] = current.replace(old_resource[locale], resource[locale])
    # Replace the prior overhead-Primary sentence when extending Circuit's rules.
    prior = {'strings': ' Primary remains available while gathering the overhead Orb.',
             'zh-CN': ' 在头顶聚集电球时仍可使用主要技能。',
             'ru': ' Основной навык доступен при сборе сферы над головой.',
             'pt-BR': ' A Primária continua disponível ao reunir o orbe acima da cabeça.'}[locale]
    if circuit[locale] not in data[locale]['HS_SKILL_CIRCUIT_DESC']:
        data[locale]['HS_SKILL_CIRCUIT_DESC'] = data[locale]['HS_SKILL_CIRCUIT_DESC'].removesuffix(prior)
        data[locale]['HS_SKILL_CIRCUIT_DESC'] += circuit[locale]
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
