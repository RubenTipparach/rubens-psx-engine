matrix World;
matrix View;
matrix Projection;
Texture2D Texture;

// PS1-style parameters
float VertexJitterAmount = 2.0; // Higher = more jitter, try 64.0 or 32.0 for strong PS1 effect
float AffineAmount = 0.0; // 0.0 = perspective correct, 1.0 = full affine mapping
bool EnableAffineMapping = true;

// Lighting parameters
float3 LightDirection = float3(0, -1, 0); // Directional light direction
float3 LightColor = float3(1, 1, 1); // Light color
float3 AmbientColor = float3(0.2, 0.2, 0.2); // Ambient light color
float LightIntensity = 1.0; // Light intensity multiplier

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = POINT; // Nearest-neighbor
    MagFilter = POINT;
    MipFilter = NONE;
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
    float2 AffineTexCoord : TEXCOORD1;
    float InvW : TEXCOORD2;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);

    // --- PS1-style vertex jitter ---
    // Simulate fixed-point precision loss by snapping view position
    float quantizeAmount = 1.0 / VertexJitterAmount;
    viewPosition.xyz = floor(viewPosition.xyz / quantizeAmount + 0.5) * quantizeAmount;

    output.Position = mul(viewPosition, Projection);
    
    // Store both perspective-correct and affine texture coordinates
    output.TexCoord = input.TexCoord;
    
    // For affine mapping, we need to scale texture coords by w (before perspective divide)
    // and store 1/w to restore them in the pixel shader
    output.AffineTexCoord = input.TexCoord * output.Position.w;
    output.InvW = 1.0 / output.Position.w;
    
    // --- Vertex Lighting Calculation ---
    // Transform normal to world space
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)World));
    
    // Calculate directional lighting
    float NdotL = max(0, dot(-LightDirection, worldNormal));
    float3 diffuse = LightColor * NdotL * LightIntensity;
    
    // Combine ambient and diffuse lighting
    float3 lighting = AmbientColor + diffuse;
    
    // Clamp lighting to reasonable values
    lighting = saturate(lighting);
    
    output.Color = float4(lighting, 1.0);
    
    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    float2 texCoord;
    
    // Affine texture mapping: reconstruct non-perspective-correct texture coordinates
    //if (EnableAffineMapping)
    //{
    //    float2 affineCoord = input.AffineTexCoord * input.InvW;
    //    texCoord = lerp(input.TexCoord, affineCoord, AffineAmount);
    //}
    //else
    //{
        texCoord = input.TexCoord;
    //}

    // Sample texture and apply vertex lighting
    float4 texColor = tex2D(TextureSampler, texCoord);
    return texColor * input.Color;
}

technique VertexLit
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}