Shader "Custom/DotShader" {
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

			struct PointData {
				float3 pos;
				half4 color;
				float size;
			};

			StructuredBuffer<PointData> point_data;	

			struct input {
				float4 pos : SV_POSITION;
				half4 color : COLOR;
				float size : SIZE;

			};

			struct g2f{
				float4 pos : SV_POSITION;
				half4 color : COLOR;
			};


			input vert(uint id : SV_VertexID){
				input o;
				o.pos = float4(point_data[id].pos, 0);
				o.color = point_data[id].color;
				o.size = point_data[id].size;		
				return o;
			}
			
			[maxvertexcount(4)]
			void geom(point input IN[1], inout TriangleStream<g2f> triStream){
				float3 vec = IN[0].pos.xyz;			
				float4 color = IN[0].color;

				float3 up = normalize(UNITY_MATRIX_IT_MV[1].xyz);
				float3 right = normalize(UNITY_MATRIX_IT_MV[0].xyz);

			    float size = IN[0].size;

				right = right * size;
				up = up * size;

				g2f OUT;
				OUT.pos = UnityObjectToClipPos(vec -right -up);
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(vec -right + up);
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(vec + right -up);
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(vec + right + up);
				OUT.color = color;
				triStream.Append(OUT);
			}

			float4 frag(g2f IN) : COLOR{
				return IN.color;
			}

			ENDCG
		}
	}
}
