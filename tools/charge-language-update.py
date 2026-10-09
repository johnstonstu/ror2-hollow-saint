"""One-time additive authoring utility; preserves existing language-file lines."""
import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "HollowSaintMod/Language/HollowSaint.language"
source = path.read_text(encoding="utf-8-sig")
document = json.loads(source)
entries = {
    "strings": ["Thundercloud", "Requires a Static Charge. Hold to gather charges, then release a growing storm over an aimed area. Strike each enemy within <style=cIsUtility>{cloudMinRadius}-{cloudMaxRadius}m</style> for <style=cIsDamage>{cloudMinDamage}-{cloudMaxDamage} damage</style>.",
                "Hollowed Orb", "Requires a Static Charge. Gather charges between both hands, then throw a growing orb for <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} damage</style> with <style=cIsUtility>{orbMinHits}-{orbMaxHits} total hits</style>. Prefers new enemies, then revisits at reduced damage."],
    "zh-CN": ["雷云", "需要静电充能。按住以聚集充能，松开后在瞄准区域召唤随充能扩大的雷云。对<style=cIsUtility>{cloudMinRadius}-{cloudMaxRadius}米</style>内每个敌人造成<style=cIsDamage>{cloudMinDamage}-{cloudMaxDamage}伤害</style>。",
              "空洞电球", "需要静电充能。双手聚集充能，投出随充能扩大的电球，造成<style=cIsDamage>{orbMinDamage}-{orbMaxDamage}伤害</style>，最多命中<style=cIsUtility>{orbMinHits}-{orbMaxHits}次</style>。优先弹向新敌人；重复命中的伤害降低。"],
    "ru": ["Грозовая туча", "Требуется заряд статики. Удерживайте, чтобы собрать заряды, затем отпустите и вызовите растущую тучу над выбранной областью. Каждый враг в радиусе <style=cIsUtility>{cloudMinRadius}-{cloudMaxRadius}м</style> получает <style=cIsDamage>{cloudMinDamage}-{cloudMaxDamage} урона</style>.",
           "Полая сфера", "Требуется заряд статики. Соберите заряды между ладонями и бросьте растущую сферу: <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} урона</style>, до <style=cIsUtility>{orbMinHits}-{orbMaxHits} попаданий</style>. Сначала новые враги, затем повторные цели с уменьшенным уроном."],
    "pt-BR": ["Nuvem Trovejante", "Requer uma Carga Estática. Segure para reunir cargas e solte uma tempestade crescente na área visada. Atinge cada inimigo em <style=cIsUtility>{cloudMinRadius}-{cloudMaxRadius}m</style> por <style=cIsDamage>{cloudMinDamage}-{cloudMaxDamage} de dano</style>.",
              "Orbe Oco", "Requer uma Carga Estática. Reúna cargas entre as mãos e lance um orbe crescente por <style=cIsDamage>{orbMinDamage}-{orbMaxDamage} de dano</style>, com <style=cIsUtility>{orbMinHits}-{orbMaxHits} acertos totais</style>. Prioriza novos inimigos, depois repete alvos com dano reduzido."],
}
tokens = ["HS_SKILL_THUNDERCLOUD_NAME", "HS_SKILL_THUNDERCLOUD_DESC", "HS_SKILL_HOLLOWED_ORB_NAME", "HS_SKILL_HOLLOWED_ORB_DESC"]
for lang in entries:
    if any(token in document[lang] for token in tokens):
        raise SystemExit("New tokens already present; review them before editing.")
lines = source.splitlines(keepends=True)
lang = None
result = []
for line in lines:
    stripped = line.strip()
    for candidate in entries:
        if stripped == json.dumps(candidate) + ": {":
            lang = candidate
    result.append(line)
    if stripped.startswith('"HS_SKILL_GAZE_NAME":'):
        assert lang in entries
        for token, value in zip(tokens, entries[lang]):
            result.append("    " + json.dumps(token) + ": " + json.dumps(value, ensure_ascii=False) + ",\n")
updated = "".join(result)
parsed = json.loads(updated)
for lang in entries:
    assert all(parsed[lang][key] == document[lang][key] for key in document[lang])
path.write_text(updated, encoding="utf-8")
print("Added four skill tokens in all four languages; existing values preserved.")
