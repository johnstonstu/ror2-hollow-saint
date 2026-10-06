Run: dotnet run --project tools/TerrainSpearChecks -c Release -p:UseSharedCompilation=false

Links the real world-clearance helper, steering component, spear burst dispatcher,
primary SkillDef and body gate, and their pure rules. Synthetic halfspace physics
and native-slot adapters exercise flat/up/down terrain, finite sphere edge contact,
wall-clamped muzzle origins, preserved heading/budget, primary exclusion, falloff,
LOS, target cap, unchanged crit/proc/mask, mapped input, no blocked stock/cooldown
consumption, Circuit parallelism/closure, cancellation/repress, and death/disable.
Existing GazePrimaryChecks separately covers Gaze override restoration.

No Unity PhysX, network state ordering, controller hardware, item balance,
frame cost or native appearance is claimed. Later native checks must cover
crest/ledge mesh corners, actual template Rigidbody/collider settings, simultaneous
primary+secondary in both device bindings, closing Circuit mid-charge, and
no-item vs Crowbar/confirmed-Voidsent-Flame cluster comparisons.
