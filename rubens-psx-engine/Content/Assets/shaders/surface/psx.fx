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

float4x4 WorldViewProj()
{
    return mul(mul(World, View), Projection);
}

// stanadard PS
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float4 Normal : NORMAL0;
    float4 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
    float4 WorldPosition : TEXCOORD1;
    float4 Normal : TEXCOORD2;
};

// custom values
float Time = 1; // passed from C#
float2 ScreenSize = float2(800, 600); // your screen resolution

// Jitter strength in pixels (converted to clip space)
float VertexJitterAmount = 1.0;

texture Texture;
sampler2D TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = POINT; // Nearest-neighbor
    MagFilter = POINT;
    MipFilter = NONE;
    AddressU = WRAP;
    AddressV = WRAP;
};

float random(float2 pos)
{
    return frac(sin(dot(pos, float2(12.9898, 78.233))) * 43758.5453);
}

// functions
VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	
    // Add screen-space jitter
    float2 ndcJitter = VertexJitterAmount / ScreenSize;
    float2 jitter = ndcJitter * (random(input.Position.xy + Time) - 0.5);

    input.Position.xy += jitter;

    //output.Position = input.Position;
    //output.TexCoord = input.TexCoord;
	
    output.Position = mul(input.Position, WorldViewProj());

    //output.TexCoord = input.TexCoord;
    
	output.Color = input.Color;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(TextureSampler, input.TexCoord) * input.Color;

    // Optional quantization for PS1 color banding
    float levels = 8.0;
    color.rgb = floor(color.rgb * levels) / (levels - 1.0);
	
	return input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};