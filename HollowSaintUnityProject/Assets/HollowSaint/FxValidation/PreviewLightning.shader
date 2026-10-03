Shader "HollowSaint/PreviewLightning" {
 Properties { _TintColor ("Tint", Color) = (1,1,1,1) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Pass { Blend SrcAlpha One ZWrite Off Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
   fixed4 _TintColor;
   Output vert(Input i) { Output o; o.vertex=UnityObjectToClipPos(i.vertex); o.uv=i.uv; return o; }
   fixed4 frag(Output i):SV_Target { fixed4 c=_TintColor; c.a *= pow(saturate(1-abs(i.uv.y*2-1)),2); return c; }
   ENDCG
  }
 }
}
