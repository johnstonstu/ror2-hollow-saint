These checks compile the production reversible metal-crown pose, perimeter
component, radius policy and HaloRingShape with hierarchy/render substitutes.
They cover exact-radius bone docks, core-centred equator, 1000-cycle restoration,
same-frame reapply, invalid tuning/model replacement, buff-based observer startup,
sparse lightning bounds including halfwidth, bounded pool reuse, native-pose
ownership yielding to Gaze, expiry/death/disable and warmed managed allocations.

Run `dotnet run --project tools/OpenCircuitPerimeterChecks -c Release`.

The adapters substitute Unity rendering/physics and palette selection. Passing
does not accept native animation ordering, metal mesh extents, bloom, Unity
allocations, actual skin materials or host/client lifecycle. Actual skin palette
tests remain in GazePresentationChecks; native playtesting remains required.
