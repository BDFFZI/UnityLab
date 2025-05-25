#include "MarchingCubesTable.hlsl"

Texture3D<float> _World;
SamplerState sampler_World;
int3 _WorldResolution;
int3 _WorldDimension;
float _WorldVisibleDensity;

uint3 CubeIndexToPointIndex(uint cubeIndex)
{
    int3 array;
    array.z = cubeIndex / (_WorldResolution.x * _WorldResolution.y);
    uint xy = cubeIndex % (_WorldResolution.x * _WorldResolution.y);
    array.y = xy / _WorldResolution.x;
    array.x = xy % _WorldResolution.x;
    return array;
}
uint PointIndexToCubeIndex(uint3 pointIndex)
{
    return pointIndex.z * _WorldResolution.x * _WorldResolution.y + pointIndex.y * _WorldResolution.x + pointIndex.x;
}

float3 PointIndexToLocalPosition(uint3 pointIndex)
{
    return pointIndex - (float3)_WorldDimension / 2;
}
float SamplePointVisibility(uint3 pointIndex)
{
    float3 uv = (float3)pointIndex / _WorldDimension;
    return _World.SampleLevel(sampler_World, uv, 0).r;
}
float3 ComputePointNormal(uint3 pointIndex)
{
    int3 offsetX = float3(1, 0, 0);
    int3 offsetY = float3(0, 1, 0);
    int3 offsetZ = float3(0, 0, 1);

    float dx = SamplePointVisibility(pointIndex + offsetX) - SamplePointVisibility(pointIndex - offsetX);
    float dy = SamplePointVisibility(pointIndex + offsetY) - SamplePointVisibility(pointIndex - offsetY);
    float dz = SamplePointVisibility(pointIndex + offsetZ) - SamplePointVisibility(pointIndex - offsetZ);

    return -normalize(float3(dx, dy, dz));
}
void ComputePointInfos(uint3 cubePointIndex, out uint3 pointIndices[8], out float3 positions[8], out float3 normals[8], out float densities[8])
{
    pointIndices[0] = cubePointIndex + cornerOffsets[0];
    pointIndices[1] = cubePointIndex + cornerOffsets[1];
    pointIndices[2] = cubePointIndex + cornerOffsets[2];
    pointIndices[3] = cubePointIndex + cornerOffsets[3];
    pointIndices[4] = cubePointIndex + cornerOffsets[4];
    pointIndices[5] = cubePointIndex + cornerOffsets[5];
    pointIndices[6] = cubePointIndex + cornerOffsets[6];
    pointIndices[7] = cubePointIndex + cornerOffsets[7];
    positions[0] = PointIndexToLocalPosition(pointIndices[0]);
    positions[1] = PointIndexToLocalPosition(pointIndices[1]);
    positions[2] = PointIndexToLocalPosition(pointIndices[2]);
    positions[3] = PointIndexToLocalPosition(pointIndices[3]);
    positions[4] = PointIndexToLocalPosition(pointIndices[4]);
    positions[5] = PointIndexToLocalPosition(pointIndices[5]);
    positions[6] = PointIndexToLocalPosition(pointIndices[6]);
    positions[7] = PointIndexToLocalPosition(pointIndices[7]);
    densities[0] = SamplePointVisibility(pointIndices[0]).r;
    densities[1] = SamplePointVisibility(pointIndices[1]).r;
    densities[2] = SamplePointVisibility(pointIndices[2]).r;
    densities[3] = SamplePointVisibility(pointIndices[3]).r;
    densities[4] = SamplePointVisibility(pointIndices[4]).r;
    densities[5] = SamplePointVisibility(pointIndices[5]).r;
    densities[6] = SamplePointVisibility(pointIndices[6]).r;
    densities[7] = SamplePointVisibility(pointIndices[7]).r;
    normals[0] = ComputePointNormal(pointIndices[0]);
    normals[1] = ComputePointNormal(pointIndices[1]);
    normals[2] = ComputePointNormal(pointIndices[2]);
    normals[3] = ComputePointNormal(pointIndices[3]);
    normals[4] = ComputePointNormal(pointIndices[4]);
    normals[5] = ComputePointNormal(pointIndices[5]);
    normals[6] = ComputePointNormal(pointIndices[6]);
    normals[7] = ComputePointNormal(pointIndices[7]);
}
int ComputeCubeType(in float density[8])
{
    int cubeType = 0;
    if (density[0] < _WorldVisibleDensity) cubeType |= 1;
    if (density[1] < _WorldVisibleDensity) cubeType |= 2;
    if (density[2] < _WorldVisibleDensity) cubeType |= 4;
    if (density[3] < _WorldVisibleDensity) cubeType |= 8;
    if (density[4] < _WorldVisibleDensity) cubeType |= 16;
    if (density[5] < _WorldVisibleDensity) cubeType |= 32;
    if (density[6] < _WorldVisibleDensity) cubeType |= 64;
    if (density[7] < _WorldVisibleDensity) cubeType |= 128;
    return cubeType;
}
float ComputeLerpRate(float point0Density, float point1Density)
{
    return (_WorldVisibleDensity - point0Density) / (point1Density - point0Density);
}
