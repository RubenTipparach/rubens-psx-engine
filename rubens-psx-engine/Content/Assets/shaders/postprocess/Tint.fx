#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	//	Next we'll multiply the pixelColor by the tint color that was passed int
    float4 TintColor = float4(1.0f, 1.0f, 1.0f, 1.0f);;//float4(0.9f, 0.9f, 0.9f, 1.0f);;
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float4 tintedPixelColor = color * TintColor;
    return tintedPixelColor;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};