// Emissive lit shader for glowing ship windows/lights
matrix World;
matrix View;
matrix Projection;

Texture2D Texture;          // Main texture
Texture2D EmissiveMap;      // Emissive/glow map (if available)

float3 LightDirection = float3(0.0, -1.0, 0.5); // Directional light
float4 LightColor = float4(1.0, 1.0, 1.0, 1.0);
float AmbientIntensity = 0.3;
float EmissiveStrength = 2.0;  // How bright the emissive parts glow

bool UseEmissiveMap = false;    // Whether we have a separate emissive texture

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = NONE;
    AddressU = WRAP;
    AddressV = WRAP;
};

sampler EmissiveSampler = sampler_state
{
    Texture = <EmissiveMap>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
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
    float3 Normal : TEXCOORD1;
    float3 WorldPos : TEXCOORD2;
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.Normal = normalize(mul(input.Normal, (float3x3)World));
    output.WorldPos = worldPosition.xyz;

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample textures
    float4 texColor = tex2D(TextureSampler, input.TexCoord);

    // Calculate lighting
    float3 normal = normalize(input.Normal);
    float3 lightDir = normalize(-LightDirection);
    float diffuse = max(dot(normal, lightDir), 0.0);

    // Ambient + diffuse
    float3 lighting = (AmbientIntensity + diffuse) * LightColor.rgb;
    float3 litColor = texColor.rgb * lighting;

    // Add emissive
    float3 emissive = float3(0, 0, 0);
    if (UseEmissiveMap)
    {
        // Use separate emissive texture
        float4 emissiveColor = tex2D(EmissiveSampler, input.TexCoord);
        emissive = emissiveColor.rgb * EmissiveStrength;
    }
    else
    {
        // Use bright areas of main texture as emissive
        // Bright pixels (> 0.7) will glow
        float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
        if (brightness > 0.7)
        {
            emissive = texColor.rgb * EmissiveStrength * (brightness - 0.7) * 3.0;
        }
    }

    float3 finalColor = litColor + emissive;

    return float4(finalColor, texColor.a);
}

technique EmissiveLit
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}
