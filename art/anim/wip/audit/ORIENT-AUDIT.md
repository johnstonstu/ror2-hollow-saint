# 9f orientation audit of hollow-saint-anim-v16.blend (read-only copy), Sep 27 ~12:50 AM PT

Method: background Blender on wip/audit/audit-v16-copy.blend, every frame of all 28 `HS_anim |` actions. Character faces -Y (verified: chest core and front camera are on -Y).
- Gate: upper arm >45 deg from hanging, OR elbow >140 deg with the upper arm >30 deg.
- thumb-up = world Z of the unit radial axis (index knuckle mesh minus little knuckle mesh, i.e. which edge of the hand the thumb sits on). The thumb.1 bone vector and thumb-tip-light direction agree in sign on the failing clips. Checked against renders: Discharge f15, Open Circuit f17 and Arc Bolt f15 all show the red thumb hanging below the hand.
- palm·fwd is reported two ways, because they disagree on this rig (see finding 2).

## Finding 1: thumb-down on extended arms is real (Stuart is right)
The worst clips are the casting/reaching ones:
- Discharge: both hands, f9-28, worst -0.79 L (f15) / -0.62 R (f17).
- Open Circuit: L f18-27 (-0.90), R f14-28 (-0.96). Open Circuit end: f18-22 (~-0.75). Open Circuit hold: f2-11 (L -0.45, R -0.13).
- Arc Bolt right: R casting arm f3-20, worst -0.80 at f15. Arc Bolt left mirrors it (L -0.82).
- Arc Step end: both hands f4-16 (-0.72 / -0.80).
- Charge full: R the whole clip (-1.00 at f22, thumb pointing straight down).
- Select intro: R f11-25 (-0.94).
- Jump: both hands f4-11 (-0.94 at f6).
- Milder: Ascend/Descend (~-0.4), Land f1-7 (-0.4), Walk arm swing (-0.5, arms not really extended), Spawn f32-41 (-0.27), Select idle L (-0.47).
- Passing on thumb-up: Glide loop (+0.55/+0.61), Glide enter, Run forward, Arc Step loop (+0.9).

## Finding 2: the hand geometry appears mirror-handed. Needs a human check before anyone "fixes" roll.
- On every frame of every clip, for both hands, the finger-curl direction is opposite to the palm normal a correctly-handed hand would have (mean dot -0.8, 100% of frames).
- The rest pose shows the same thing. On the L hand, the index sits at the back and the ring/little finger at the front, while the fingers curl medial-forward. A real left hand in that pose has the index in front. The R hand shows the same mirror pattern.
- In effect each side carries the other side's hand.
- Consequence: on an arm held out, "thumb up" and "fingers curl forward" are mutually exclusive with this geometry. Glide loop has the thumb up but the fingers curl backward (curl-side palm·fwd -0.4). Discharge has the fingers curling forward but the thumb down (+0.97 / -0.79).
- So forearm roll alone cannot satisfy 9f everywhere. The fix is probably rig-level (mirror the finger/thumb layout on each hand, or swap which face the hand pass curls toward), plus forearm supination.
- Recommend: look at orient-rest-f01-lside.png / -lclose.png and Glide loop lclose to confirm, then decide.

