// Pixel shader extracts the brighter areas of an image.
// This is the first step in applying a bloom postprocess.

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

float BloomThreshold;

struct VSOutput
{
	float4 position		: SV_Position;
	float4 color		: COLOR;
	float2 texCoord		: TEXCOORD;
};


float4 PixelShaderFunction(VSOutput input) : COLOR
{    
    float4 c = tex2D(SpriteTextureSampler, input.texCoord) * input.color; // Look up the original image color.    
	
	// Adjust it to keep only values brighter than the specified threshold.
    return saturate((c - BloomThreshold) / (1 - BloomThreshold)); 
}


technique BloomExtract
{
    pass p0
    {
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
}
