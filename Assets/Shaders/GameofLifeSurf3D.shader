Shader "Custom/GameofLifeSurf3D"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Cutoff ("Cutoff", Range(0, 1)) = 0.5 // 透過用
    }
    SubShader
    {
        // 透過用
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }
        LOD 200

        CGPROGRAM
        // 透過させるため alphatest:_Cutoff を設定
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows alphatest:_Cutoff
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup // 初期化処理としてsetupを実行する

        #include "UnityCG.cginc"

        struct Input
        {
            float4 color;
            float4 emission;
        };

        struct Cell
        {
            float3 position;
            float3 velocity;
            float4 color;
            float4 emission;
            int state;
            int display;
            float scale;
        };
    
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<Cell> _CellBuffer;
    #endif

        half _Glossiness;
        half _Metallic;

        float4x4 _World2Local;
        float4x4 _Local2World;

        // 初期化処理
        void setup()
        {
            unity_WorldToObject = _World2Local; // ワールド行列の逆変換
            unity_ObjectToWorld = _Local2World; // モデル行列
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            // インスタンスIDとセルバッファからセルの情報を取得する
            Cell c = _CellBuffer[unity_InstanceID];
			o.color = c.color;
			o.emission = c.emission;
			v.vertex.xyz *= c.scale;
			v.vertex.xyz += c.position.xyz;
        #endif
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 col = IN.color;
            o.Albedo = col.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
            o.Emission = IN.emission;
        }
        ENDCG
    }
    FallBack "Diffuse"
}