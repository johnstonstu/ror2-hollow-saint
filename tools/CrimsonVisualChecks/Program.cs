using System;
using HollowSaint;
using UnityEngine;
public static class CrimsonChecks
{
    private static int checks;
    private static void Check(bool value,string message) { checks++; if(!value)throw new Exception(message); }
    private static float Max(Color c) => Math.Max(c.r,Math.Max(c.g,c.b));
    public static void Main()
    {
        string[] plate={"V12_ivory_ceramic_plate","HYBRID_warm_ivory_Unity_body"};
        foreach(var n in plate)Check(CrimsonMasteryVisuals.Describe(n).Kind==CrimsonMasteryVisuals.Role.Plate,"plate classification");
        Check(CrimsonMasteryVisuals.Describe("HYBRID_source_texture___ceramic_graphite_and_cyan_Unity_body").Kind==CrimsonMasteryVisuals.Role.Body,"mixed atlas is not a glowing conductor or plate-only mesh");
        Check(CrimsonMasteryVisuals.Describe("V11_aged_copper_blocks").Kind==CrimsonMasteryVisuals.Role.Crown,"crown metal");
        Check(CrimsonMasteryVisuals.Describe("V11_tabard_copper_trim").Kind==CrimsonMasteryVisuals.Role.Trim,"small trim accents");
        Check(CrimsonMasteryVisuals.Describe("V11_tabard_cloth").Metallic==0,"matte cloth");
        foreach(string n in new[]{"V11_cyan_conductor_light","V11_halo_gap_light","V11_halo_top_gap_light","Shaft_conductor","Contained_lightning_spine","Sharp_contained_energy_point","Tail_energy_point","Pulse_entering_spear","V11_cyan_core_hot"})
        {
            var s=CrimsonMasteryVisuals.Describe(n);
            Check(s.IsLight,"authored lights recognized");Check(Max(s.Emission)<=1f,"bounded base emission");
            Check(s.Emission.r>s.Emission.g*3f,"red heat retained");
        }
        foreach(string n in new[]{"V12_ivory_ceramic_plate","HYBRID_graphite","V11_tabard_cloth","V11_tabard_copper_trim","V11_aged_copper_blocks",null})
            Check(Max(CrimsonMasteryVisuals.Describe(n).Emission)==0f,"broad surfaces never emissive");
        for(int value=0;value<=255;value++)
        foreach(bool p in new[]{false,true})
        {
            var source=new Color(value/255f,.9f,.75f,.42f);
            var dark=CrimsonMasteryVisuals.AtlasPixel(source,0,p);
            var light=CrimsonMasteryVisuals.AtlasPixel(source,1,p);
            Check(dark.a==source.a&&light.a==source.a,"original alpha preserved");
            Check(Max(dark)<.3f,"non-light atlas remains charcoal");
            Check(light.r>light.g*5f&&light.r>light.b*5f,"neutral mask remaps cyan to red");
            Check(dark.b>=dark.r,"armor stays neutral-cool not red wash");
        }
        Check(CrimsonMasteryVisuals.MatchesMaterial("V11_conductor (Crimson Vow)"),"material suffix for display detection");
        foreach(string n in new[]{"x (Verdigris)","x (Obsidian)","x (Solar)","x (Umbral)",null})
            Check(!CrimsonMasteryVisuals.MatchesMaterial(n),"existing skins not captured");
        Check(CrimsonMasteryVisuals.Pulse.g-CrimsonMasteryVisuals.Arc.g>.6f,"gold pulse distinguishes red current");
        Console.WriteLine("PASS: "+checks+" Crimson Vow visual assertions. Material/shader GPU appearance not tested.");
    }
}
namespace UnityEngine
{
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color black=>new Color(0,0,0);
        public static Color operator *(Color c,float f)=>new Color(c.r*f,c.g*f,c.b*f,c.a*f);
        public static Color Lerp(Color x,Color y,float t){t=Mathf.Clamp01(t);return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);}
    }
    public static class Mathf {public static float Clamp01(float x)=>Math.Max(0f,Math.Min(1f,x));}
}
