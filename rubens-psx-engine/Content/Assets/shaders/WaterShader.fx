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

// Water animation parameters
float Time = 0.0;
float WaveHeight = 0.02f;
float WaveFrequency = 15.0f;
float WaveSpeed = 2.0f;
float WaterTransparency = 0.7f;
float3 WaterColor = float3(0.1, 0.3, 0.8);
float3 FoamColor = float3(0.9, 0.95, 1.0);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    float4 Color : COLOR0;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Add wave animation to vertex position
    float4 worldPos = mul(input.Position, World);

    // Calculate wave displacement
    float waveTime = Time * WaveSpeed;
    float waveX = sin(worldPos.x * WaveFrequency + waveTime) * WaveHeight;
    float waveZ = cos(worldPos.z * WaveFrequency + waveTime * 0.7) * WaveHeight;

    // Apply wave displacement
    worldPos.y += waveX + waveZ;

    // Calculate new normal for lighting (approximate)
    float3 waveNormal = normalize(float3(-waveX * WaveFrequency, 1.0, -waveZ * WaveFrequency));

    float4 viewPosition = mul(worldPos, View);
    output.Position = mul(viewPosition, Projection);

    output.WorldPosition = worldPos.xyz;
    output.Normal = normalize(mul(waveNormal, (float3x3)World));
    output.TexCoord = input.TexCoord;
    output.Color = float4(WaterColor, WaterTransparency);

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Calculate simple foam based on wave height and time
    float foamTime = Time * WaveSpeed * 2.0;
    float foamNoise = sin(input.TexCoord.x * 50.0 + foamTime) * cos(input.TexCoord.y * 50.0 + foamTime * 0.8);
    float foamMask = saturate(foamNoise * 0.5 + 0.5);

    // Add subtle animation to foam
    float animatedFoam = foamMask * (0.5 + 0.5 * sin(foamTime * 3.0));

    // Mix water color with foam
    float3 finalColor = lerp(WaterColor, FoamColor, animatedFoam * 0.3);

    // Calculate transparency based on viewing angle (Fresnel-like effect)
    float3 viewDir = normalize(input.WorldPosition - input.WorldPosition); // Simplified
    float fresnel = 1.0 - saturate(dot(input.Normal, float3(0, 1, 0)));
    float finalAlpha = WaterTransparency + (1.0 - WaterTransparency) * fresnel * 0.5;

    return float4(finalColor, finalAlpha);
}

technique Water
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}