# Controller-first acceptance: Hollow Saint

Primary target: Stuart's controller through his normal r2modman/Steam launch path.
Keyboard/mouse parity is required. The Unity art harness is not the controller test.

Record controller model, native/Steam Input mode, game build, mod DLL hash and profile
when performing this checklist. Do not change the user's bindings or Steam Input mode
to hide a problem. A physical playtest is required for a verified result.

| Check | Required observation | Status |
|---|---|---|
| Selection | Choose Hollow Saint and navigate loadout using controller focus | Pending |
| Partial stick | Small/medium/full tilt preserve native speed response; release stops without drift | Pending |
| Direction | Circles/figure-eight/diagonals/reversals respond continuously | Pending |
| Camera | Right stick respects native sensitivity/inversion; no forced custom camera | Pending |
| Sprint/glide | User's mapped sprint action controls sprint; no keyboard-only requirement | Pending |
| Jump/interact/equipment | Native mapped actions work while moving and aiming | Pending |
| Device swap | Keyboard/mouse still works; returning to controller restores appropriate prompts | Pending |
| Rebinding | Changed native bindings invoke the same action without editing the mod | Pending |
| Primary hold | Held input fires at intended cadence; release stops; movement remains responsive | M3 |
| Dash direction | Movement direction latched at activation; neutral fallback reviewed; air use works | M4 |
| Buff/passive | Circuit/Discharge do not steal movement or camera input | M4 |
| Remote player | Same actions and visible states work when client owns Hollow Saint | M3–M6 |

Foundation implementation rule: no direct KeyCode/Input.GetKey/joystick APIs in the
plugin. Native CharacterMotor, InputBankTest and SkillLocator are retained. Animation
reads velocity and sprint state; it does not normalize or replace player movement.
Temporary vanilla skills in the foundation do not constitute a test of the final kit.
