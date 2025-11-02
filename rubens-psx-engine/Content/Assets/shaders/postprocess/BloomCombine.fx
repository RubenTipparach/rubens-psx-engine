// Pixel shader combines the bloom image with the original
// scene, using tweakable intensity levels and saturation.
// This is the final step in applying a bloom postprocess.

sampler BloomSampler : register(s0);
sampler BaseSampler : register(s1)
{
	Texture = (BaseTexture); // <--- must use SetValue for BaseTexture
	Filter = Linear;
	AddressU = clamp;
	AddressV = clamp;
};

float BloomIntensity;
float BaseIntensity;
float BloomSaturation;
float BaseSaturation;

struct VSOutput 
{
	float4 position		: SV_Position;
	float4 color		: COLOR0;
	float2 texCoord		: TEXCOORD;
};


// Helper for modifying the saturation of a color.
float4 AdjustSaturation(float4 color, float saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    float grey = dot(color, float3(0.3, 0.59, 0.11));
    return lerp(grey, color, saturation);
}


float4 PixelShaderFunction(VSOutput input) : COLOR
{
    // Look up the bloom and original base image colors.
    float4 bloom = tex2D(BloomSampler, input.texCoord);
	float4 base = tex2D(BaseSampler, input.texCoord);
    
    // Adjust color saturation and intensity.
    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;

    // NOTE: Removed darkening of base to preserve dithering when bloom is applied after
    // Original code: base *= (1 - saturate(bloom));

    // Combine the two images with simple additive blending.
    return base + bloom;
}


technique BloomCombine
{
    pass Pass1
    {
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
}
