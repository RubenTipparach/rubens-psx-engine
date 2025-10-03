#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float3 CameraPosition;

// Planet parameters for depth calculation
float3 PlanetCenter = float3(0, 0, 0);
float PlanetRadius = 50.0;

// Sun direction for lighting
float3 SunDirection = float3(0.0, 0.5, 0.866);

// Advanced water parameters
float Time = 0.0;
float WaveSpeed = 1.0;
float WaveHeight = 0.05; // Reduced for small-scale waves only

// Deep/shallow water colors
float3 ShallowWaterColor = float3(0.0, 0.6, 0.7);   // Turquoise
float3 DeepWaterColor = float3(0.0, 0.1, 0.3);      // Deep blue
float3 ScatterColor = float3(0.0, 0.4, 0.3);        // Underwater light scattering
float3 FoamColor = float3(0.9, 0.95, 1.0);

// Optical properties
float WaterClarity = 15.0;        // How far you can see through water
float Refraction = 0.02;          // Refraction strength
float ReflectionStrength = 0.8;   // Surface reflection intensity
float SpecularPower = 128.0;      // Specular highlight sharpness
float SpecularIntensity = 2.0;    // Specular brightness

// Wave parameters
float WaveUVScale = 1.0;
float WaveFrequency = 1.0;
float WaveAmplitude = 1.0;
float WaveNormalStrength = 1.0;
float WaveDistortion = 1.0;
float WaveScrollSpeed = 1.0;

// Foam parameters
float FoamAmount = 0.3;
float FoamCutoff = 0.6;
float FoamEdgeDistance = 2.0;     // Distance for foam near terrain

// Subsurface scattering
float SubsurfaceStrength = 0.8;
float3 SubsurfaceColor = float3(0.0, 0.8, 0.6);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    float3 ViewDirection : TEXCOORD3;
    float4 ScreenPosition : TEXCOORD4;
    float WaterDepth : TEXCOORD5;
};

// 3D Hash for procedural noise
float3 hash3(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

// 3D Perlin-style noise
float noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(lerp(hash3(i).x, hash3(i + float3(1,0,0)).x, f.x),
             lerp(hash3(i + float3(0,1,0)).x, hash3(i + float3(1,1,0)).x, f.x), f.y),
        lerp(lerp(hash3(i + float3(0,0,1)).x, hash3(i + float3(1,0,1)).x, f.x),
             lerp(hash3(i + float3(0,1,1)).x, hash3(i + float3(1,1,1)).x, f.x), f.y), f.z);
}

