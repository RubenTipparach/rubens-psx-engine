matrix World;
matrix View;
matrix Projection;
Texture2D Texture;

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

};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;
    //float4 worldPosition = mul(input.Position, World);
    //float4 viewPosition = mul(worldPosition, View);
    //output.Position = mul(viewPosition, Projection);
    //output.TexCoord = input.TexCoord;
    
   
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);

    // --- PS1-style vertex jitter ---
    // Simulate fixed-point precision loss by snapping view position
    float quantizeAmount = 1.0 / 2.0; // lower = more jitter, try 1/64 or 1/32
    viewPosition.xyz = floor(viewPosition.xyz / quantizeAmount + 0.5) * quantizeAmount;

    output.Position = mul(viewPosition, Projection);
    output.TexCoord = input.TexCoord;
    
    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0 //SV_Target
{
    float4 TintColor = float4(1.0f, 1.0f, 1.0f, 1.0f);;

    return Texture.Sample(TextureSampler, input.TexCoord) * TintColor;
}

technique Unlit
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}