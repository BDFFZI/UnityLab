Shader "Unlit/MarchingCubesGPU"
{
	Properties
	{
		_Albedo("Albedo",Color) = (1,1,1,1)
		_Metallic("Metallic",Range(0,1)) = 0
		_Smoothness("Smoothness",Range(0,1)) = 0
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
			"Queue" = "Geometry"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			#pragma geometry GeometryPass
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../MarchingCubesShader.hlsl"

			struct VertexInput
			{
				float3 positionOS:POSITION;
			};

			struct VertexOutput
			{
				float4 positionCS:SV_POSITION;
				float3 positionWS :COLOR;
				float3 normalWS :NORMAL;
			};

			float4x4 _WorldMatrixM;
			float4 _Albedo;
			float _Metallic;
			float _Smoothness;

			VertexOutput VertexPass(const VertexInput _)
			{
				VertexOutput vertexOutput = (VertexOutput)0;
				return vertexOutput;
			}

			[maxvertexcount(16)]
			void GeometryPass(point VertexOutput _[1], uint cubeIndex : SV_PrimitiveID, inout TriangleStream<VertexOutput> output)
			{
				float4x4 matrixMVP = mul(UNITY_MATRIX_VP, _WorldMatrixM); //几何着色器无法直接获取模型矩阵
				uint3 cubePointIndex = CubeIndexToPointIndex(cubeIndex);

				uint3 pointIndices[8];
				float3 positions[8];
				float3 normals[8];
				float densities[8];
				ComputePointInfos(cubePointIndex, pointIndices, positions, normals, densities);
				int cubeType = ComputeCubeType(densities);

				int edges[16] = triTable[cubeType];
				for (int i = 0; edges[i] != -1; i += 3)
				{
					int edge0Vertex0 = edgeConnections[edges[i + 0]][0];
					int edge0Vertex1 = edgeConnections[edges[i + 0]][1];
					float edge0LerpRate = ComputeLerpRate(densities[edge0Vertex0], densities[edge0Vertex1]);
					float3 edge0PositionOS = lerp(positions[edge0Vertex0], positions[edge0Vertex1], edge0LerpRate);
					float3 edge0NormalOS = normalize(lerp(normals[edge0Vertex0], normals[edge0Vertex1], edge0LerpRate));

					int edge1Vertex0 = edgeConnections[edges[i + 1]][0];
					int edge1Vertex1 = edgeConnections[edges[i + 1]][1];
					float edge1LerpRate = ComputeLerpRate(densities[edge1Vertex0], densities[edge1Vertex1]);
					float3 edge1PositionOS = lerp(positions[edge1Vertex0], positions[edge1Vertex1], edge1LerpRate);
					float3 edge1NormalOS = normalize(lerp(normals[edge1Vertex0], normals[edge1Vertex1], edge1LerpRate));

					int edge2Vertex0 = edgeConnections[edges[i + 2]][0];
					int edge2Vertex1 = edgeConnections[edges[i + 2]][1];
					float edge2LerpRate = ComputeLerpRate(densities[edge2Vertex0], densities[edge2Vertex1]);
					float3 edge2PositionOS = lerp(positions[edge2Vertex0], positions[edge2Vertex1], edge2LerpRate);
					float3 edge2NormalOS = normalize(lerp(normals[edge2Vertex0], normals[edge2Vertex1], edge2LerpRate));

					VertexOutput vertex0;
					vertex0.positionWS = mul(_WorldMatrixM, float4(edge0PositionOS, 1)).xyz;
					vertex0.normalWS = mul((float3x3)_WorldMatrixM, edge0NormalOS);
					vertex0.positionCS = TransformWorldToHClip(vertex0.positionWS);
					VertexOutput vertex1;
					vertex1.positionWS = mul(_WorldMatrixM, float4(edge1PositionOS, 1)).xyz;
					vertex1.normalWS = mul((float3x3)_WorldMatrixM, edge1NormalOS);
					vertex1.positionCS = TransformWorldToHClip(vertex1.positionWS);
					VertexOutput vertex2;
					vertex2.positionWS = mul(_WorldMatrixM, float4(edge2PositionOS, 1)).xyz;
					vertex2.normalWS = mul((float3x3)_WorldMatrixM, edge2NormalOS);
					vertex2.positionCS = TransformWorldToHClip(vertex2.positionWS);

					output.Append(vertex0);
					output.Append(vertex1);
					output.Append(vertex2);
					output.RestartStrip();
				}
			}

			float4 PixelPass(VertexOutput pixel):SV_TARGET
			{
				float3 color = normalize(pixel.normalWS) * 0.5 + 0.5;
				return float4(color, 1);
			}
			ENDHLSL
		}
	}
}