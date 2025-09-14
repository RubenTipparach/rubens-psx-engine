matrix World;
matrix View;
matrix Projection;
Texture2D Texture;

// PS1-style parameters
float VertexJitterAmount = 30.0; // Higher = more jitter, try 64.0 or 32.0 for strong PS1 effect
float AffineAmount = 0.0; // 0.0 = perspective correct, 1.0 = full affine mapping
bool EnableAffineMapping = true;
float Brightness = 1.0; // Color brightness multiplier, 1.0 = normal, > 1.0 = brighter, < 1.0 = darker

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
    
    output.Color = float4(1.0, 1.0, 1.0, 1.0); // Default white color
    
    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    float4 TintColor = float4(1.0f, 1.0f, 1.0f, 1.0f);
    
    float2 texCoord;
    if (EnableAffineMapping)
    {
        // Affine texture mapping: reconstruct non-perspective-correct texture coordinates
        float2 affineCoord = input.AffineTexCoord * input.InvW;
        texCoord = lerp(input.TexCoord, affineCoord, AffineAmount);
    }
    else
    {
        texCoord = input.TexCoord;
    }

    float4 finalColor = tex2D(TextureSampler, texCoord) * TintColor;
    finalColor.rgb *= Brightness; // Apply brightness to RGB channels, leave alpha unchanged
    return finalColor;
}

technique Unlit
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}