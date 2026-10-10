using BepInEx.Configuration;
using HollowSaint.FoundationKit.ChargedStorm;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindChargedStorm(ConfigFile c)
        {
            const string cloud = "8. Thundercloud", orb = "9. Hollowed Orb";
            const string circuit = "4. Open Circuit", gaze = "7. Gaze of the Hollow", storm = "5. Storm";
            B(c, circuit, "Closed Circuit", ChargedStormTuning.ClosedCircuit, v => ChargedStormTuning.ClosedCircuit = v, "Electrocutes near the open crown refund the charges fed into it; charges still owed when it closes burst out as a crown nova.");
            F(c, circuit, "Refund reach", ChargedStormTuning.ClosedCircuitReach, v => ChargedStormTuning.ClosedCircuitReach = v, 4, 40, 1, "Electrocutes within this distance of you refund a fed charge (never less than the crown radius).");
            F(c, circuit, "Closing nova damage per charge", ChargedStormTuning.ClosingNovaDamage, v => ChargedStormTuning.ClosingNovaDamage = v, 0, 10, .1f, "Raw coefficient per charge still in the crown when it closes; existing non-Gaze scaling applies once.");
            F(c, circuit, "Closing nova radius", ChargedStormTuning.ClosingNovaRadius, v => ChargedStormTuning.ClosingNovaRadius = v, 4, 30, 1, "Radius of the crown nova around you. With no enemy inside it the charges return to the bank.");
            F(c, circuit, "Closing nova Static priming", ChargedStormTuning.ClosingStrikePrime, v => ChargedStormTuning.ClosingStrikePrime = v, 0, .95f, .05f, "Static the closing nova leaves on each enemy it hits.");
            B(c, circuit, "Empowered pulses finish primed", ChargedStormTuning.CircuitFeedsPrimed, v => ChargedStormTuning.CircuitFeedsPrimed = v, "Extra pulses from fed charges build Static on enemies that already have some, so Circuit finishes primed targets.");
            F(c, gaze, "Static priming", ChargedStormTuning.GazeStaticPrime, v => ChargedStormTuning.GazeStaticPrime = v, 0, .95f, .05f, "Static each Gaze opening blast, surge and lock-on strike leaves on its target. Never Electrocutes by itself.");
            F(c, storm, "Charge income per second", ChargedStormTuning.ChargeIncomePerSecond, v => ChargedStormTuning.ChargeIncomePerSecond = v, 0, 10, .5f, "Late-game flood guard: at most this many Static Charges bank per second (a burst of this many still banks at once). Electrocutes past it still stun, Shock and arc. Zero disables.");
            F(c, "2. Stormspear", "Primed Static multiplier", ChargedStormTuning.SpearPrimedMultiplier, v => ChargedStormTuning.SpearPrimedMultiplier = v, 1, 5, .25f, "Stormspear hits on enemies primed by a charge spender build this much more Static.");
            B(c, "2. Stormspear", "Thunderbolt needs full charge", ChargedStormTuning.ThunderboltNeedsFullCharge, v => ChargedStormTuning.ThunderboltNeedsFullCharge = v, "Only a fully charged throw spends a full Static bank on the Thunderbolt; quick throws never touch the bank.");
            F(c, storm, "Thunderbolt splash Static priming", ChargedStormTuning.ThunderboltSplashPrime, v => ChargedStormTuning.ThunderboltSplashPrime = v, 0, .95f, .05f, "Static the full-bank Thunderbolt's splash leaves on nearby enemies.");
            F(c, cloud, "Aim range", ChargedStormTuning.CloudRange, v => ChargedStormTuning.CloudRange = v, 20, 120, 5, "Maximum distance to the aimed cloud area, in metres.");
            F(c, cloud, "Starting radius", ChargedStormTuning.CloudRadius, v => ChargedStormTuning.CloudRadius = v, 8, 40, 1, "Storm radius with one charge (three quarters of it for a free cast).");
            F(c, cloud, "Radius per extra charge", ChargedStormTuning.CloudRadiusPerCharge, v => ChargedStormTuning.CloudRadiusPerCharge = v, 0, 8, .5f, "Additional attack radius per gathered charge; total radius capped at 40m.");
            F(c, cloud, "Cooldown", ChargedStormTuning.CloudCooldown, v => ChargedStormTuning.CloudCooldown = v, 1, 60, 1, "Cooldown after the strike sequence; restart required.", true);
            F(c, cloud, "Strike damage", ChargedStormTuning.CloudStrikeDamage, v => ChargedStormTuning.CloudStrikeDamage = v, .05f, 10, .05f, "Raw damage of each storm strike with no charges; existing non-Gaze scaling applies once.");
            F(c, cloud, "Strike damage per charge", ChargedStormTuning.CloudStrikeDamagePerCharge, v => ChargedStormTuning.CloudStrikeDamagePerCharge = v, 0, 3, .02f, "Additional raw damage per strike for each gathered charge.");
            F(c, cloud, "Strike interval", ChargedStormTuning.CloudStrikeInterval, v => ChargedStormTuning.CloudStrikeInterval = v, .25f, 3, .05f, "Seconds between storm pulses; each pulse strikes every visible enemy under the cloud once.");
            F(c, cloud, "Storm duration", ChargedStormTuning.CloudBaseDuration, v => ChargedStormTuning.CloudBaseDuration = v, 1, 10, .5f, "Seconds the storm keeps striking with no charges.");
            F(c, cloud, "Duration per charge", ChargedStormTuning.CloudDurationPerCharge, v => ChargedStormTuning.CloudDurationPerCharge = v, 0, 3, .25f, "Extra storm seconds per gathered charge (total capped at 15).");
            F(c, cloud, "Strike proc coefficient", ChargedStormTuning.CloudProc, v => ChargedStormTuning.CloudProc = v, 0, 1, .05f, "Item proc coefficient of each storm strike.");
            F(c, cloud, "Early end refund", ChargedStormTuning.CloudEarlyEndRefund, v => ChargedStormTuning.CloudEarlyEndRefund = v, 0, 1, .05f, "Press Special again during the storm to end it early. This share of the cooldown, scaled by the unused storm time, is refunded (0.5 = half).");
            B(c, cloud, "Free cast", ChargedStormTuning.CloudFreeCast, v => ChargedStormTuning.CloudFreeCast = v, "Thundercloud casts a small, short storm with no charges; holding gathers charges for a bigger, longer, stronger one.");
            I(c, cloud, "Target limit", ChargedStormTuning.CloudTargetLimit, v => ChargedStormTuning.CloudTargetLimit = v, 1, 64, "Maximum enemies struck per storm pulse.");
            F(c, cloud, "Static priming", ChargedStormTuning.CloudStaticPrime, v => ChargedStormTuning.CloudStaticPrime = v, 0, .95f, .05f, "Static each strike leaves on its victim. Priming never Electrocutes by itself; a follow-up hit finishes it. Zero disables.");
            B(c, cloud, "Strikes Shock", ChargedStormTuning.CloudShocks, v => ChargedStormTuning.CloudShocks = v, "Struck enemies are Shocked (take extra damage) for the usual Shocked duration.");
            F(c, cloud, "Static priming cap", ChargedStormTuning.StaticPrimeCap, v => ChargedStormTuning.StaticPrimeCap = v, 0, .95f, .05f, "Highest Static any charge spender (Orb, Thundercloud, Gaze blasts, Thunderbolt splash, closing nova) can leave on an enemy.");
            I(c, cloud, "Cast charge limit", ChargedStormTuning.CastChargeLimit, v => ChargedStormTuning.CastChargeLimit = v, 1, 20, "Maximum stored charges gathered by either new ability; extra banked charges remain available.");
            F(c, orb, "Starting diameter", ChargedStormTuning.OrbDiameter, v => ChargedStormTuning.OrbDiameter = v, .3f, 2.5f, .05f, "Orb diameter without charges or with one empowering charge.");
            F(c, orb, "Diameter per extra charge", ChargedStormTuning.OrbDiameterPerCharge, v => ChargedStormTuning.OrbDiameterPerCharge = v, 0, .3f, .05f, "Visible orb growth; total diameter capped at 1.5m.");
            F(c, orb, "Hit damage", ChargedStormTuning.OrbDamage, v => ChargedStormTuning.OrbDamage = v, .1f, 20, .5f, "Raw first-hit damage at one charge; the free cast subtracts one per-extra-charge increment. Existing non-Gaze scaling applies once.");
            F(c, orb, "Hit damage per charge", ChargedStormTuning.OrbDamagePerCharge, v => ChargedStormTuning.OrbDamagePerCharge = v, 0, 5, .25f, "Additional raw first-hit coefficient per charge. Revisits attenuate per victim.");
            F(c, orb, "Cooldown", ChargedStormTuning.OrbCooldown, v => ChargedStormTuning.OrbCooldown = v, 1, 60, 1, "Cooldown after release/recovery; restart required.", true);
            F(c, orb, "Flight speed", ChargedStormTuning.OrbSpeed, v => ChargedStormTuning.OrbSpeed = v, 10, 80, 2, "Metres per second.");
            F(c, orb, "Launch range", ChargedStormTuning.OrbRange, v => ChargedStormTuning.OrbRange = v, 10, 100, 5, "Maximum initial flight distance.");
            F(c, orb, "Bounce range", ChargedStormTuning.OrbBounceRange, v => ChargedStormTuning.OrbBounceRange = v, 4, 30, 1, "Maximum distance to the next enemy with no charges or one empowering charge.");
            F(c, orb, "Bounce range per extra charge", ChargedStormTuning.OrbBounceRangePerCharge, v => ChargedStormTuning.OrbBounceRangePerCharge = v, 0, 10, .5f, "Extra bounce reach per gathered charge after the first; total reach capped at 60m. Set to zero to retain constant reach.");
            F(c, orb, "Static priming", ChargedStormTuning.OrbStaticPrime, v => ChargedStormTuning.OrbStaticPrime = v, 0, .95f, .05f, "Static the first hit leaves on an enemy; revisits prime less, following damage falloff. Never Electrocutes by itself. Zero disables.");
            I(c, orb, "Hits", ChargedStormTuning.OrbBaseHits, v => ChargedStormTuning.OrbBaseHits = v, 1, 12, "Total hits at one charge, including the first impact; free casts get one fewer (minimum one). Each extra charge adds one, capped at sixteen.");
            I(c, orb, "Hits per Backup Magazine", ChargedStormTuning.OrbHitsPerMagazine, v => ChargedStormTuning.OrbHitsPerMagazine = v, 0, 4, "Each Backup Magazine adds this many hits to the Orb instead of an extra cast. Hits also land on a lone, latched target.");
            B(c, orb, "Latch", ChargedStormTuning.OrbLatch, v => ChargedStormTuning.OrbLatch = v, "With no other enemy in reach the orb clings to its target and zaps its remaining hits there.");
            F(c, orb, "Latch zap interval", ChargedStormTuning.OrbLatchInterval, v => ChargedStormTuning.OrbLatchInterval = v, .08f, 1, .02f, "Seconds between latched zaps.");
            F(c, orb, "Burst damage", ChargedStormTuning.OrbBurstFraction, v => ChargedStormTuning.OrbBurstFraction = v, 0, 3, .05f, "The spent orb bursts for this fraction of its first-hit damage.");
            F(c, orb, "Burst radius", ChargedStormTuning.OrbBurstRadius, v => ChargedStormTuning.OrbBurstRadius = v, 1, 12, .25f, "Burst radius with no charges.");
            F(c, orb, "Burst radius per charge", ChargedStormTuning.OrbBurstRadiusPerCharge, v => ChargedStormTuning.OrbBurstRadiusPerCharge = v, 0, 3, .1f, "Extra burst radius per gathered charge.");
        }
    }
}
