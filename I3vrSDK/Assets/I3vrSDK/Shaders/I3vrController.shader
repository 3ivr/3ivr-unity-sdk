Shader "I3vr/Unlit/Controller" {
  Properties {
    _Color ("Color", COLOR) = (1, 1, 1, 1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {
      "Queue" = "Overlay+100"
      "IgnoreProjector" = "True"
      "RenderType"="Transparent"
    }
    LOD 100

    ZWrite On
    Blend SrcAlpha OneMinusSrcAlpha

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      #define _I3VR_DISPLAY_RADIUS .25
      #define _BUTTON_PRESS_DEPTH 0.0003
      #define _TOUCH_FEATHER 8

      #define _I3VR_TOUCHPAD_CENTER half2(.15, .85)
      #define _I3VR_TOUCHPAD_RADIUS .139

	  #define _RETURN_UV_X_MAX 0.2
	  #define _RETURN_UV_Y_MAX 0.3

      #define _HOME_UV_X_MAX 0.2
      #define _HOME_UV_Y_MAX 0.5

      #define _APP_UV_X_MAX 0.2
      #define _APP_UV_Y_MAX 0.7

      #define _TOUCHPAD_UV_X_MAX 0.305

      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
      };

      struct v2f {
        half2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        half4 color : TEXCOORD1;
        half2 touchVector : TEXCOORD2;
        half alpha : TEXCOORD3;
      };

      sampler2D _MainTex;
      half4 _I3vrControllerAlpha;
      float4 _MainTex_ST;

      half4 _Color;
      half4 _I3vrTouchPadColor;
      half4 _I3vrAppButtonColor;
	  half4 _I3vrReturnButtonColor;
      half4 _I3vrHomeButtonColor;
      half4 _I3vrTouchInfo;//xy position, z touch duration

      v2f vert (appdata v) {
        v2f o;
        float4 vertex4;
        vertex4.xyz = v.vertex;
        vertex4.w = 1.0;

        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.color = half4(0,0,0,0);
        o.touchVector = half2(0,0);
        o.alpha = 1;
		
		// Return Button
		if (v.uv.y < _RETURN_UV_Y_MAX) {
			 if (v.uv.x < _RETURN_UV_X_MAX) {
				 o.color = _I3vrReturnButtonColor;
				 o.color.rgb = _I3vrControllerAlpha.y * o.color.rgb;
				 o.color.a = (_I3vrControllerAlpha.y);

				 vertex4.y -= _BUTTON_PRESS_DEPTH*_I3vrControllerAlpha.y;
			 }
		}
		// Home Button
        else if( v.uv.y < _HOME_UV_Y_MAX){
          if(v.uv.x < _HOME_UV_X_MAX){
            o.color = _I3vrHomeButtonColor;
            o.color.rgb = _I3vrControllerAlpha.w * o.color.rgb;
            o.color.a = ( _I3vrControllerAlpha.w);

            vertex4.y -= _BUTTON_PRESS_DEPTH*0.05*_I3vrControllerAlpha.w;
          }
        }
        // App Button
        else if(v.uv.y < _APP_UV_Y_MAX){
          if(v.uv.x < _APP_UV_X_MAX){
            o.color = _I3vrAppButtonColor;
            o.color.rgb = _I3vrControllerAlpha.z * o.color.rgb;
            o.color.a = ( _I3vrControllerAlpha.z);
            vertex4.y -= _BUTTON_PRESS_DEPTH*_I3vrControllerAlpha.z;
          }
        }
        // Touchpad
        else{
          if(v.uv.x < _TOUCHPAD_UV_X_MAX){
            half2 touchPosition = ((v.uv - _I3VR_TOUCHPAD_CENTER)/_I3VR_TOUCHPAD_RADIUS - _I3vrTouchInfo.xy);

            half scaledInput = _I3vrTouchInfo.z + .25;
            half bounced = 2 * (2 * scaledInput - scaledInput*scaledInput -.4375);
            o.touchVector = (2-bounced)*( (1 - _I3vrControllerAlpha.y)/_I3VR_DISPLAY_RADIUS ) *touchPosition;
            o.color = _I3vrTouchPadColor;
            o.color.rgb = _I3vrTouchInfo.z *o.color.rgb;
            o.color.a = (_I3vrTouchInfo.z);
          }
        }
        o.vertex = UnityObjectToClipPos(vertex4);

        return o;
      }

      fixed4 frag (v2f i) : SV_Target {

        // Compute the length from a touchpoint, scale the value to control the edge sharpness.
        half len = saturate(_TOUCH_FEATHER*(1-length(i.touchVector)) );
        i.color = i.color *len;

        half4 texcol =  tex2D(_MainTex, i.uv);
        half3 tintColor = (i.color.rgb + (1-i.color.a) * _Color.rgb);

        half luma = Luminance(tintColor);
        tintColor = texcol.rgb *(tintColor + .25*(1-luma));

        texcol.a = i.alpha * texcol.a + (1-i.alpha)*(texcol.r)* i.color.a;
        texcol.rgb = i.alpha * tintColor + (1-i.alpha)*i.color.rgb;

        return texcol;
      }
      ENDCG
    }
  }
  FallBack "Unlit/Transparent"
}
