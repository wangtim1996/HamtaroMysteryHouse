Shader "Custom/LineShader" {
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

			struct LineData {
				float3 a;
				float3 b;
				half4 color;
				float width;
			};

			StructuredBuffer<LineData> line_data;

			struct v2g {
				float4 pos : SV_POSITION;
				half4 color : COLOR;
				float3 pos2 : POS2;
				float width : WIDTH;
			};

			struct g2f{
				half4 pos : SV_POSITION;
				float4 color : COLOR;
			};


			v2g vert(uint id : SV_VertexID){
				v2g o;
				o.pos = float4(line_data[id].a, 0);
				o.pos2 = line_data[id].b;
				o.color = line_data[id].color;
				o.width = line_data[id].width;
				return o;
			}
			
			[maxvertexcount(4)]
			void geom(point v2g IN[1], inout TriangleStream<g2f> triStream){
				float3 vec = IN[0].pos;			
				half4 color = IN[0].color;
				float3 cPos = _WorldSpaceCameraPos;

				float3 dir = IN[0].pos2 - IN[0].pos;
				float cameraDist1 = distance(cPos, IN[0].pos);
				float cameraDist2 = distance(cPos, IN[0].pos2);
				float3 up = normalize(cross(cPos - vec, dir)) * IN[0].width;
				
				g2f OUT;
				OUT.pos = UnityObjectToClipPos(IN[0].pos + (up * cameraDist1));
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(IN[0].pos - (up * cameraDist1));
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(IN[0].pos2 + (up * cameraDist2));
				OUT.color = color;
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(IN[0].pos2 - (up * cameraDist2));
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
