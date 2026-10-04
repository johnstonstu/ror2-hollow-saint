using System;
using HollowSaint.FoundationKit.ArcBolt;

// Success: faster supported settings cannot increase nominal travel range,
// old/slower settings retain their range, and the contour reaches the collider
// edge without exceeding it at multiple supported radii.
int assertions = 0;
void Near(float actual, float expected, string label)
{
    assertions++;
    if (Math.Abs(actual - expected) > 0.0001f)
        throw new Exception(label + ": " + actual + " != " + expected);
}
foreach (float templateLifetime in new[] { 0.5f, 5f, 10f })
{
    foreach (float speed in new[] { 30f, 60f, 80f, 120f, 200f })
    {
        float actualRange = speed * ArcBoltReliabilityRules.Lifetime(templateLifetime, speed);
        Near(actualRange, Math.Min(speed, 80f) * templateLifetime, "range speed " + speed);
    }
}
Near(ArcBoltReliabilityRules.Lifetime(5f, 120f), 10f / 3f, "default fast lifetime");
foreach (float radius in new[] { 0.3f, 0.6f, 0.75f, 1f })
{
    Near(ArcBoltReliabilityRules.ContourRadius(radius) + ArcBoltReliabilityRules.ContourWidth(radius) / 2f,
        radius, "visible contour envelope");
    assertions++;
    if (ArcBoltReliabilityRules.CoreDiameter(radius) / 2f >= radius)
        throw new Exception("white core must leave room for the skin contour");
}
Console.WriteLine("Arc Bolt reliability: " + assertions + " assertions passed.");
