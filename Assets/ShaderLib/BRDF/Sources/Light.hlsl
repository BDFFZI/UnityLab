#define MaxLightCount 4
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void GetLights(float3 positionWS, out int lightCount, out float3 lightDirections[MaxLightCount], out float3 lightIntensities[MaxLightCount])
{
    Light lights[MaxLightCount];
    lights[0] = GetMainLight(TransformWorldToShadowCoord(positionWS));
    for (int i = 1; i < MaxLightCount; ++i)
        lights[i] = GetAdditionalLight(i - 1, positionWS, 0);

    lightCount = 1 + min(MaxLightCount - 1, GetAdditionalLightsCount());
    for (int i = 0; i < MaxLightCount; i++)
    {
        lightDirections[i] = lights[i].direction;
        lightIntensities[i] = lights[i].color * lights[i].distanceAttenuation * lights[i].shadowAttenuation;
    }
}

/**
 * 兰伯特光照，一种漫反射光照模型。
 * https://en.wikipedia.org/wiki/Lambertian_reflectance
 * @param surfaceNormal 表面方向
 * @param surfaceColor 表面颜色
 * @param lightDirections 表面到灯光的方向
 * @param lightIntensities 灯光颜色及强度
 * @return 
 */
float3 Lambert(float3 surfaceNormal, float3 surfaceColor, float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount], int lightCount)
{
    float3 light = 0;

    for (int i = 0; i < lightCount; ++i)
    {
        float diffuseIntensity = saturate(dot(lightDirections[i], surfaceNormal));
        light += diffuseIntensity * surfaceColor * lightIntensities[i];
    }

    return light;
}

/**
 * Phong，一种结合了漫反射，镜面反射，环境反射的光照模型。
 * https://en.wikipedia.org/wiki/Phong_reflection_model
 * @param ambientColor 环境反射的颜色
 * @param diffuseColor 漫反射的颜色
 * @param specularColor 镜面反射的颜色
 * @param ambientRatio 环境光反射比例
 * @param diffuseRatio 光线的漫反射比例
 * @param specularRatio 光线的镜面反射比例 
 * @param shininess 表面光泽度，越光滑镜面高光越集中
 * @param surfaceNormal 表面方向
 * @param cameraDirection 表面到相机的方向
 * @param lightDirections 表面到灯光的方向
 * @param lightIntensities 灯光颜色及强度
 * @param lightCount 灯光数量
 * @return 
 */
float3 Phong(
    float3 ambientColor, float3 diffuseColor, float3 specularColor,
    float ambientRatio, float diffuseRatio, float specularRatio, float shininess,
    float3 surfaceNormal, float3 cameraDirection, float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount], int lightCount)
{
    float3 light = 0;
    //环境光
    light += ambientColor * ambientRatio;
    //灯光
    for (int i = 0; i < lightCount; ++i)
    {
        //漫射光
        float diffuseIntensity = saturate(dot(lightDirections[i], surfaceNormal)); //利用Lambert方法得出漫射强度
        float3 diffuse = diffuseRatio * diffuseIntensity * lightIntensities[i] * diffuseColor;
        //镜射光
        float3 reflectedDirection = reflect(-lightDirections[i], surfaceNormal);
        float specularIntensity = saturate(dot(reflectedDirection, cameraDirection)); //利用出射向量与相机方向的点乘得出镜射强度
        specularIntensity = pow(specularIntensity, shininess); //利用光泽度使高光集中
        float3 specular = specularRatio * specularIntensity * lightIntensities[i] * specularColor;

        light += diffuse + specular * sign(diffuseIntensity); //只有满足漫射条件时，才能发出镜射光
    }

    return light;
}

/**
 * BlinnPhong，一种改进版的Phong，其在高光处理上效率更高效果也更真实，曾是过去很多图形接口内置的光照模型。
 * https://en.wikipedia.org/wiki/Blinn%E2%80%93Phong_reflection_model
 * @param ambientColor 环境反射的颜色
 * @param diffuseColor 漫反射的颜色
 * @param specularColor 镜面反射的颜色
 * @param ambientRatio 环境光反射比例
 * @param diffuseRatio 光线的漫反射比例
 * @param specularRatio 光线的镜面反射比例 
 * @param shininess 表面光泽度，越光滑镜面高光越集中
 * @param surfaceNormal 表面方向
 * @param cameraDirection 表面到相机的方向
 * @param lightDirections 表面到灯光的方向
 * @param lightIntensities 灯光颜色及强度
 * @param lightCount 灯光数量
 * @return 
 */
float3 BlinnPhong(
    float3 ambientColor, float3 diffuseColor, float3 specularColor,
    float ambientRatio, float diffuseRatio, float specularRatio, float shininess,
    float3 surfaceNormal, float3 cameraDirection, float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount], int lightCount)
{
    float3 light = 0;
    //环境光
    light += ambientColor * ambientRatio;
    //灯光
    for (int i = 0; i < lightCount; ++i)
    {
        //漫射光
        float diffuseIntensity = saturate(dot(lightDirections[i], surfaceNormal));
        float3 diffuse = diffuseRatio * diffuseIntensity * lightIntensities[i] * diffuseColor;
        //镜射光
        float3 halfway = normalize(lightDirections[i] + cameraDirection);
        float specularIntensity = saturate(dot(halfway, surfaceNormal)); //改用半角向量点乘法线来得出镜射强度，这种方式得到的强度值通常更大，变化率也有所不同
        specularIntensity = pow(specularIntensity, shininess * 4); //由于BlinnPhong得出的点乘值通常比Phong更大，为了统一参数效果，增强shininess来抑制多出的点乘值
        float3 specular = specularRatio * specularIntensity * lightIntensities[i] * specularColor;

        light += diffuse + specular * sign(diffuseIntensity);
    }

    return light;
}


float SmoothnessToRoughness(float smoothness)
{
    return max(pow(1 - smoothness, 2),HALF_MIN_SQRT);
}

float NormalDistribution_GGX(float nDotH, float roughness2)
{
    return roughness2 / (PI * pow(pow(nDotH, 2) * (roughness2 - 1) + 1.0, 2));
}

float NormalDistribution_UnityGGX(float nDotH, float roughness2)
{
    return roughness2 / pow(pow(nDotH, 2) * (roughness2 - 1) + 1, 2);
}

float GeometricAttenuation_Smith(float nDotL, float nDotV, float roughness)
{
    float k = pow(roughness + 1, 2) / 8;

    float attenuation1 = 1 / lerp(nDotL, 1, k);
    float attenuation2 = 1 / lerp(nDotV, 1, k);
    return attenuation1 * attenuation2;
}

float GeometricAttenuation_SKSm(float lDotH, float roughness)
{
    return 1 / lerp(pow(lDotH, 2), 1, roughness);
}

float Fresnel(float nDotV, float refractive)
{
    float f = pow(refractive - 1, 2) / pow(refractive + 1, 2);

    return lerp(pow(1 - nDotV, 5), 1, f);
}
float3 Fresnel_Metallic(float nDotV, float3 specular)
{
    return lerp(pow(1 - nDotV, 5), 1, specular);
}

// float GeometricAttenuationAndFresnel_Unity_Origin(float lDotH, float roughness)
// {
//     return GeometricAttenuation_SKSm(lDotH, roughness) * pow(1 - lDotH, 5);
// }

float GeometricAttenuationAndFresnel_Unity(float lDotH, float roughness)
{
    return 1 / (pow(lDotH, 2) * (roughness + 0.5));
}

float3 GlobalIlluminationDiffuse_Unity(float3 n, float3 diffuse)
{
    half3 radiance = SampleSH(n);

    return diffuse * radiance;
}
float3 GlobalIlluminationSpecular_Unity(float3 n, float3 v, float3 specular, float metallic, float smoothness)
{
    float perceptualRoughness = 1 - smoothness;
    float roughness = max(pow(1 - smoothness, 2),HALF_MIN_SQRT);
    float roughness2 = roughness * roughness;

    float3 radiance = GlossyEnvironmentReflection(reflect(-v, n), perceptualRoughness, 1);

    //反射衰减（确保能量守恒）
    float surfaceReduction = 1.0 / (roughness2 + 1.0);

    //镜射光反射率
    float fresnel = Pow4(1.0 - saturate(dot(n, v))); //菲涅尔效应导致光全反射
    float ks = lerp(0.04, 1, metallic); //镜射率
    float3 finalSpecular = lerp(specular, saturate(ks + smoothness), fresnel);

    return finalSpecular * radiance * surfaceReduction;
}

float3 CookTorrance(float3 albedo, float metallic, float smoothness, float3 surfaceNormal, float3 cameraDirection,
                    float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount], int lightCount)
{
    float3 diffuse = lerp(0.94 * albedo, 0, metallic); //漫射光反射率
    float3 specular = lerp(0.04, 1 * albedo, metallic); //镜射光反射率

    float perceptualRoughness = 1 - smoothness;
    float roughness = max(pow(1 - smoothness, 2),HALF_MIN_SQRT);
    float roughness2 = roughness * roughness;

    float vDotN = saturate(dot(cameraDirection, surfaceNormal));

    float3 light = 0;
    //直接光
    for (int i = 0; i < lightCount; ++i)
    {
        //辐射率
        float nDotL = saturate(dot(surfaceNormal, lightDirections[i]));
        float3 radiance = lightIntensities[i] * nDotL;

        //镜射光
        float3 halfway = normalize(lightDirections[i] + cameraDirection);
        float nDotH = saturate(dot(surfaceNormal, halfway));
        float lDotH = saturate(dot(lightDirections[i], halfway));
        float D = roughness2 / pow(nDotH * nDotH * (roughness2 - 1) + 1, 2);
        float G = 1 / lerp(lDotH * lDotH, 1, roughness);
        float3 F = lerp(pow(1 - vDotN, 5), 1, specular);
        float3 specularTerm = (D * G * F) / 4;

        light += radiance * (diffuse + specularTerm);
    }
    //间接光
    {
        float3 diffuseRadiance = SampleSH(surfaceNormal);
        light += diffuseRadiance * diffuse;

        float3 specularRadianceXD = GlossyEnvironmentReflection(reflect(-cameraDirection, surfaceNormal), perceptualRoughness, 1);
        float G = 1 / (roughness2 + 1.0);
        float F = pow(1 - vDotN, 4);
        float maxSpecular = saturate(smoothness + lerp(0.04, 1, metallic));
        light += specularRadianceXD * lerp(specular, maxSpecular, F) * G;
    }


    return light;
}

float3 CookTorrance_Unity(float3 albedo, float metallic, float smoothness, float3 surfaceNormal, float3 cameraDirection,
                          float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount], int lightCount)
{
    float3 diffuse = lerp(0.94, 0, metallic) * albedo; //漫射光反射率
    float3 specular = lerp(0.04, albedo, metallic); //镜射光反射率

    float perceptualRoughness = 1 - smoothness;
    float roughness = max(pow(1 - smoothness, 2),HALF_MIN_SQRT);
    float roughness2 = roughness * roughness;

    float3 light = 0;
    //直接光
    for (int i = 0; i < lightCount; ++i)
    {
        //镜射光
        float3 halfway = normalize(lightDirections[i] + cameraDirection);
        float nDotH = saturate(dot(surfaceNormal, halfway));
        float lDotH = saturate(dot(lightDirections[i], halfway));
        float specularTerm = NormalDistribution_UnityGGX(nDotH, roughness2) * GeometricAttenuationAndFresnel_Unity(lDotH, roughness) / 4;

        //辐射率
        float nDotL = saturate(dot(surfaceNormal, lightDirections[i]));
        float3 radiance = lightIntensities[i] * nDotL;

        light += radiance * (diffuse + specular * specularTerm);
    }

    //间接光
    light += GlobalIlluminationDiffuse_Unity(surfaceNormal, diffuse);
    light += GlobalIlluminationSpecular_Unity(surfaceNormal, cameraDirection, specular, metallic, smoothness);

    return light;
}