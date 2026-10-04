using System;
using G = HollowSaint.FoundationKit.OpenCircuit.Fx.OpenCircuitDomeGeometry;

// Sparse moving ribbons remain on the exact core-centered damage sphere.
int assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
Check(G.PathCount == 8, "bounded sparse path count");
int samples = 0;
for (int path = 0; path < G.PathCount; path++)
{
    var points = G.CreateUnitPath(path);
    samples += points.Length;
    Check(points.Length == (path < 4 ? 25 : 21), "bounded subdivisions");
    foreach (float time in new[] { 0f, 1f, 10f, 100f })
    {
        G.WriteUnitPath(path, time, points);
        var a = points[0]; var b = points[points.Length - 1];
        Check((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z) > 0.2f, "open ribbon, no closed cage ring");
        foreach (var p in points)
        {
            Check(Math.Abs(p.X*p.X+p.Y*p.Y+p.Z*p.Z-1f)<0.00001f, "tangential animation stays on sphere");
            Check(G.IsLower(path) ? p.Y < 0f : p.Y >= 0f, "honest lower hemisphere");
            Check(path < 4 ? p.Y == 0f : G.IsLower(path) ? p.Y <= -0.18f : p.Y >= 0.22f,
                "separated bands avoid crossing the equator");
            foreach (float radius in new[] { 3f, 8f, 25f })
            {
                var center = new G.Point(11f, 32f, -19f);
                var world = G.ScaleTranslate(p, center, radius);
                float x=world.X-center.X,y=world.Y-center.Y,z=world.Z-center.Z;
                Check(Math.Abs(Math.Sqrt(x*x+y*y+z*z)-radius)<0.0001,
                    "configured radius and core center preserved");
            }
        }
    }
}
Check(samples == 184, "184 cached positions per layer / 368 total");
Check(G.IsValidRadius(25f), "no cosmetic clamp at configured maximum");
foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    Check(!G.IsValidRadius(invalid), "invalid radius hidden");
for (int mask = 0; mask < 16; mask++)
    Check(G.ShouldShow((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0)
        == (mask == 15), "death/buff off/invisibility/disable gates preserved");
Console.WriteLine($"PASS: {assertions} assertions; 8 open ribbons, 16 renderers, 368 cached positions; moving 3/8/25m sphere boundaries.");
