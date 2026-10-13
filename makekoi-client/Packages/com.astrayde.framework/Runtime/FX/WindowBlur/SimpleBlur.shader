Shader "Unlit/SimpleBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _Offset;

                fixed4 col = 0;
                col += tex2D(_MainTex, i.uv + texel * float2(-1, -1));
                col += tex2D(_MainTex, i.uv + texel * float2( 0, -1));
                col += tex2D(_MainTex, i.uv + texel * float2( 1, -1));
                col += tex2D(_MainTex, i.uv + texel * float2(-1,  0));
                col += tex2D(_MainTex, i.uv);
                col += tex2D(_MainTex, i.uv + texel * float2( 1,  0));
                col += tex2D(_MainTex, i.uv + texel * float2(-1,  1));
                col += tex2D(_MainTex, i.uv + texel * float2( 0,  1));
                col += tex2D(_MainTex, i.uv + texel * float2( 1,  1));

                return col / 9;
            }
            ENDHLSL
        }
    }
}