## Table (gated frames only)
| clip | hand | gated frames | THUMB-DOWN frames (thumb-up<0) | worst thumb-up (frame) | palm·fwd, finger-curl side (min/max over gated) | palm·fwd, anatomical-chirality (min) |
|---|---|---|---|---|---|---|
| Aim down | L | 2 | — | +0.14 (f1) | +0.56 / +0.56 | -0.56 |
| Aim down | R | 2 | — | +0.14 (f1) | +0.56 / +0.56 | -0.56 |
| Aim left | L | 2 | 1-2 | -0.18 (f1) | +0.66 / +0.66 | -0.66 |
| Aim left | R | 2 | 1-2 | -0.18 (f2) | +0.66 / +0.66 | -0.66 |
| Aim neutral | L | 2 | 1-2 | -0.18 (f1) | +0.66 / +0.66 | -0.66 |
| Aim neutral | R | 2 | 1-2 | -0.18 (f1) | +0.66 / +0.66 | -0.66 |
| Aim right | L | 2 | 1-2 | -0.18 (f2) | +0.66 / +0.66 | -0.66 |
| Aim right | R | 2 | 1-2 | -0.18 (f1) | +0.66 / +0.66 | -0.66 |
| Aim up | L | 2 | 1-2 | -0.47 (f1) | +0.65 / +0.65 | -0.65 |
| Aim up | R | 2 | 1-2 | -0.47 (f1) | +0.66 / +0.66 | -0.66 |
| Arc Bolt left | L | 19 | 1,3-20 | -0.82 (f15) | +0.02 / +0.66 | -0.66 |
| Arc Bolt left | R | 20 | 1-20 | -0.26 (f3) | +0.66 / +0.67 | -0.67 |
| Arc Bolt right | L | 20 | 1-20 | -0.26 (f3) | +0.66 / +0.67 | -0.67 |
| Arc Bolt right | R | 19 | 1,3-20 | -0.80 (f15) | +0.03 / +0.66 | -0.66 |
| Arc Step end | L | 16 | 4-16 | -0.72 (f5) | -0.60 / +0.66 | -0.66 |
| Arc Step end | R | 16 | 4-16 | -0.80 (f5) | -0.54 / +0.66 | -0.66 |
| Arc Step loop | L | 11 | — | +0.91 (f1) | -0.59 / -0.45 | +0.45 |
| Arc Step loop | R | 11 | — | +0.89 (f2) | -0.61 / -0.47 | +0.47 |
| Arc Step start | L | 7 | 1 | -0.18 (f1) | -0.59 / +0.66 | -0.66 |
| Arc Step start | R | 7 | 1 | -0.18 (f1) | -0.54 / +0.66 | -0.66 |
| Ascend | L | 21 | 1-21 | -0.40 (f18) | +0.13 / +0.21 | -0.21 |
| Ascend | R | 21 | 1-21 | -0.32 (f20) | +0.14 / +0.21 | -0.21 |
| Charge full | R | 25 | 1-25 | -1.00 (f22) | -0.46 / -0.39 | +0.39 |
| Descend | L | 21 | 1-21 | -0.43 (f17) | +0.07 / +0.11 | -0.11 |
| Descend | R | 21 | 1-21 | -0.40 (f14) | +0.07 / +0.12 | -0.12 |
| Discharge | L | 22 | 1-2,9-28 | -0.79 (f15) | +0.41 / +0.97 | -0.97 |
| Discharge | R | 22 | 1-2,9-28 | -0.62 (f17) | +0.42 / +0.97 | -0.97 |
| Glide enter | L | 9 | — | +0.13 (f5) | -0.36 / -0.25 | +0.25 |
| Glide enter | R | 13 | — | +0.12 (f6) | -0.42 / -0.19 | +0.19 |
| Glide exit | L | 10 | 7 | -0.21 (f7) | -0.38 / -0.16 | +0.16 |
| Glide exit | R | 10 | — | +0.20 (f10) | -0.42 / -0.15 | +0.15 |
| Glide loop | L | 33 | — | +0.55 (f28) | -0.51 / -0.34 | +0.34 |
| Glide loop | R | 33 | — | +0.61 (f32) | -0.46 / -0.42 | +0.42 |
| Idle | L | 59 | 8-20,56-67 | -0.02 (f14) | -0.09 / -0.06 | +0.06 |
| Idle | R | 59 | 8-19,56-68 | -0.02 (f62) | -0.09 / -0.06 | +0.06 |
| Idle combat | L | 3 | 6-7,31 | -0.20 (f7) | -0.26 / -0.26 | +0.26 |
| Jump | L | 10 | 4-11 | -0.94 (f6) | -0.31 / +0.16 | -0.16 |
| Jump | R | 10 | 4-11 | -0.93 (f6) | -0.32 / +0.15 | -0.15 |
| Land | L | 13 | 1-7 | -0.41 (f2) | -0.08 / +0.08 | -0.08 |
| Land | R | 13 | 1-7 | -0.39 (f2) | -0.08 / +0.08 | -0.08 |
| Open Circuit | L | 12 | 1-2,18-27 | -0.90 (f18) | +0.52 / +0.87 | -0.87 |
| Open Circuit | R | 18 | 1-3,14-28 | -0.96 (f17) | +0.29 / +0.89 | -0.89 |
| Open Circuit end | L | 6 | 2,18-22 | -0.78 (f18) | -0.04 / +0.85 | -0.85 |
| Open Circuit end | R | 6 | 2,18-22 | -0.70 (f18) | +0.05 / +0.84 | -0.84 |
| Open Circuit hold | L | 9 | 2-10 | -0.45 (f5) | +0.79 / +0.86 | -0.86 |
| Open Circuit hold | R | 10 | 2-11 | -0.13 (f5) | +0.79 / +0.85 | -0.85 |
| Run backward | L | 7 | 7,13 | -0.08 (f13) | -0.13 / +0.03 | -0.03 |
| Run backward | R | 8 | 5,15 | -0.08 (f5) | -0.14 / +0.04 | -0.04 |
| Run forward | L | 7 | — | +0.14 (f15) | -0.22 / -0.15 | +0.15 |
| Run forward | R | 6 | — | +0.14 (f7) | -0.22 / -0.15 | +0.15 |
| Select idle | L | 29 | 1-7,76-97 | -0.47 (f7) | -0.26 / -0.19 | +0.19 |
| Select intro | L | 20 | 1-3,28-44 | -0.46 (f3) | -0.22 / -0.14 | +0.14 |
| Select intro | R | 15 | 11-25 | -0.94 (f25) | +0.18 / +0.78 | -0.78 |
| Spawn | L | 13 | 33-41 | -0.27 (f33) | -0.07 / +0.02 | -0.02 |
| Spawn | R | 37 | 15-16,32-41 | -0.21 (f33) | -0.58 / +0.02 | -0.02 |
| Walk forward | L | 19 | 12-18,23 | -0.52 (f16) | +0.44 / +0.62 | -0.62 |
| Walk forward | R | 19 | 1-5,10,25-27 | -0.52 (f3) | +0.45 / +0.62 | -0.62 |
