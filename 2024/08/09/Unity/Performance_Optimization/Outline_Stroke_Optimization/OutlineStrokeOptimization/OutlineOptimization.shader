Shader "HaruoYaguchi/UI/OutlineOptimization"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Int) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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

        pass
        {
            Name "OUTLINE_OPTIMIZATION"
            CGPROGRAM
        
            // 顶点着色器
            #pragma vertex vert
            // 片元着色器
            #pragma fragment frag


            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_TexelSize;

            float4 _OutlineColor;
            int _OutlineWidth;

            struct a2v // 命名表示从应用程序传递到顶点函数，application to vertex
            {
                float4 vertex : POSITION;    // 告诉Unity把模型空间下的顶点坐标填充给vertex属性
                float3 normal : NORMAL;      // 告诉Unity把模型空间下的法线方向填充给normal属性
                float2 texcoord : TEXCOORD0; // 告诉Unity把第一套纹理坐标填充给texcoord属性
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                float4 tangent : TANGENT;    // 告诉Unity把模型空间下的切线向量填充给tangent属性
                float4 color : COLOR;        // 告诉Unity顶点色填充给color属性
            }; 

            struct v2f //命名表示从顶点函数传递到片元函数，vertex to fragment
            {
                float4 vertex : SV_POSITION;    //转换为裁剪空间的顶点坐标
                float2 texcoord : TEXCOORD0;
                float2 uvOriginXY : TEXCOORD1;
                float2 uvOriginZW : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(a2v IN)
            {
                v2f vtf;
                
                vtf.vertex = UnityObjectToClipPos(IN.vertex);
                vtf.texcoord = IN.texcoord;
                vtf.uvOriginXY = IN.texcoord1;
                vtf.uvOriginZW = IN.texcoord2;
                vtf.color = IN.color * _Color;

                return vtf;
            }

            // 判断当前点是否在 uvOrigin 的方法如下：在范围内返回 1，不在范围内返回 0
            fixed IsInRect(float2 fPoint, v2f IN)
            {
                float2 inside = step(IN.uvOriginXY, fPoint) * step(fPoint, IN.uvOriginZW);
                return inside.x * inside.y;
            }

            // 沿当前像素采样的 8 个方向偏移
            const float2 SampleOffsets[8] = 
            {
                {-1, -1}, {0, -1}, {1, -1},
                {-1, 0},           {1, 0},
                {-1, 1},  {0, 1},  {1, 1}
            };

            fixed SampleAlpha(int pIndex, v2f IN)
            {
                const fixed sinArray[12] = { 0, 0.5, 0.866, 1, 0.866, 0.5, 0, -0.5, -0.866, -1, -0.866, -0.5 };
                const fixed cosArray[12] = { 1, 0.866, 0.5, 0, -0.5, -0.866, -1, -0.866, -0.5, 0, 0.5, 0.866 };
                float2 pos = IN.texcoord + _MainTex_TexelSize.xy * float2(cosArray[pIndex], sinArray[pIndex]) * _OutlineWidth;
                return IsInRect(pos, IN) * (tex2D(_MainTex, pos) + _TextureSampleAdd).w * _OutlineColor.w;
            }

            fixed4 frag(v2f IN) : SV_TARGET
            {
                
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                if (_OutlineWidth > 0) 
                {
                    color.w *= IsInRect(IN.texcoord, IN);
                    half4 val = half4(_OutlineColor.x, _OutlineColor.y, _OutlineColor.z, 0);

                    val.w += SampleAlpha(0, IN);
                    val.w += SampleAlpha(1, IN);
                    val.w += SampleAlpha(2, IN);
                    val.w += SampleAlpha(3, IN);
                    val.w += SampleAlpha(4, IN);
                    val.w += SampleAlpha(5, IN);
                    val.w += SampleAlpha(6, IN);
                    val.w += SampleAlpha(7, IN);
                    val.w += SampleAlpha(8, IN);
                    val.w += SampleAlpha(9, IN);
                    val.w += SampleAlpha(10, IN);
                    val.w += SampleAlpha(11, IN);

                    val.w = clamp(val.w, 0, 1);
                    color = (val * (1.0 - color.a)) + (color * color.a);
                }

                return color;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
