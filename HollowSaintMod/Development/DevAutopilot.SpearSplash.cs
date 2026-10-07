using System.Collections;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>HS_SEGMENTS=spear-splash: tap and full-charge Stormspear into a dummy and into the
    /// ground, slowed so the 3D burst splash is captured across its life.</summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator SpearSplashSegments()
        {
            foreach (var full in new[] { false, true })
            foreach (var ground in new[] { false, true })
            {
                string tag = (full ? "full" : "tap") + (ground ? "-ground" : "-enemy");
                yield return Segment("spear-splash-" + tag);
                aimTarget = ground ? null : DummyChest(full ? 0 : 1);
                aimPitch = ground ? 32f : 0f;
                yield return Wait(0.4f);
                fire2 = true;
                yield return Wait(full ? 2.2f : 0.05f);
                fire2 = false;
                yield return Wait(0.08f);
                Time.timeScale = 0.25f;
                yield return Wait(0.18f);
                for (int i = 0; i < 6; i++)
                {
                    yield return Wait(0.06f);
                    Shot("ss-" + tag + "-" + i);
                    if (i == 2 || i == 4) WideShot("ss-" + tag + "-wide-" + i);
                }
                Time.timeScale = 1f;
                aimPitch = 0f;
                yield return Wait(4.5f);
            }
        }
    }
}
