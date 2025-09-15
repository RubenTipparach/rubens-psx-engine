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
    MinFilter = POINT; // Nearest-neighbor
    MagFilter = POINT;
    MipFilter = NONE;

};
float2 ScreenSize = float2(320, 180); // Set from C#
float DitherStrength = 1.0; // Set from C#
float ColorLevels = 6.0; // Set from C#

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 TintColor = float4(1.0f, 0.5f, 0.1f, 1.0f);;

	//	First we use the tex2D function to get the color of the pixel
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

	//	Next we'll multiply the pixelColor by the tint color that was passed int
    //float4 tintedPixelColor = color * TintColor;
    //return tintedPixelColor;
    
    
        // Calculate screen-space pixel position
    float2 screenPos = input.TextureCoordinates * ScreenSize;
    int x = (int) floor(screenPos.x) % 4;
    int y = (int) floor(screenPos.y) % 4;

    // Bayer 4x4 matrix (0 to 1 scale)
    float bayer4x4[16] =
    {
        0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
        12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
        3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
        15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
    };

    float threshold = bayer4x4[y * 4 + x];

    // Quantize to fixed levels (use ColorLevels from C#)
    color.rgb = floor(color.rgb * ColorLevels + threshold * DitherStrength) / (ColorLevels - 1.0);


	//	And we return the value
    return color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};