// Fractal Brownian Motion for natural-looking waves
float fbm(float3 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for (int i = 0; i < 6; i++)
    {
        if (i >= octaves) break;
        value += amplitude * noise3D(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Gerstner wave function for realistic ocean waves
float3 GerstnerWave(float3 pos, float2 direction, float wavelength, float steepness, float time)
{
    float k = 2.0 * 3.14159 / wavelength;
    float c = sqrt(9.8 / k);
    float2 d = normalize(direction);
    float f = k * (dot(d, pos.xz) - c * time);
    float a = steepness / k;

    return float3(
        d.x * a * cos(f),
        a * sin(f),
        d.y * a * cos(f)
    );
}

// Calculate water depth below a point
float CalculateWaterDepth(float3 worldPos)
{
    // Distance from planet center
    float distFromCenter = length(worldPos - PlanetCenter);

    // Water is at PlanetRadius, so depth is how far below the surface
    float depth = max(0.0, distFromCenter - PlanetRadius);

    return depth;
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform to world space
    float4 worldPos = mul(input.Position, World);
    float3 baseWorldPos = worldPos.xyz;

    // Calculate distance from camera for LOD
    float distanceToCamera = length(baseWorldPos - CameraPosition);
    float waveLOD = saturate(distanceToCamera / 50.0); // Fade out waves at distance

    // Only small-scale ripples (no large planetary waves)
    float3 waveOffset = float3(0, 0, 0);

    // Small ripples only (visible when close)
    float ripples = fbm(baseWorldPos * 4.0 + Time * 0.3 * WaveScrollSpeed, 3) * (1.0 - waveLOD);
    waveOffset.y = ripples * WaveHeight * WaveAmplitude;

    // Apply wave displacement
    worldPos.xyz += waveOffset;

    // Calculate view direction
    output.ViewDirection = normalize(CameraPosition - worldPos.xyz);

    // Transform to clip space
    float4 viewPosition = mul(worldPos, View);
    output.Position = mul(viewPosition, Projection);

    // Pass through data
    output.WorldPosition = worldPos.xyz;
    output.Normal = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));
    output.TexCoord = input.TexCoord;
    output.ScreenPosition = output.Position;

    // Calculate water depth for this vertex
    output.WaterDepth = CalculateWaterDepth(worldPos.xyz);

    return output;
}

// Calculate detailed water normal from noise
float3 CalculateWaterNormal(float3 worldPos, float2 uv, float time)
{
    float2 scaledUV = uv * WaveUVScale;

    // Sample noise at offset positions for normal calculation
    float epsilon = 0.1;

    float3 pos = worldPos * 0.5 + float3(time * 0.2 * WaveScrollSpeed, 0, 0);

    float h0 = fbm(pos, 4);
    float hX = fbm(pos + float3(epsilon, 0, 0), 4);
    float hZ = fbm(pos + float3(0, 0, epsilon), 4);

    // Calculate gradient
    float3 normal;
    normal.x = (h0 - hX) * WaveNormalStrength;
    normal.z = (h0 - hZ) * WaveNormalStrength;
    normal.y = 1.0;

    return normalize(normal);
}

// Schlick's Fresnel approximation
float FresnelSchlick(float cosTheta, float F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Calculate detailed normal from noise
    float3 detailNormal = CalculateWaterNormal(input.WorldPosition, input.TexCoord, Time);
    float3 normal = normalize(input.Normal + detailNormal - float3(0, 1, 0));

    // View and light directions
    float3 viewDir = normalize(input.ViewDirection);
    float3 lightDir = normalize(SunDirection);
    float3 halfDir = normalize(viewDir + lightDir);

    // Depth-based color
    float depth = input.WaterDepth;
    float depthFactor = 1.0 - exp(-depth / WaterClarity);
    float3 waterColor = lerp(ShallowWaterColor, DeepWaterColor, depthFactor);

    // Fresnel effect
    float NdotV = max(0.0, dot(normal, viewDir));
    float fresnel = FresnelSchlick(NdotV, 0.02); // Water F0 ≈ 0.02

    // Specular highlight (sun reflection)
    float NdotH = max(0.0, dot(normal, halfDir));
    float specular = pow(NdotH, SpecularPower) * SpecularIntensity;
    float3 specularColor = float3(1.0, 0.98, 0.95) * specular;

    // Diffuse lighting
    float NdotL = max(0.0, dot(normal, lightDir));
    float3 diffuse = waterColor * NdotL * 0.5;

    // Subsurface scattering (light passing through waves)
    float3 subsurface = float3(0, 0, 0);
    float backside = max(0.0, dot(-normal, lightDir));
    subsurface = SubsurfaceColor * backside * SubsurfaceStrength;

    // Scatter color (underwater light scattering effect)
    float3 scatter = ScatterColor * (1.0 - depthFactor) * 0.3;

    // Shore foam calculation - appears near terrain (shallow water)
    float shoreFoam = saturate(1.0 - depth / FoamEdgeDistance);

    // Multi-layered foam noise for better looking shore foam
    float foamNoise1 = fbm(input.WorldPosition * 6.0 + Time * 0.2 * WaveScrollSpeed, 3);
    float foamNoise2 = fbm(input.WorldPosition * 12.0 - Time * 0.4 * WaveScrollSpeed, 2);
    float combinedFoamNoise = foamNoise1 * 0.6 + foamNoise2 * 0.4;

    // Create animated foam line at shoreline
    float foamLine = saturate((shoreFoam - 0.3) / 0.7); // Sharp transition near shore
    float foamMask = foamLine * saturate(combinedFoamNoise + 0.3);
    foamMask = smoothstep(0.3, 0.8, foamMask);

    float3 foam = FoamColor * foamMask;

    // Sky reflection (simplified - would be better with cubemap)
    float3 skyColor = lerp(float3(0.5, 0.7, 0.9), float3(0.1, 0.3, 0.6), depthFactor);
    float3 reflection = skyColor * fresnel * ReflectionStrength;

    // Combine all lighting components
    float3 finalColor = diffuse + scatter + subsurface + reflection + specularColor + foam;

    // Distance fog for atmospheric perspective
    float distanceToCamera = length(input.WorldPosition - CameraPosition);
    float distanceFog = saturate(distanceToCamera * 0.005);
    float3 fogColor = float3(0.6, 0.8, 0.9);
    finalColor = lerp(finalColor, fogColor, distanceFog * 0.3);

    // Alpha based on depth and fresnel
    float alpha = saturate(0.85 + fresnel * 0.15 + depthFactor * 0.1 - foamMask * 0.3);

    return float4(finalColor, alpha);
}

technique Water
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
