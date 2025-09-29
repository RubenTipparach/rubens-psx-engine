matrix World;
matrix View;
matrix Projection;
matrix WorldInverseTranspose;
float3 CameraPosition;
Texture2D HeightmapTexture;

float Brightness = 1.0;

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
    float4 Color : COLOR0;
    float2 AffineTexCoord : TEXCOORD1;
    float InvW : TEXCOORD2;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.AffineTexCoord = input.TexCoord * output.Position.w;
    output.InvW = 1.0 / output.Position.w;

    output.Color = float4(0.4, 0.7, 0.3, 1.0); // Green planet color

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    return input.Color;
}

technique Planet
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VS();
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}