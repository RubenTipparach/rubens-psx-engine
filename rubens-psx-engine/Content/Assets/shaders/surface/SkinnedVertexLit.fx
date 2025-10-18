matrix World;
matrix View;
matrix Projection;
Texture2D Texture;

#define MAX_BONES 72

// Bone transforms for skinning
float4x3 Bones[MAX_BONES];

// Lighting parameters
float3 LightDirection = float3(0, -1, 0); // Directional light direction
float3 LightColor = float3(1, 1, 1); // Light color
float3 AmbientColor = float3(0.5, 0.5, 0.5); // Ambient light color
float3 EmissiveColor = float3(0.0, 0.0, 0.0); // Emissive color (self-illumination)

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = POINT; // Nearest-neighbor for PS1 style
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
    int4 BlendIndices : BLENDINDICES0;
    float4 BlendWeights : BLENDWEIGHT0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

void Skin(inout VertexShaderInput vin, uniform int boneCount)
{
    float4x3 skinning = 0;

    [unroll]
    for (int i = 0; i < boneCount; i++)
    {
        skinning += Bones[vin.BlendIndices[i]] * vin.BlendWeights[i];
    }

    vin.Position.xyz = mul(vin.Position, skinning);
    vin.Normal = mul(vin.Normal, (float3x3)skinning);
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Apply skinning to 4 bones
    Skin(input, 4);

    // Transform position to world space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Pass through texture coordinates
    output.TexCoord = input.TexCoord;

    // --- Vertex Lighting Calculation ---
    // Transform normal to world space
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)World));

    // Calculate directional lighting
    float NdotL = max(0, dot(-LightDirection, worldNormal));
    float3 diffuse = LightColor * NdotL;

    // Combine ambient, diffuse, and emissive lighting
    float3 lighting = AmbientColor + diffuse + EmissiveColor;

    // Clamp lighting to reasonable values
    lighting = saturate(lighting);

    output.Color = float4(lighting, 1.0);

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample texture and apply vertex lighting
    float4 texColor = tex2D(TextureSampler, input.TexCoord);

    // Apply vertex color (lighting) to texture
    return texColor * input.Color;
}

technique SkinnedVertexLit
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}
