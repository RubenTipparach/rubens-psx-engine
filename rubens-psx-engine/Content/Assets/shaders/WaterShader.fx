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

// Water parameters
float Time = 0.0;
float WaveSpeed = 1.0;
float WaveStrength = 0.02;
float3 WaterColor = float3(0.1, 0.4, 0.8);
float WaterAlpha = 0.7;
float3 FoamColor = float3(0.9, 0.95, 1.0);

// User-controllable water parameters
float WaveUVScale = 1.0;        // Scale of UV for wave patterns
float WaveFrequency = 1.0;      // Frequency multiplier for waves
float WaveAmplitude = 1.0;      // Amplitude multiplier for waves
float WaveNormalStrength = 1.0; // Strength of normal map effect
float WaveDistortion = 1.0;     // Distortion intensity of water surface
float WaveScrollSpeed = 1.0;    // Speed of wave pattern movement

// Simple noise function using sin waves instead of complex voronoi
float simpleNoise(float2 p)
{
    return sin(p.x * 6.2831) * sin(p.y * 6.2831);
}

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
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform position to world space
    float4 worldPos = mul(input.Position, World);

    // Apply UV scale to wave calculations using texture coordinates
    float2 scaledUV = input.TexCoord * WaveUVScale;

    // Simple wave displacement using sin waves with user controls
    float wave1 = sin(scaledUV.x * 20.0 * WaveFrequency + Time * WaveSpeed * WaveScrollSpeed) * 0.5;
    float wave2 = sin(scaledUV.y * 15.0 * WaveFrequency - Time * WaveSpeed * WaveScrollSpeed * 0.7) * 0.3;

    // Apply wave displacement with amplitude control
    worldPos.y += (wave1 + wave2) * WaveStrength * WaveAmplitude;

    float4 viewPosition = mul(worldPos, View);
    output.Position = mul(viewPosition, Projection);

    output.WorldPosition = worldPos.xyz;
    output.Normal = normalize(mul(input.Normal, (float3x3)World));
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Apply UV scale to texture coordinates
    float2 scaledUV = input.TexCoord * WaveUVScale;

    // Simple animated distortion using sin waves with user control
    float2 distortUV = scaledUV + Time * 0.1 * WaveScrollSpeed;
    float distortion = sin(distortUV.x * 10.0 * WaveFrequency) *
                       sin(distortUV.y * 8.0 * WaveFrequency) * 0.02 * WaveDistortion;

    // Create simple ripple normals with user-controlled strength
    float normalSample = sin(scaledUV.x * 20.0 * WaveFrequency + Time * 2.0 * WaveScrollSpeed) *
                        sin(scaledUV.y * 15.0 * WaveFrequency - Time * 1.5 * WaveScrollSpeed) * WaveNormalStrength;

    // Simple foam using noise with scroll speed
    float foam = simpleNoise(scaledUV * 8.0 + Time * WaveScrollSpeed);
    foam = saturate((foam + 0.5) * 0.5); // Normalize to 0-1

    // Simple lighting
    float3 lightDir = normalize(float3(0.7, -0.7, 0.0));
    float NdotL = max(0.3, dot(input.Normal, lightDir));

    // Simple fresnel
    float3 viewDir = normalize(input.WorldPosition);
    float fresnel = 1.0 - abs(dot(input.Normal, viewDir));

    // Depth fog (simplified)
    float depth = length(input.WorldPosition) * 0.1;
    float depthFog = saturate(depth);

    // Combine colors
    float3 waterSurface = lerp(WaterColor, FoamColor, foam * 0.2);
    waterSurface *= NdotL;

    // Final alpha
    float finalAlpha = WaterAlpha + fresnel * 0.2 + depthFog * 0.1;

    return float4(waterSurface, saturate(finalAlpha));
}

technique Water
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}