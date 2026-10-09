"""Apply reviewed 1.3 Static exceptions and harmless player-relay copy in all languages."""
import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'HollowSaintMod/Language/HollowSaint.language'
doc = json.loads(path.read_text(encoding='utf-8-sig'))
copy = {
    'strings': (
        'every hit charges the enemy.', 'eligible hits charge the enemy.',
        ' Direct Hollowed Orb, Thundercloud, Gaze opening blasts and surges do not build Static.',
        ' With one or two reachable enemies left, it can bounce harmlessly through you within 8m to revisit enemies; player contacts add no enemy hits.'),
    'zh-CN': (
        '每次命中都会为敌人充能。', '可积累静电的命中会为敌人充能。',
        ' 空洞电球、雷云、凝视开场爆发与涌流的直接命中不会积累静电。',
        ' 仅剩一至两个可弹向的敌人时，电球可经8米内的你无伤返回敌人；接触玩家不增加敌人命中次数。'),
    'ru': (
        'каждое попадание заряжает врага.', 'подходящие попадания заряжают врага.',
        ' Прямые попадания Полой сферы, Грозовой тучи, начального взрыва и волн Взгляда не копят Статику.',
        ' Когда доступны один или два врага, сфера может безвредно отскочить от вас в пределах 8м и вернуться к врагам; касание игрока не добавляет попаданий по врагам.'),
    'pt-BR': (
        'cada acerto carrega o inimigo.', 'acertos elegíveis carregam o inimigo.',
        ' Acertos diretos do Orbe Oco, Nuvem Trovejante, explosão inicial e surtos do Olhar não acumulam Estática.',
        ' Com um ou dois inimigos ao alcance, o orbe pode passar por você sem causar dano a até 8m e voltar aos inimigos; contatos com o jogador não adicionam acertos inimigos.')
}
for language, (old, new, exception, relay) in copy.items():
    tokens = doc[language]
    for key in ('HS_KEYWORD_STATIC', 'HS_KEYWORD_STORM', 'HS_KEYWORD_STORM_RELEASE'):
        tokens[key] = tokens[key].replace(old, new)
        tokens[key] = tokens[key].replace(exception, '')
        head, tail = tokens[key].rsplit('</style>', 1)
        tokens[key] = head + exception + '</style>' + tail
    for key in ('HS_PASSIVE_STORM_DESC', 'HS_PASSIVE_STORM_DESC_RELEASE'):
        if exception.strip() not in tokens[key]: tokens[key] += exception
    if relay.strip() not in tokens['HS_SKILL_HOLLOWED_ORB_DESC']: tokens['HS_SKILL_HOLLOWED_ORB_DESC'] += relay
path.write_text(json.dumps(doc, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
