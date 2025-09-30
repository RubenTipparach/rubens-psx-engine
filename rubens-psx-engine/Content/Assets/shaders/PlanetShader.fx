#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;

Texture2D HeightmapTexture;

// Simple directional light parameters (simulates sun light)
float3 SunDirection = float3(0.707, -0.707, 0.0);  // 45 degree angle
float3 SunColor = float3(1.0, 0.95, 0.8);         // Warm sunlight

sampler HeightmapSampler = sampler_state
{
    Texture = <HeightmapTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = NONE;
    AddressU = WRAP;
    AddressV = WRAP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float3 WorldNormal : TEXCOORD1;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample heightmap for height-based coloring
    float height = tex2D(HeightmapSampler, input.TexCoord).r;

    // Simple height-based color using smooth lerp (no conditionals)
    float3 lowColor = float3(0.0, 0.5, 0.0);    // Green
    float3 highColor = float3(1.0, 1.0, 1.0);   // White
    float3 terrainColor = lerp(lowColor, highColor, height);

    // Simple directional lighting
    float3 normal = normalize(input.WorldNormal);
    float3 lightDir = normalize(-SunDirection);
    float NdotL = max(0.0, dot(normal, lightDir));

    // Apply lighting
    float3 finalColor = terrainColor * (0.3 + 0.7 * NdotL);

    return float4(finalColor, 1.0);
}

technique Planet
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}