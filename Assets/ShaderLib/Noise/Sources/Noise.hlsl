/*! @file

## 噪波的特性

- 随机性：宏观来看，各区域的颜色是随机的。
- 平滑性：微观来看，各像素间的颜色是连续的。
- 稳定性：同样的输入参数只会得到同样的结果。

## 实现方式

1. 随机性：利用随机数生成算法获取随机值。
2. 稳定性：随机数算法实际是一种哈希函数，产生的是可控的伪随机。
3. 平滑性：通过平滑算法平滑随机值，使各区域间有颜色过渡。

## 噪波分类

- 基于晶格
    - 梯度噪波（根据向量点乘插值）
        - Perlin噪波（柏林噪波）：最早最流行的噪波实现。
        - Simplex噪波（单纯形噪波）：相比柏林噪波，开销更低且减弱方向伪影。
        - Simulation噪波
        - Wavelet噪波
    - 值噪波（根据值线性插值）：
        - Value噪波
- 基于点
    - Worley噪波（细胞格噪波）

## 参考资料

- [噪波分类](https://en.wikipedia.org/wiki/Template:Coherent_noise)
- [游戏开发中的噪声算法](https://www.cnblogs.com/KillerAery/p/10765897.html)
- [使用Unity生成各类型噪声图的分享](https://zhuanlan.zhihu.com/p/463369923)
- [生成连续的2D、3D柏林噪声](https://zhuanlan.zhihu.com/p/620107368)
*/

#pragma once

#include "Random.hlsl"

//! 基于正方形晶格的四个顶点的值，获取其中任意位置的插值结果
float SmoothLerp(
    const float bottomLeft,
    const float bottomRight,
    const float topLeft,
    const float topRight,
    const float2 uv)
{
    //利用smoothstep获取更加平滑的结果
    const float2 smoothUv = smoothstep(0, 1, frac(uv));

    //获取x轴方向的插值
    const float bottom = lerp(bottomLeft, bottomRight, smoothUv.x);
    const float top = lerp(topLeft, topRight, smoothUv.x);
    //获取y轴方向的插值
    const float middle = lerp(bottom, top, smoothUv.y);

    return middle;
}

//! 视觉效果像马赛克的噪波
float MosaicNoise(const float2 uv)
{
    return Rand(floor(uv));
}

//! 最简陋的值噪波
float ValueNoise(const float2 uv)
{
    /// 晶格为正方形，通过四个角的位置产生随机数，然后对这些数插值以得出结果。
    const float2 origin = floor(uv);
    const float bottomLeft = Rand(origin + float2(0, 0));
    const float bottomRight = Rand(origin + float2(1, 0));
    const float topLeft = Rand(origin + float2(0, 1));
    const float topRight = Rand(origin + float2(1, 1));

    return SmoothLerp(bottomLeft, bottomRight, topLeft, topRight, uv);
}

//! 早期最流行的噪波实现
float PerlinNoise(const float2 uv)
{
    /// 类似值噪波，但不同在将基于四个顶点位置产生的随机值换成了，基于四个顶点的位置生成的随机向量与目标位置方向的点乘结果，再以此进行插值。
    const float2 origin = floor(uv);
    const float2 positionOS = uv - origin;
    const float bottomLeft = dot(RandDirection2(origin + float2(0, 0)), positionOS - float2(0, 0));
    const float bottomRight = dot(RandDirection2(origin + float2(1, 0)), positionOS - float2(1, 0));
    const float topLeft = dot(RandDirection2(origin + float2(0, 1)), positionOS - float2(0, 1));
    const float topRight = dot(RandDirection2(origin + float2(1, 1)), positionOS - float2(1, 1));
    const float lerp = SmoothLerp(bottomLeft, bottomRight, topLeft, topRight, uv);

    return lerp * 0.5 + 0.5; //点乘的结果是[-1,1]，所以需要映射回[0,1]
}

float2 TransformSimplexToHypercube(float2 positionSS)
{
    const float F = (sqrt(2 + 1) - 1) / 2;
    return positionSS + (positionSS.x + positionSS.y) * F;
}
float2 TransformHypercubeToSimplex(float2 positionHS)
{
    const float G = (1 - 1 / sqrt(2 + 1)) / 2;
    return positionHS - (positionHS.x + positionHS.y) * G;
}
//! 性能与效果更好的噪波
float SimplexNoise(const float2 uv)
{
    /// 单纯形：能铺满当前维度空间的最简单几何体，如二维是三角形，三维是三棱锥，顶点数为n+1
    /// 超方体：由对准当前维度每个基向量的整齐排列的等长的线段组成，如二维是正方形，三维是正方体，顶点数为2^n <https://zh.wikipedia.org/wiki/%E8%B6%85%E6%96%B9%E5%BD%A2>
    /// 
    /// 1. 使用每个维度的单纯形划分晶格而不是超方型。
    /// 2. 但以单纯形作为晶格，很难计算对应的顶点位置，所以需要转换到超方形空间计算，然后逆推回来。
    /// 3. 随机值获取依旧采取点乘，但混合方式改为一种基于半径的混合方式。
    /// 
    /// 参考资料中的教程间存在冲突，故具体实现以wiki为准

    const float2 p1HS = floor(TransformSimplexToHypercube(uv));
    const float2 v1SS = uv - TransformHypercubeToSimplex(p1HS);

    const float2 p2HS = p1HS + float2(1, 1);
    const float2 v2SS = uv - TransformHypercubeToSimplex(p2HS);

    //使用单纯形空间的向量做判断与超方形空间的结果是一致的，这样可以减少代码
    const float2 p3HS = p1HS + (v1SS.x < v1SS.y ? float2(0, 1) : float2(1, 0));
    const float2 v3SS = uv - TransformHypercubeToSimplex(p3HS);

    //计算各点随机值
    const float3 random = float3(
        dot(RandDirection2(p1HS), v1SS),
        dot(RandDirection2(p2HS), v2SS),
        dot(RandDirection2(p3HS), v3SS)
    );

    //计算各点混合系数
    const float3 attenuation = pow(
        max(0.5 - float3(
                dot(v1SS, v1SS),
                dot(v2SS, v2SS),
                dot(v3SS, v3SS)
            ), 0),
        4);

    //乘以70是为了将随机值均匀缩放到区间[-1,1]，否则变化太小了，70是预估的最大默认结果的倒数。
    return dot(random, attenuation) * 70 * 0.5 + 0.5;
}

//! 看起来像细胞格的噪波
float WorleyNoise(const float2 uv)
{
    /// 将空间分成多个晶格，在每个晶格内生成一个随机点，计算每个像素最近随机点的距离，该距离就是噪波值。
    /// 因为任意像素的最近点可能出现在自身区域，或周围8个区域，所以这些点都要遍历一遍。
    
    const float2 origin = floor(uv);

    float minDis = 1; //虽然最短距离可能超过1，但返回值是[0,1]区间
    //遍历周围区域（九宫格）
    for (float x = -1; x <= 1; x++)
    {
        for (float y = -1; y <= 1; y++)
        {
            //计算当前区域随机点的位置
            const float2 areaOrigin = origin + float2(x, y);
            const float2 areaPoint = areaOrigin + Rand2(areaOrigin);
            //计算当前像素与随机点的最小距离
            minDis = min(minDis, distance(uv, areaPoint));
        }
    }

    return minDis;
}


//! PerlinNoise，3D实现
float PerlinNoise(const float3 uv)
{
    const float3 origin = floor(uv);
    const float3 positionOS = uv - origin;

    const float bottomLeft = dot(RandDirection3(origin + float3(0, 0, 0)), positionOS - float3(0, 0, 0));
    const float bottomRight = dot(RandDirection3(origin + float3(1, 0, 0)), positionOS - float3(1, 0, 0));
    const float topLeft = dot(RandDirection3(origin + float3(0, 1, 0)), positionOS - float3(0, 1, 0));
    const float topRight = dot(RandDirection3(origin + float3(1, 1, 0)), positionOS - float3(1, 1, 0));
    const float value = SmoothLerp(bottomLeft, bottomRight, topLeft, topRight, uv);

    const float bottomLeft2 = dot(RandDirection3(origin + float3(0, 0, 1)), positionOS - float3(0, 0, 1));
    const float bottomRight2 = dot(RandDirection3(origin + float3(1, 0, 1)), positionOS - float3(1, 0, 1));
    const float topLeft2 = dot(RandDirection3(origin + float3(0, 1, 1)), positionOS - float3(0, 1, 1));
    const float topRight2 = dot(RandDirection3(origin + float3(1, 1, 1)), positionOS - float3(1, 1, 1));
    const float value2 = SmoothLerp(bottomLeft2, bottomRight2, topLeft2, topRight2, uv);

    return lerp(value, value2, smoothstep(0, 1, positionOS.z)) * 0.5 + 0.5; //点乘的结果是[-1,1]，所以需要映射回[0,1]
}

float3 TransformSimplexToHypercube(float3 positionSS)
{
    const float F = (sqrt(3 + 1) - 1) / 3;
    return positionSS + (positionSS.x + positionSS.y + positionSS.z) * F;
}
float3 TransformHypercubeToSimplex(float3 positionHS)
{
    const float G = (1 - 1 / sqrt(3 + 1)) / 3;
    return positionHS - (positionHS.x + positionHS.y + positionHS.z) * G;
}
//! SimplexNoise，3D实现
float SimplexNoise(const float3 uv)
{
    //获取当前点在超方形空间基本信息
    const float3 uvHS = TransformSimplexToHypercube(uv);
    const float3 originHS = floor(uvHS);
    const float3 vectorHS = uvHS - originHS;
    const int3 vectorState = int3(
        vectorHS.x > vectorHS.y,
        vectorHS.y > vectorHS.z,
        vectorHS.z > vectorHS.x
    );
    //根据向量特征，获取对应单纯形在超方体空间中的顶点位置
    const float2x3 pointDictionary[] = {
        (float2x3)0, //(0,0,0)
        float2x3(float3(0, 0, 1), float3(0, 1, 1)), //(0,0,1)
        float2x3(float3(0, 1, 0), float3(1, 1, 0)), //(0,1,0)
        float2x3(float3(0, 1, 0), float3(0, 1, 1)), //(0,1,1)
        float2x3(float3(1, 0, 0), float3(1, 0, 1)), //(1,0,0)
        float2x3(float3(0, 0, 1), float3(1, 0, 1)), //(1,0,1)
        float2x3(float3(1, 0, 0), float3(1, 1, 0)), //(1,1,0)
        (float2x3)0, //(1,1,1)
    };
    const float2x3 otherPoint = pointDictionary[vectorState.x * 4 + vectorState.y * 2 + vectorState.z];
    float3 p1 = originHS + float3(0, 0, 0);
    float3 p2 = originHS + float3(1, 1, 1);
    float3 p3 = originHS + otherPoint[0];
    float3 p4 = originHS + otherPoint[1];

    //将单纯形顶点从超方形空间转回单纯形空间
    p1 = TransformHypercubeToSimplex(p1);
    p2 = TransformHypercubeToSimplex(p2);
    p3 = TransformHypercubeToSimplex(p3);
    p4 = TransformHypercubeToSimplex(p4);

    //获得各顶点的方向向量
    const float3 v1 = uv - p1;
    const float3 v2 = uv - p2;
    const float3 v3 = uv - p3;
    const float3 v4 = uv - p4;

    //计算各点随机值
    const float4 random = float4(
        dot(RandDirection3(p1), v1),
        dot(RandDirection3(p2), v2),
        dot(RandDirection3(p3), v3),
        dot(RandDirection3(p4), v4)
    );

    //计算各点的径向衰减系数
    const float4 attenuation = pow(
        max(0.5 - float4(
                dot(v1, v1),
                dot(v2, v2),
                dot(v3, v3),
                dot(v4, v4)
            ), 0),
        4);

    //3D单纯形噪波使用70缩放系数可能超出[-1,1]范围，故改为65
    return dot(random, attenuation) * 65 * 0.5 + 0.5;
}