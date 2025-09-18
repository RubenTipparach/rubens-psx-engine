matrix World;
matrix View;
matrix Projection;
Texture2D Texture;

// Texture tiling
float2 TextureTiling = float2(10.0, 10.0);

// Lighting parameters
float3 LightDirection = float3(0, -1, 0); // Directional light direction
float3 LightColor = float3(1, 1, 1); // Light color
float3 AmbientColor = float3(0.2, 0.2, 0.2); // Ambient light color
float LightIntensity = 1.0; // Light intensity multiplier

// Point lights (up to 8)
#define MAX_POINT_LIGHTS 8
float3 PointLightPositions[MAX_POINT_LIGHTS];
float3 PointLightColors[MAX_POINT_LIGHTS];
float PointLightRanges[MAX_POINT_LIGHTS];
float PointLightIntensities[MAX_POINT_LIGHTS];
int ActivePointLights = 0;

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
    AddressU = WRAP;
    AddressV = WRAP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float3 WorldPosition : TEXCOORD1;
    float3 WorldNormal : TEXCOORD2;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Apply texture tiling
    output.TexCoord = input.TexCoord * TextureTiling;

    // Store world position and normal for per-pixel point lighting
    output.WorldPosition = worldPosition.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));

    // Calculate vertex lighting (directional light only)
    float3 worldNormal = output.WorldNormal;

    // Calculate directional lighting
    float NdotL = max(0, dot(-LightDirection, worldNormal));
    float3 diffuse = LightColor * NdotL * LightIntensity;

    // Start with ambient and directional light
    float3 lighting = AmbientColor + diffuse;

    // Add point lights contribution (vertex-based calculation)
    for (int i = 0; i < ActivePointLights && i < MAX_POINT_LIGHTS; i++)
    {
        float3 lightDir = normalize(PointLightPositions[i] - worldPosition.xyz);
        float distance = length(PointLightPositions[i] - worldPosition.xyz);
        float attenuation = saturate(1.0 - (distance / PointLightRanges[i]));
        attenuation *= attenuation; // Quadratic falloff

        float pointNdotL = max(0, dot(lightDir, worldNormal));
        lighting += PointLightColors[i] * pointNdotL * PointLightIntensities[i] * attenuation;
    }

    // Clamp lighting to reasonable values
    lighting = saturate(lighting);

    output.Color = float4(lighting, 1.0);

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample texture with tiling applied
    float4 texColor = tex2D(TextureSampler, input.TexCoord);

    // Per-pixel point light calculation for smoother lighting
    float3 lighting = input.Color.rgb;

    // Optional: Add per-pixel point lighting for better quality
    // (Currently using vertex lighting for performance)

    return texColor * float4(lighting, 1.0);
}

technique VertexLitStandard
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}