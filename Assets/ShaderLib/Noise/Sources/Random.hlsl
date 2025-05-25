/*! @file
随机数生成算法都是伪随机，伪随机是通过一个固定的数学公式算出来的，理论上可以预测，所以叫伪随机。
能形成随机效果是因为算法过于复杂，变化过于跳跃，难以简单预测出结果。

广义的随机数算法依赖迭代实现，但在 Shader 中很难做到，且不能满足稳定性要求，所以shader中的随机数生成算法需另辟蹊径。
https://blog.csdn.net/qq_23030843/article/details/104353754
 */

#pragma once

#define sinf 43758.5453

//! 随机生成[0,1]的数
float Rand(const float seed)
{
    /*
Shader的随机数生成基本都是通过`fract(sin(x*C))`来实现，其中 C 变化率非常大，大到使结果都看上去随机变化了一样。
     */

    return frac(sin(seed) * sinf);
}

//! 随机生成[0,1]的数
float Rand(const float2 seed)
{
    /*
对于多维输入值生成随机数，实际还是将其转换为一维输入值处理。
通过直接相加是最简单的化为一维输入的方法，但这种太过规律，可通过给每个维度输入值一定的系数偏移，只要系数够大就可以起到类似一维随机数生成原理一样的效果。
通常这样操作会使用`dot(x,C)`来简化代码实现。
     */

    return frac(sin(dot(seed, float2(12.9898, 78.233))) * sinf);
}

//! 随机生成[0,1]的数
float Rand(const float3 seed)
{
    return frac(sin(dot(seed, float3(12.9898, 78.233, 43.772))) * sinf);
}

//! 随机生成两个[0,1]的数
float2 Rand2(const float2 seed)
{
    /*
seed一般是位置坐标，通过偏移打乱连续性，使其在后续的随机数生成中能生成两个不相关的随机数
     */
    const float2 randomSeed = float2(
        dot(seed, float2(251.36, 487.63)),
        dot(seed, float2(232.39, 153.21))
    );

    return frac(sin(randomSeed) * sinf);
}

//! 随机生成三个[0,1]的数
float3 Rand3(const float3 seed)
{
    /*
seed一般是位置坐标，通过偏移打乱连续性，使其在后续的随机数生成中能生成两个不相关的随机数
     */
    const float3 randomSeed = float3(
        dot(seed, float3(251.36, 487.63, 232.39)),
        dot(seed, float3(153.21, 277.52, 302.32)),
        dot(seed, float3(483.51, 261.13, 460.61))
    );

    return frac(sin(randomSeed) * sinf);
}

//! 随机生成两个[-1,1]的数
float2 RandDirection2(const float2 seed)
{
    return Rand2(seed) * 2 - 1;
}

//! 随机生成三个[-1,1]的数
float3 RandDirection3(const float3 seed)
{
    return Rand3(seed) * 2 - 1;
}
