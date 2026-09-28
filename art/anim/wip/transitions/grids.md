### State -> state (rows: from, columns: to; passed/total handoffs, bold = has a flagged handoff)

| From \ To | Arc Bolt | Arc Step | Ascend | Conduit Spear | Discharge | Fall (Descend) | Glide | Glide enter | Glide exit | Idle | Idle combat | Jump | Land | Meter full flourish | Plant turn | Run (8-way) | Run lean | Run pivot | Run start | Run stop | Walk (8-way) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Arc Bolt |  |  |  |  |  |  |  |  |  | 2/2 |  |  |  |  |  |  |  |  |  |  |  |
| Arc Step |  | 41/41 |  |  |  | 4/4 | 4/4 |  |  | 5/5 |  |  |  |  |  | 6/6 |  |  |  |  |  |
| Ascend |  |  |  |  |  | 5/5 | 1/1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Conduit Spear |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Fall (Descend) |  | 4/4 |  |  |  |  | 1/1 |  |  |  |  |  | 5/5 |  |  |  |  |  |  |  |  |
| Glide |  | **2/4** |  |  |  | 1/1 |  |  | 6/6 |  |  |  |  |  |  |  |  |  |  |  |  |
| Glide enter |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Glide exit |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 5/5 |  |  |  | 1/1 |  |
| Idle | 2/2 | 5/5 |  | 1/1 | 1/1 |  |  |  |  |  | 1/1 | 1/1 |  | 1/1 |  | 2/2 |  |  | 1/1 |  | 2/2 |
| Idle combat |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Jump |  |  | 3/3 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Land |  |  |  |  |  |  |  |  |  | 3/3 |  |  |  |  |  | 2/2 |  |  |  |  |  |
| Meter full flourish |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Plant turn |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run (8-way) |  | 7/7 |  |  |  | 1/1 |  | 1/1 |  | 1/1 |  | 2/2 |  |  | 1/1 | 19/19 | 1/1 | 1/1 |  | 3/3 | 1/1 |
| Run lean |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run pivot |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run start |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 2/2 |  |  |  |  |  |
| Run stop |  |  |  |  |  |  |  |  |  | 4/4 |  |  |  |  |  |  |  |  |  |  |  |
| Walk (8-way) |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  | 1/1 |  |  |  |  | 9/9 |

### Skill layer over locomotion (rows: skill event, columns: the state underneath)

| Skill | Arc Step | Ascend | Fall (Descend) | Glide | Glide enter | Idle | Idle combat | Jump | Land | Run (8-way) | Run lean | Run start | Run stop | Walk (8-way) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Arc Bolt (next phase / chain / state change under it) |  | 1/1 |  | 1/1 | 1/1 | 2/2 |  | 1/1 |  | 3/3 |  | 1/1 |  |  |
| Arc Bolt -> Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Arc Bolt in |  |  | 2/2 | 2/2 |  | 4/4 |  |  |  | 14/14 |  |  |  | 2/2 |
| Arc Bolt out | 1/1 | 1/1 | 2/2 | 3/3 |  | 3/3 |  |  |  | 11/11 |  | 1/1 |  | 2/2 |
| Charge -> Discharge |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |
| Charge in |  |  |  |  |  | 3/3 | 2/2 |  |  |  |  |  |  |  |
| Charge out |  |  |  |  |  | 2/2 | 2/2 |  |  |  |  |  |  |  |
| Conduit Spear (next phase / chain / state change under it) |  |  |  |  |  |  |  |  | 1/1 | 1/1 |  |  | 1/1 |  |
| Conduit Spear -> Arc Bolt |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Conduit Spear -> Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Conduit Spear in |  |  | 2/2 | 1/1 |  | 1/1 |  |  |  | 7/7 |  |  |  | 1/1 |
| Conduit Spear out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 5/5 |  |  | 1/1 | 1/1 |
| Discharge (next phase / chain / state change under it) | 4/4 | 1/1 |  |  |  |  |  |  |  | 2/2 |  |  |  |  |
| Discharge in | 2/2 | 1/1 | 2/2 | 2/2 |  | 2/2 | 1/1 | 1/1 |  | 13/13 | 1/1 |  | 1/1 | 9/9 |
| Discharge out | 1/1 | 2/2 | 2/2 | 2/2 |  | 4/4 | 1/1 |  |  | 16/16 | 1/1 |  |  | 9/9 |
| Meter full flourish in |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |
| Meter full flourish out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |
| Open Circuit (next phase / chain / state change under it) |  |  | 2/2 | 2/2 |  | 2/2 |  |  |  | 6/6 |  |  |  | 2/2 |
| Open Circuit in |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | **2/3** |  |  |  | 1/1 |
| Open Circuit out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |
