using System;
using HollowSaint.FoundationKit.OpenCircuit.Fx;
using G = HollowSaint.FoundationKit.OpenCircuit.Fx.OpenCircuitDomeGeometry;

// Success: bounded reusable topology, every sample on the true sphere at 3/8/25m,
// exact full-sphere extents at arbitrary core centers, closed rings, honest lower half,
// and immediate cancellation for buff-off, invisibility, death and disable.
int assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
void Near(float actual, float expected, string message)
    => Check(Math.Abs(actual - expected) < 0.0001f, message + $": {actual} != {expected}");

Check(G.PathCount == 11, "bounded path count");
int samples = 0;
for (int path = 0; path < G.PathCount; path++)
{
    var points = G.CreateUnitPath(path);
    samples += points.Length;
    Check(points.Length == (path < 3 ? 49 : 25), "bounded subdivisions");
    foreach (var p in points)
    {
        Near(p.X * p.X + p.Y * p.Y + p.Z * p.Z, 1f, "unit sphere shell");
        Check(G.IsLower(path) ? p.Y <= 0.00001f : p.Y >= -0.00001f, "hemisphere sign");
    }
    if (path < 3)
    {
        Near(points[0].X, points[points.Length - 1].X, "ring seam x");
        Near(points[0].Z, points[points.Length - 1].Z, "ring seam z");
    }
}
Check(samples == 347, "347 positions per layer / 694 total renderer positions");
foreach (float radius in new[] { 3f, 8f, 25f })
foreach (var center in new[] { new G.Point(0f, 0f, 0f), new G.Point(11f, 32f, -19f) })
{
    float minX = float.MaxValue, minY = minX, minZ = minX;
    float maxX = float.MinValue, maxY = maxX, maxZ = maxX;
    for (int path = 0; path < G.PathCount; path++)
    foreach (var unit in G.CreateUnitPath(path))
    {
        var p = G.ScaleTranslate(unit, center, radius);
        float dx = p.X - center.X, dy = p.Y - center.Y, dz = p.Z - center.Z;
        Check(Math.Abs(Math.Sqrt(dx * dx + dy * dy + dz * dz) - radius) < 0.0001,
            "every displayed sample uses configured radius and core center");
        minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
        minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
    }
    Near(minX, center.X - radius, "left extent"); Near(maxX, center.X + radius, "right extent");
    Near(minY, center.Y - radius, "lower extent"); Near(maxY, center.Y + radius, "upper extent");
    Near(minZ, center.Z - radius, "back extent"); Near(maxZ, center.Z + radius, "front extent");
}
Check(G.IsValidRadius(25f), "no cosmetic clamp at configured maximum");
foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    Check(!G.IsValidRadius(invalid), "invalid radius hidden");
for (int mask = 0; mask < 16; mask++)
    Check(G.ShouldShow((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0)
        == (mask == 15), "death/buff off/invisibility/disable cancel; cast alone cannot show");
Console.WriteLine($"PASS: {assertions} assertions; 11 paths, 22 renderers, 694 total positions; 3/8/25m spheres and all visibility gates.");
