/*
* Copyright (C) 2017 3ivr. All rights reserved.
*
* Author: Lucas(Wu Pengcheng)
* Date  : 2017/06/19 08:08
*/

Shader "I3vr/Unlit/Transparent Overlay" {
  Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
  }

  SubShader {
    Tags {
      "Queue"="Overlay+100"
      "IgnoreProjector"="True"
      "RenderType"="Transparent"
    }

    LOD 100

    Blend SrcAlpha OneMinusSrcAlpha
    AlphaTest Off
    Cull Back
    Lighting Off
    ZWrite Off
    ZTest Always

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 2.0
      #pragma multi_compile_fog

      #include "UnityCG.cginc"

      #include "I3vrUnityCompatibility.cginc"

      struct appdata_t {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        half2 texcoord : TEXCOORD0;
        UNITY_FOG_COORDS(1)
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;

      v2f vert (appdata_t v) {
        v2f o;
        o.vertex = GvrUnityObjectToClipPos(v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        UNITY_TRANSFER_FOG(o,o.vertex);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        fixed4 col = tex2D(_MainTex, i.texcoord);
        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
      }
      ENDCG
    }
  }
}
