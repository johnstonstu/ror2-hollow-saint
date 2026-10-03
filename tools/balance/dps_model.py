#!/usr/bin/env python3
"""Hollow Saint damage model (stdlib only).

Discrete-time simulation of the kit as coded in HollowSaintMod/FoundationKit:
  StormServer.cs        Static gain, Electrocute, pop, cascade, rate cap, decay
  ArcBoltChain.cs       primary hit + spread + chain hops (cumulative falloff)
  ConduitSpearAnchor.cs pulse (BlastAttack) and Arc Bolt spread
  ConductorMark.cs      mark x mult on Hollow Saint skill damage, Shocked x mult on all
  ThunderboltDriver.cs  charge 6, telegraph, strike + splash
  OpenCircuitPulseDriver.cs  8 s window, pulse every 0.5 s, proc 0 (Static weight)

Units: damage is reported as "% of the player's damage stat per second"
(coefficient x 100 / s), the same unit RoR2 skill descriptions use, so the
numbers compare directly with vanilla survivors regardless of level.

Usage:  python tools/balance/dps_model.py            (table)
        python tools/balance/dps_model.py --detail   (per-source breakdown)

Simplifications (deliberately generous to the Saint, i.e. the worst realistic
case for balance):
  * Pack = 6 enemies all inside 6 m of each other and inside the spear radius.
  * The player never misses, always focuses the front enemy, the spear is
    planted at all times after the first throw (recall gaps ignored).
  * Enemies never die in the "sustained" numbers (measures output, not kills);
    the "clr" figure uses real HP and is seconds to kill the whole pack.
  * Crit is an expectation (damage x(1+c), Static x(1+c*0.5)).
  * Stun has no damage effect; only bosses get Shocked (stun immune).

Vanilla baselines (sources fetched 2026-09-29; anything the wiki does not state
is from recollection of the game's EntityState baseDuration values and is
flagged UNSURE):
  Commando   https://riskofrain2.fandom.com/wiki/Commando
             Double Tap 100% per bullet (one bullet per click), Phase Round
             300% +40% per pierce, 3 s cooldown. Fire rate 0.2 s/shot: UNSURE.
  Artificer  https://riskofrain2.fandom.com/wiki/Artificer
             Flame Bolt 280% + 50% ignite over time, wiki lists 1.3 s per cast
             (1.25 used), Charged Nano-Bomb 400-2000%, 5 s cooldown, 14 m
             radius. Average charge 1200% assumed: UNSURE.
  Huntress   https://riskofrain2.fandom.com/wiki/Huntress
             Strafe 150% per 0.5 s, Laser Glaive 250% (+10% per bounce, up to
             6 bounces), 7 s cooldown.
Vanilla numbers assume no items or attack speed (early game), which is the
comparison that matters here.
"""
import argparse
from collections import deque

# --------------------------------------------------------------------------
# Tuning sets. CURRENT mirrors KitTuning before the v0.7 balance pass.
# --------------------------------------------------------------------------
CURRENT = dict(
    bolt_interval=0.5, bolt_coeff=1.0, chain_targets=4, chain_falloff=0.75, chain_proc=0.5,
    spear_coeff=4.5, spear_cd=5.0, spear_cast=0.6, mark_mult=1.5,
    pulse_interval=1.5, pulse_coeff=0.5, pulse_static_w=0.5,
    spread_targets=3, spread_coeff=0.6, spread_proc=0.5,
    oc_cd=12.0, oc_buff=8.0, oc_interval=0.5, oc_coeff=0.6, oc_w=0.3,
    static_thr=0.25, static_min=0.08, static_crit=1.5, static_mark=1.5,
    decay_delay=2.0, decay_rate=0.5,
    shocked_s=3.0, shocked_mult=1.2,
    pop_coeff=2.5, pop_targets=3, pop_proc=0.5, pop_static=0.4,
    immune_s=4.0, rate_cap=4, charge_max=6,
    tb_coeff=10.0, tb_splash=0.5, tb_cd=4.0, tb_telegraph=0.35, tb_flight=0.25,
)

