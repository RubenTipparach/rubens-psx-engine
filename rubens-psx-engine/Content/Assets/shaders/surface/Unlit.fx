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
float3 CameraPosition;
Texture2D HeightmapTexture;
Texture2D NormalMapTexture;

// PS1-style parameters (kept for compatibility)
float VertexJitterAmount = 30.0;
float AffineAmount = 0.0;
bool EnableAffineMapping = true;
float Brightness = 1.0;

// Enhanced lighting parameters
float3 DirectionalLightDirection = float3(1, -1, -1);
float3 DirectionalLightColor = float3(1.0, 0.95, 0.8);  // Warm sunlight
float DirectionalLightIntensity = 1.0;
float3 AmbientLightColor = float3(0.2, 0.3, 0.4);       // Cool ambient
float AmbientLightIntensity = 0.3;
float SpecularPower = 16.0;
float SpecularIntensity = 0.1;

sampler HeightmapSampler = sampler_state
{
    Texture = <HeightmapTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = NONE;
    AddressU = WRAP;
    AddressV = WRAP;
};

sampler NormalMapSampler = sampler_state
{
    Texture = <NormalMapTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
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
    float4 Color : COLOR0;
    float2 AffineTexCoord : TEXCOORD1;
    float InvW : TEXCOORD2;
    float3 WorldPosition : TEXCOORD3;
    float3 WorldNormal : TEXCOORD4;
};

// Terrain gradient function - interpolates between terrain colors based on height
float3 GetTerrainColor(float height)
{
    // Terrain gradient key colors for height-based interpolation
    float3 beachColor = float3(0.96, 0.64, 0.38);      // SandyBrown
    float3 lowlandColor = float3(0.0, 0.5, 0.0);       // Green
    float3 midlandColor = float3(0.34, 0.68, 0.16);    // ForestGreen
    float3 highlandColor = float3(0.54, 0.27, 0.07);   // SaddleBrown
    float3 mountainColor = float3(0.41, 0.41, 0.41);   // DimGray
    float3 peakColor = float3(1.0, 1.0, 1.0);          // White

    // Define height thresholds for color transitions
    float beachThreshold = 0.1;
    float lowlandThreshold = 0.25;
    float midlandThreshold = 0.5;
    float highlandThreshold = 0.7;
    float mountainThreshold = 0.85;

    float3 color;

    if (height < beachThreshold)
    {
        // Beach to lowland transition
        float t = height / beachThreshold;
        color = lerp(beachColor, lowlandColor, t);
    }
    else if (height < lowlandThreshold)
    {
        // Lowland to midland transition
        float t = (height - beachThreshold) / (lowlandThreshold - beachThreshold);
        color = lerp(lowlandColor, midlandColor, t);
    }
    else if (height < midlandThreshold)
    {
        // Midland to highland transition
        float t = (height - lowlandThreshold) / (midlandThreshold - lowlandThreshold);
        color = lerp(midlandColor, highlandColor, t);
    }
    else if (height < highlandThreshold)
    {
        // Highland to mountain transition
        float t = (height - midlandThreshold) / (highlandThreshold - midlandThreshold);
        color = lerp(highlandColor, mountainColor, t);
    }
    else if (height < mountainThreshold)
    {
        // Mountain to peak transition
        float t = (height - highlandThreshold) / (mountainThreshold - highlandThreshold);
        color = lerp(mountainColor, peakColor, t);
    }
    else
    {
        // Pure peak color
        color = peakColor;
    }

    return color;
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.AffineTexCoord = input.TexCoord * output.Position.w;
    output.InvW = 1.0 / output.Position.w;

    // Pass world position and transformed normal for lighting calculations
    output.WorldPosition = worldPosition.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));

    output.Color = float4(0.4, 0.7, 0.3, 1.0); // Default green planet color

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample heightmap for height-based coloring
    float4 heightSample = tex2D(HeightmapSampler, input.TexCoord);
    float height = heightSample.r;

    // Get terrain color based on height
    float3 terrainColor = GetTerrainColor(height);

    // Sample normal map and blend with vertex normal
    float4 normalMapSample = tex2D(NormalMapSampler, input.TexCoord);
    float3 normalMapNormal = normalize((normalMapSample.rgb * 2.0 - 1.0)); // Convert from [0,1] to [-1,1]

    // Blend vertex normal with normal map for enhanced detail
    float3 vertexNormal = normalize(input.WorldNormal);
    float3 normal = normalize(lerp(vertexNormal, normalMapNormal, 0.5)); // 50% blend

    float3 lightDir = normalize(-DirectionalLightDirection);
    float3 viewDir = normalize(CameraPosition - input.WorldPosition);

    // Calculate diffuse lighting
    float NdotL = max(0.0, dot(normal, lightDir));
    float3 diffuse = DirectionalLightColor * DirectionalLightIntensity * NdotL;

    // Calculate specular lighting (Blinn-Phong)
    float3 halfwayDir = normalize(lightDir + viewDir);
    float NdotH = max(0.0, dot(normal, halfwayDir));
    float3 specular = DirectionalLightColor * SpecularIntensity * pow(NdotH, SpecularPower);

    // Add ambient lighting
    float3 ambient = AmbientLightColor * AmbientLightIntensity;

    // Combine lighting components
    float3 lighting = ambient + diffuse + specular;
    float3 finalColor = terrainColor * lighting;

    return float4(finalColor * Brightness, 1.0);
}

technique Unlit
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}