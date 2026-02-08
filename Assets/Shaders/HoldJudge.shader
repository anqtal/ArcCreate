Shader "Gameplay/HoldJudge"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Particle Texture", 2D) = "white" {}
    }
    Category
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"
        }
        Cull Off Lighting Off ZWrite Off ZTest Off

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="true"
            }
            ZWrite Off
            Cull Off
            Blend SrcAlpha One
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                CBUFFER_START(Variables)
                    float4 _TintColor;
                CBUFFER_END

                sampler2D _MainTex;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.color = v.color;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    // float alphaFactor = (smoothstep(0.9, 1, 1 - distance(i.uv, float2(0.5, 0.5))) * 4 + 0.15);
                    float alphaFactor = (smoothstep(0.985, 1, 1 - pow(distance(i.uv, float2(0.5, 0.5)), 2)) + 0.2);
                    float4 c = tex2D(_MainTex, i.uv) * i.color;
                    c.a *= alphaFactor;
                    return c;
                }
                ENDCG
            }
        }
    }
}