PROPOSED = dict(CURRENT)
PROPOSED.update(
    # Mirrors KitTuning / KitConfig defaults after the v0.7 balance pass.
    pop_coeff=1.5,        # was 2.5
    pop_targets=2,        # was 3
    pop_static=0.15,      # was 0.4  (cascade)
    pop_proc=0.3,         # was 0.5
    spread_targets=2,     # was 3
    spread_coeff=0.35,    # was 0.6
    spread_proc=0.3,      # was 0.5
    static_mark=1.25,     # was 1.5
    shocked_mult=1.15,    # was 1.2
    static_min=0.06,      # was 0.08
)

# (label, player damage stat, attack speed, crit chance, pack enemy HP, boss HP)
STAGES = [
    ("early", 14.4, 1.0, 0.00, 170.0, 2500.0),    # level 3, no items, stage 1
    ("mid",   40.0, 1.8, 0.20, 480.0, 9000.0),    # ~level 10, a few items
    ("late", 120.0, 3.0, 0.40, 1100.0, 30000.0),  # ~level 25, ~3x attack speed
]

# Vanilla early, % of damage stat per second: (single target, pack of 6).
VANILLA = {
    # Double Tap 5 shots/s x 100% (UNSURE rate) + Phase Round 300% / 3 s; pack: pierces ~3 (300+420+540)
    "Commando M1+M2": (500 + 300 / 3.0, 500 + 1260 / 3.0),
    # Flame Bolt 280% x1.5 (ignite) / 1.25 s + Nano-Bomb 1200% avg / 5 s (hits all 6)
    "Artificer M1+M2": (280 * 1.5 / 1.25 + 1200 / 5.0, 280 * 1.5 / 1.25 + 6 * 1200 / 5.0),
    # Strafe 150% / 0.5 s + Laser Glaive 250% (x1.1 per bounce) / 7 s
    "Huntress M1+M2": (300 + 250 / 7.0, 300 + sum(250 * 1.1 ** k for k in range(6)) / 7.0),
}
VANILLA_M1 = {
    "Commando M1": (500, 500),
    "Artificer M1": (280 * 1.5 / 1.25, 280 * 1.5 / 1.25),
    "Huntress M1": (300, 300),
}


class Enemy:
    def __init__(self, hpmax, boss):
        self.hpmax = hpmax
        self.hp = hpmax
        self.boss = boss
        self.alive = True
        self.static = 0.0
        self.last = -99.0
        self.immune = -99.0
        self.shocked = -99.0


