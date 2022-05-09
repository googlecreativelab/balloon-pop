Shader "Custom/CircularBarGradientTexture" {
    Properties {
        _Frac ("Progress Bar Value", Range(0,1)) = 1.0
        [NoScaleOffset] _AlphaTex ("Alpha", 2D) = "White" {}
        [NoScaleOffset] _GradientTex ("Gradient", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BackColor ("Back Color", Color) = (0,0,0,1)
    }

    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Direct3D compiled stats:
            // vertex shader:
            //   8 math
            // fragment shader:
            //   2 math, 2 texture

            // half _Frac;
            fixed4 _Color;
            fixed4 _BackColor;

            sampler2D _AlphaTex;
            sampler2D _GradientTex;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in __fragment Shader__.
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half, _Frac)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata_img v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.xy = v.texcoord.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

                // sample gradient texture
                fixed gradient = tex2D(_GradientTex, i.uv).r;

                // ternary to pick between fill and background colors
                fixed4 col = (UNITY_ACCESS_INSTANCED_PROP(Props, _Frac) >= gradient) ? _Color : _BackColor;

                fixed alpha = tex2D(_AlphaTex, i.uv).a;
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
}