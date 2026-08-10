Shader "UI/UIGrayscale"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        _GrayAmount ("Gray Amount", Range(0, 1)) = 0 // 0 = 正常 1 = 完全灰 0.5 = 半灰
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GrayAmount;
            float4 _ClipRect;
            float4 _MainTex_TexelSize;
            float _TextureSampleAdd = 0; // Unity UI 内部使用，防止未声明错误

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 1. 转换为灰度值（权重与 UIGray.ToGray / TMP_SDF_GRAY 一致）
                fixed gray = dot(color.rgb, fixed3(0.299, 0.587, 0.114));
                // 2. 按 _GrayAmount 在 原色 与 灰度 间插值：0 = 正常 1 = 完全灰
                color.rgb = lerp(color.rgb, fixed3(gray, gray, gray), _GrayAmount);

                // 3. 剪裁判断 (支持 ScrollView 剪裁)
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipRect(IN.worldPosition, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHATEST
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}