class Sim:
    def __init__(self, cfg, stage, n, boss, kit, immortal, T, dt=0.02):
        _, self.dmg, self.aspd, self.crit, pack_hp, boss_hp = stage
        self.c = cfg
        self.kit = kit
        self.immortal = immortal
        self.T = T
        self.dt = dt
        self.en = [Enemy(boss_hp if boss else pack_hp, boss) for _ in range(n)]
        self.center = n // 2
        self.now = 0.0
        self.mark_idx = None
        self.mark_until = -1.0
        self.charge = 0
        self.tb_ready = 0.0
        self.tb_pending = None
        self.electro_times = deque()
        self.depth = 0
        self.electro_count = 0
        self.strikes = 0
        self.by = {}
        self.total = 0.0
        self.spear_planted = False

    def alive(self):
        return [i for i, e in enumerate(self.en) if e.alive]

    def marked(self, i):
        return self.mark_idx == i and self.now < self.mark_until

    def nearest(self, origin, k, exclude=()):
        ids = [i for i in self.alive() if i not in exclude]
        ids.sort(key=lambda i: (abs(i - origin), i))
        return ids[:k]

    def deal(self, i, base, proc, src, critable, skill_src, static_hook):
        """One damage event on enemy i (base includes the damage stat)."""
        e = self.en[i]
        if not e.alive:
            return
        c = self.c
        mult = 1.0
        if skill_src and self.marked(i):
            mult *= c["mark_mult"]
        if self.now < e.shocked:
            mult *= c["shocked_mult"]
        dmg = base * mult * ((1 + self.crit) if critable else 1.0)
        dealt = dmg if self.immortal else min(dmg, e.hp)
        self.total += dealt
        self.by[src] = self.by.get(src, 0.0) + dealt
        if not self.immortal:
            e.hp -= dmg
            if e.hp <= 0:
                e.alive = False
                e.static = 0.0
                return
        if not static_hook:
            return
        p = proc
        if src == "pulse":
            p = c["pulse_static_w"]
        elif p <= 0:
            if src == "oc":
                p = c["oc_w"]
            else:
                return
        full = e.hpmax * max(0.01, c["static_thr"])
        gain = min(max(dmg / max(1.0, full), c["static_min"]), 1.0) * p
        if critable:
            gain *= 1 + self.crit * (c["static_crit"] - 1)
        if self.marked(i):
            gain *= c["static_mark"]
        self.add_static(i, gain)

    # ---- Storm (StormServer.cs)
    def add_static(self, i, amount):
        e = self.en[i]
        if not e.alive or amount <= 0 or self.now < e.immune:
            return
        e.static = min(1.0, e.static + amount)
        e.last = self.now
        if e.static >= 1.0:
            self.try_electrocute(i)

    def try_electrocute(self, i):
        while self.electro_times and self.now - self.electro_times[0] > 1.0:
            self.electro_times.popleft()
        if self.depth >= 3 or len(self.electro_times) >= max(1, self.c["rate_cap"]):
            self.en[i].static = 1.0
            return
        self.electrocute(i, pop=True)

    def electrocute(self, i, pop):
        c = self.c
        e = self.en[i]
        e.static = 0.0
        e.last = self.now
        e.immune = self.now + c["immune_s"]
        if pop:
            self.electro_times.append(self.now)
            self.electro_count += 1
            self.charge = min(c["charge_max"], self.charge + 1)
        if e.boss:
            e.shocked = self.now + c["shocked_s"]
        if pop:
            hit = self.nearest(i, c["pop_targets"], exclude=(i,))
            for j in hit:
                self.deal(j, c["pop_coeff"] * self.dmg, c["pop_proc"], "pop", False, False, False)
            self.depth += 1
            try:
                for j in hit:
                    self.add_static(j, c["pop_static"])
            finally:
                self.depth -= 1

    # ---- Skills
    def bolt(self):
        c = self.c
        al = self.alive()
        if not al:
            return
        p = al[0]
        base = c["bolt_coeff"] * self.dmg
        hit = {p}
        self.deal(p, base, 1.0, "bolt", True, True, True)
        if self.kit != "m1" and self.spear_planted and c["spread_targets"] > 0:
            for j in self.nearest(self.center, c["spread_targets"], exclude=hit):
                hit.add(j)
                self.deal(j, c["spread_coeff"] * self.dmg, c["spread_proc"], "spread", True, True, True)
        frm = p
        dmg = base
        for _ in range(c["chain_targets"] - 1):  # ArcBoltChain: max targets includes the primary
            nxt = self.nearest(frm, 1, exclude=hit)
            if not nxt:
                break
            j = nxt[0]
            hit.add(j)
            dmg *= c["chain_falloff"]
            self.deal(j, dmg, c["chain_proc"], "chain", True, True, True)
            frm = j

    def spear(self):
        c = self.c
        al = self.alive()
        if not al:
            return
        p = al[0]
        self.deal(p, c["spear_coeff"] * self.dmg, 1.0, "spear", True, True, True)
        self.mark_idx = p  # mark lands after the hit
        self.mark_until = self.now + 6.0
        self.spear_planted = True

    def pulse(self):
        for i in self.alive():
            self.deal(i, self.c["pulse_coeff"] * self.dmg, 0.3, "pulse", False, True, True)

    def oc_pulse(self):
        for i in self.nearest(self.center, 99):
            self.deal(i, self.c["oc_coeff"] * self.dmg, 0.0, "oc", False, True, True)

    def thunderbolt_tick(self):
        c = self.c
        if self.tb_pending is not None:
            when, tgt = self.tb_pending
            if self.now >= when:
                self.tb_pending = None
                base = c["tb_coeff"] * self.dmg
                self.deal(tgt, base, 1.0, "thunder", True, False, False)
                for j in self.nearest(tgt, 2, exclude=(tgt,)):  # ~2 neighbours inside 3 m
                    self.deal(j, base * c["tb_splash"], 0.5, "thunder", True, False, False)
                if self.en[tgt].alive:
                    self.electrocute(tgt, pop=False)
                self.strikes += 1
            return
        if self.charge >= c["charge_max"] and self.now >= self.tb_ready:
            al = self.alive()
            if not al:
                return
            tgt = max(al, key=lambda i: self.en[i].hp)
            self.charge = 0
            self.tb_ready = self.now + c["tb_cd"]
            self.tb_pending = (self.now + c["tb_telegraph"] + c["tb_flight"], tgt)

    def run(self):
        c = self.c
        interval = c["bolt_interval"] / self.aspd
        next_bolt = 0.0
        next_spear = 0.3 if self.kit != "m1" else 1e9
        next_pulse = 1e9
        oc_cast = 1.0 if self.kit == "full" else 1e9
        oc_start = oc_end = next_oc = 1e9
        clear = None
        for _ in range(int(self.T / self.dt)):
            t = self.now
            if t >= next_spear:
                self.spear()
                next_bolt = max(next_bolt, t) + c["spear_cast"] / self.aspd  # the cast interrupts bolts
                next_spear += c["spear_cd"]
                if next_pulse > 1e8:
                    next_pulse = t + c["pulse_interval"] * 0.5
            if t >= next_bolt:
                self.bolt()
                next_bolt += interval
            if t >= next_pulse and self.spear_planted:
                self.pulse()
                next_pulse += c["pulse_interval"]
            if t >= oc_cast:
                oc_start = t + 0.87
                oc_end = oc_start + c["oc_buff"]
                next_oc = oc_start
                oc_cast += c["oc_cd"]
            if oc_start <= t < oc_end and t >= next_oc:
                self.oc_pulse()
                next_oc += c["oc_interval"]
            for i, e in enumerate(self.en):  # Storm tick: retry held Electrocutes, decay
                if not e.alive:
                    continue
                if e.static >= 1.0 and t >= e.immune:
                    self.try_electrocute(i)
                    if e.static >= 1.0:
                        continue
                if e.static > 0 and t - e.last >= c["decay_delay"]:
                    e.static = max(0.0, e.static - c["decay_rate"] * self.dt)
            self.thunderbolt_tick()
            self.now += self.dt
            if not self.immortal and not self.alive():
                clear = self.now
                break
        elapsed = self.now
        return dict(
            dps=self.total / self.dmg * 100.0 / max(0.01, elapsed),
            clear=clear,
            electro=self.electro_count / max(0.01, elapsed),
            strikes=self.strikes,
            by={k: v / self.dmg * 100.0 / max(0.01, elapsed) for k, v in self.by.items()},
        )


