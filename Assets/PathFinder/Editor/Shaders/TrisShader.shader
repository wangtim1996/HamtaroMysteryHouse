Shader "Custom/TrisShader" {
		SubShader{
			Pass{
				Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "DisableBatching" = "true" }
				Blend SrcAlpha OneMinusSrcAlpha
				Cull Off
				ZTest Off
				ZWrite Off
				CGPROGRAM

			#include "UnityCG.cginc" 
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#pragma target 4.0

			struct TriangleData {
				float3 a;
				float3 b;
				float3 c;
				half4 color;
			};

			StructuredBuffer<TriangleData> triangle_data;

			struct v2g {
				float4 a : SV_POSITION;
				float3 b : B;
				float3 c : C;
				half4 color : COLOR;
			};

			struct g2f{
				float4 pos : SV_POSITION;
				half4 color : COLOR;
			};


			v2g vert(uint id : SV_VertexID){
				v2g o;
				o.a = float4(triangle_data[id].a, 0);
				o.b = triangle_data[id].b;
				o.c = triangle_data[id].c;
				o.color = triangle_data[id].color;
				return o;
			}
			
			[maxvertexcount(3)]
			void geom(point v2g IN[1], inout TriangleStream<g2f> triStream){				
				g2f OUT;
				OUT.pos = UnityObjectToClipPos(IN[0].a);
				OUT.color = IN[0].color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(IN[0].b);
				OUT.color = IN[0].color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(IN[0].c);
				OUT.color = IN[0].color;
				triStream.Append(OUT);		
			}

			float4 frag(g2f IN) : COLOR{
				return IN.color;
			}

			ENDCG
		}
	}
}