def measure(cfg, stage, scenario, kit):
    if scenario == "single":
        return Sim(cfg, stage, 1, False, kit, True, 30.0).run()
    if scenario == "boss":
        return Sim(cfg, stage, 1, True, kit, True, 30.0).run()
    sus = Sim(cfg, stage, 6, False, kit, True, 20.0).run()
    real = Sim(cfg, stage, 6, False, kit, False, 60.0).run()
    sus["clear"] = real["clear"]
    return sus


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--detail", action="store_true")
    args = ap.parse_args()

    print("All values: %% of damage stat per second (coefficient x 100 / s). el = Electrocutes/s.")
    print("Hollow Saint kits: m1 = Arc Bolt, m1+m2 = + Conduit Spear, full = + Open Circuit.\n")
    print("Vanilla early baselines (single target / pack of 6):")
    for k, (s, p) in list(VANILLA_M1.items()) + list(VANILLA.items()):
        print("  %-16s single %5.0f   pack %5.0f" % (k, s, p))
    print()
    for kit in ("m1", "m1+m2", "full"):
        print("=== Hollow Saint kit: %s ===" % kit)
        print("%-6s %-7s | %-26s | %-26s | change" % ("stage", "target", "CURRENT", "PROPOSED"))
        for stage in STAGES:
            for sc in ("single", "pack", "boss"):
                a = measure(CURRENT, stage, sc, kit)
                b = measure(PROPOSED, stage, sc, kit)
                ca = cb = ""
                if sc == "pack":
                    ca = " clr %4.1fs" % a["clear"] if a["clear"] else " clr >60s"
                    cb = " clr %4.1fs" % b["clear"] if b["clear"] else " clr >60s"
                print("%-6s %-7s | %6.0f el %.2f%-11s| %6.0f el %.2f%-11s| %+4.0f%%" % (
                    stage[0], sc, a["dps"], a["electro"], ca, b["dps"], b["electro"], cb,
                    (b["dps"] / a["dps"] - 1) * 100))
                if args.detail:
                    for nm, d in (("cur", a), ("new", b)):
                        parts = ", ".join("%s %d" % (k, v) for k, v in sorted(d["by"].items(), key=lambda kv: -kv[1]))
                        print("        %s: %s  strikes=%d" % (nm, parts, d["strikes"]))
        print()


if __name__ == "__main__":
    main()
