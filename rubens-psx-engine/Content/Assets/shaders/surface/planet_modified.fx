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
Texture2D HeightmapTexture;
Texture2D NormalMapTexture;

// Atmospheric scattering parameters
float3 PlanetCenter = float3(0, 0, 0);
float PlanetRadius = 50.0;
float AtmosphereRadius = 60.0;
float3 SunDirection = float3(0.0, 0.5, 0.866);

// Self-shadowing parameters
bool EnableSelfShadowing = true;
float ShadowSoftness = 0.5;
int ShadowRaySteps = 16;

// Scattering coefficients (physically based, same as atmosphere shader)
static const float3 BetaRayleigh = float3(5.8e-3, 13.5e-3, 33.1e-3);
static const float BetaMie = 21e-3;
static const float HRayleigh = 8000.0;
static const float HMie = 1200.0;
static const float g = 0.76;

float SunIntensity = 20.0;
float AtmosphereFogIntensity = 0.3; // Reduced for terrain to not overpower surface details

// PS1-style parameters (kept for compatibility)
float VertexJitterAmount = 30.0;
float AffineAmount = 0.0;
bool EnableAffineMapping = true;
float Brightness = 1.0;

// Enhanced lighting parameters
float3 DirectionalLightDirection = float3(1, -1, -1);
float3 DirectionalLightColor = float3(1.0, 0.95, 0.8);  // Warm sunlight
float DirectionalLightIntensity = 1.0;
float3 AmbientLightColor = float3(0.2, 0.3, 0.4);       // Cool ambient
float AmbientLightIntensity = 0.3;
float SpecularPower = 16.0;
float SpecularIntensity = 0.1;

// Normal map parameters
float NormalMapStrength = 0.5;  // 0-1 range for terrain normals (large scale features)
float DetailNormalStrength = 0.3; // 0-1 range for detail normals (fine surface details)
float DayNightTransition = 0.5; // 0 to 1, controls terminator harshness (0=sharp, 1=gradual)

// Detail noise parameters
float NoiseScale = 10.0;        // Scale of the noise pattern
float NoiseStrength = 0.05;     // Strength of the noise displacement
#define NOISE_OCTAVES 4         // Number of octaves for FBM (must be constant for loop unrolling)

// Rendering mode
bool UseVertexColoring = false; // Toggle between height-based terrain colors and simple vertex colors

sampler HeightmapSampler = sampler_state
{
    Texture = <HeightmapTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
    AddressU = WRAP;
    AddressV = WRAP;
};

sampler NormalMapSampler = sampler_state
{
    Texture = <NormalMapTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
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
    float3 WorldPosition : TEXCOORD3;
    float3 WorldNormal : TEXCOORD4;
};

// Terrain gradient function - interpolates between terrain colors based on height
float3 GetTerrainColor(float height)
{
    // Enhanced terrain color palette
    float3 mudColor = float3(0.35, 0.25, 0.15);        // Dark mud (underwater/coast)
    float3 sandColor = float3(0.96, 0.84, 0.66);       // Light sand (narrow beach)
    float3 grassColor = float3(0.2, 0.6, 0.2);         // Vibrant grass
    float3 darkGrassColor = float3(0.15, 0.45, 0.15);  // Darker grass (highlands)
    float3 rockyColor = float3(0.5, 0.45, 0.4);        // Rocky gray-brown
    float3 mountainColor = float3(0.4, 0.4, 0.4);      // Mountain rock
    float3 snowColor = float3(0.95, 0.95, 1.0);        // Snow/ice caps

    // Refined height thresholds for more realistic terrain
    float waterLine = 0.48;         // Below this is mud (underwater)
    float sandStart = 0.48;         // Narrow sand band
    float sandEnd = 0.52;           // End of sand, start of grass
    float grassEnd = 0.65;          // Grass dominates most terrain
    float rockyStart = 0.65;        // Rocky mountainsides begin
    float mountainStart = 0.80;     // High mountains
    float snowStart = 0.90;         // Ice caps on peaks

    float3 color;

    if (height < waterLine)
    {
        // Underwater - mud
        color = mudColor;
    }
    else if (height < sandEnd)
    {
        // Narrow sand beach band
        float t = (height - sandStart) / (sandEnd - sandStart);
        color = lerp(mudColor, sandColor, saturate(t * 2.0)); // Sharp transition
    }
    else if (height < grassEnd)
    {
        // Grass dominates - most of terrain
        float t = (height - sandEnd) / (grassEnd - sandEnd);
        color = lerp(grassColor, darkGrassColor, t);
    }
    else if (height < mountainStart)
    {
        // Rocky mountainsides
        float t = (height - rockyStart) / (mountainStart - rockyStart);
        color = lerp(rockyColor, mountainColor, t);
    }
    else if (height < snowStart)
    {
        // High mountains
        float t = (height - mountainStart) / (snowStart - mountainStart);
        color = lerp(mountainColor, snowColor, t * t); // Gradual snow appearance
    }
    else
    {
        // Ice caps
        color = snowColor;
    }

    return color;
}

// Simplified 3D Hash function for noise
float3 hash(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

// Simplified 3D noise
float noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float3 h0 = hash(i) * 2.0 - 1.0;
    float3 h1 = hash(i + float3(1, 0, 0)) * 2.0 - 1.0;
    float3 h2 = hash(i + float3(0, 1, 0)) * 2.0 - 1.0;
    float3 h3 = hash(i + float3(1, 1, 0)) * 2.0 - 1.0;
    float3 h4 = hash(i + float3(0, 0, 1)) * 2.0 - 1.0;
    float3 h5 = hash(i + float3(1, 0, 1)) * 2.0 - 1.0;
    float3 h6 = hash(i + float3(0, 1, 1)) * 2.0 - 1.0;
    float3 h7 = hash(i + float3(1, 1, 1)) * 2.0 - 1.0;

    return lerp(lerp(lerp(h0.x, h1.x, f.x), lerp(h2.x, h3.x, f.x), f.y),
                lerp(lerp(h4.x, h5.x, f.x), lerp(h6.x, h7.x, f.x), f.y), f.z);
}

// Fractal Brownian Motion for multi-octave detail
float fbm(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for (int i = 0; i < NOISE_OCTAVES; i++)
    {
        value += amplitude * noise3D(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Atmospheric density at height
float DensityRayleighTerrain(float height)
{
    float scaleHeight = (AtmosphereRadius - PlanetRadius) * (HRayleigh / 8000.0);
    return exp(-height / scaleHeight);
}

float DensityMieTerrain(float height)
{
    float scaleHeight = (AtmosphereRadius - PlanetRadius) * (HMie / 8000.0);
    return exp(-height / scaleHeight);
}

// Rayleigh phase function
float PhaseRayleighTerrain(float cosTheta)
{
    return (3.0 / (16.0 * 3.14159)) * (1.0 + cosTheta * cosTheta);
}

// Mie phase function
float PhaseMieTerrain(float cosTheta)
{
    float g2 = g * g;
    float numerator = (1.0 - g2) * (1.0 + cosTheta * cosTheta);
    float denominator = (2.0 + g2) * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5);
    return (3.0 / (8.0 * 3.14159)) * numerator / denominator;
}

// Simplified atmospheric scattering for terrain fog
float3 CalculateAtmosphericFog(float3 worldPos, float3 viewDir)
{
    float3 rayOrigin = CameraPosition;
    float3 rayDir = normalize(worldPos - CameraPosition);
    float rayLength = length(worldPos - CameraPosition);

    // Limit samples for performance
    const int numSamples = 8;
    float3 step = rayDir * (rayLength / float(numSamples));
    float stepLength = length(step);

    float3 pos = rayOrigin + step * 0.5;

    float3 sumRayleigh = float3(0, 0, 0);
    float3 sumMie = float3(0, 0, 0);

    float opticalDepthRayleigh = 0.0;
    float opticalDepthMie = 0.0;

    float cosTheta = dot(rayDir, SunDirection);
    float phaseRayleigh = PhaseRayleighTerrain(cosTheta);
    float phaseMie = PhaseMieTerrain(cosTheta);

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float height = length(pos - PlanetCenter) - PlanetRadius;

        float densityRayleigh = DensityRayleighTerrain(height);
        float densityMie = DensityMieTerrain(height);

        opticalDepthRayleigh += densityRayleigh * stepLength;
        opticalDepthMie += densityMie * stepLength;

        // Simplified light path (approximate)
        float lightHeight = height + 5.0; // Assume sun ray at slightly higher altitude
        float lightDensityRayleigh = DensityRayleighTerrain(lightHeight) * 10.0;
        float lightDensityMie = DensityMieTerrain(lightHeight) * 10.0;

        // Beer's law attenuation
        float3 tau = BetaRayleigh * (opticalDepthRayleigh + lightDensityRayleigh) +
                     BetaMie * (opticalDepthMie + lightDensityMie);
        float3 attenuation = exp(-tau);

        sumRayleigh += attenuation * densityRayleigh * stepLength;
        sumMie += attenuation * densityMie * stepLength;

        pos += step;
    }

    // Apply scattering
    float3 scatteredLight = sumRayleigh * BetaRayleigh * phaseRayleigh +
                           sumMie * BetaMie * phaseMie;

    return scatteredLight * SunIntensity * AtmosphereFogIntensity;
}

// Calculate terrain self-shadowing by ray marching towards the sun
float CalculateTerrainShadow(float3 worldPos, float3 normal)
{
    if (!EnableSelfShadowing)
        return 1.0;

    // Offset start position slightly above surface to avoid self-intersection
    float3 rayOrigin = worldPos + normal * 0.1;
    float3 rayDir = SunDirection;

    float shadow = 1.0;
    float rayStep = 2.0; // Step size for ray marching

    // Ray march towards sun checking terrain height
    [loop]
    for (int i = 0; i < ShadowRaySteps; i++)
    {
        float t = float(i + 1) * rayStep;
        float3 samplePos = rayOrigin + rayDir * t;

        // Get distance from planet center
        float sampleDist = length(samplePos - PlanetCenter);

        // Convert to sphere direction and sample heightmap
        float3 sampleDir = normalize(samplePos - PlanetCenter);

        // Calculate UV for heightmap sampling
        float theta = atan2(sampleDir.x, sampleDir.z);
        float phi = asin(clamp(sampleDir.y, -1.0, 1.0));
        float2 sampleUV = float2(0.5 + theta / (2.0 * 3.14159), 0.5 + phi / 3.14159);

        // Sample terrain height
        float terrainHeight = tex2Dlod(HeightmapSampler, float4(sampleUV, 0, 0)).r;

        // Calculate actual terrain radius at this point
        float heightScale = PlanetRadius * 0.1;
        float terrainRadius = PlanetRadius + (terrainHeight - 0.5) * heightScale * 2.0;

        // Check if ray is below terrain
        if (sampleDist < terrainRadius)
        {
            // In shadow - calculate soft shadow based on how deep
            float depth = terrainRadius - sampleDist;
            shadow = saturate(1.0 - depth / (rayStep * ShadowSoftness));
            break;
        }

        // Early exit if ray escapes atmosphere
        if (sampleDist > AtmosphereRadius * 1.5)
            break;
    }

    return shadow;
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.AffineTexCoord = input.TexCoord * output.Position.w;
    output.InvW = 1.0 / output.Position.w;

    // Pass world position and transformed normal for lighting calculations
    output.WorldPosition = worldPosition.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));

    output.Color = float4(0.4, 0.7, 0.3, 1.0); // Default green planet color

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Sample heightmap for height-based coloring
    float height = tex2D(HeightmapSampler, input.TexCoord).r;

    // Add 3D noise detail to height
    float detailNoise = fbm(input.WorldPosition * NoiseScale);
    float detailedHeight = saturate(height + detailNoise * NoiseStrength);

    // Get terrain color - blend between height-based and vertex color using branchless lerp
    // When UseVertexColoring is false (0), use terrain color. When true (1), use vertex color
    float3 heightBasedColor = GetTerrainColor(height);
    float3 terrainColor = lerp(heightBasedColor, input.Color.rgb, UseVertexColoring ? 1.0 : 0.0);

    // Calculate normals from heightmap using screen-space derivatives

    // Get world-space derivatives of position
    float3 worldDerivativeX = ddx(input.WorldPosition);
    float3 worldDerivativeY = ddy(input.WorldPosition);

    float3 vertexNormal = normalize(input.WorldNormal);
    float3 crossX = cross(vertexNormal, worldDerivativeX);
    float3 crossY = cross(worldDerivativeY, vertexNormal);

    float d = dot(worldDerivativeX, crossY);
    float sgn = d < 0.0 ? -1.0 : 1.0;
    float surface = sgn / max(0.00000001, abs(d));

    // Terrain normals (large scale features from base heightmap)
    float dTerrainHdx = ddx(height);
    float dTerrainHdy = ddy(height);
    float3 terrainGrad = surface * (dTerrainHdx * crossY + dTerrainHdy * crossX);
    float3 terrainNormal = normalize(vertexNormal - NormalMapStrength * terrainGrad);

    // Detail normals (fine surface details from noise)
    float dDetailHdx = ddx(detailedHeight - height); // Only the noise contribution
    float dDetailHdy = ddy(detailedHeight - height);
    float3 detailGrad = surface * (dDetailHdx * crossY + dDetailHdy * crossX);
    float3 detailNormal = normalize(vertexNormal - DetailNormalStrength * detailGrad);

    // Blend terrain and detail normals
    float3 normal = normalize(terrainNormal + detailNormal - vertexNormal);

    float3 lightDir = normalize(-DirectionalLightDirection);
    float3 viewDir = normalize(CameraPosition - input.WorldPosition);

    // Calculate diffuse lighting with day/night transition
    float NdotL = dot(normal, lightDir);
    float lightIntensity = smoothstep(-lerp(0.01, 0.5, DayNightTransition), lerp(0.01, 0.5, DayNightTransition), NdotL);

    // Calculate terrain self-shadowing
    float shadow = CalculateTerrainShadow(input.WorldPosition, normal);

    // Combine ambient and diffuse with shadow
    float3 lighting = AmbientLightColor * AmbientLightIntensity +
                      DirectionalLightColor * DirectionalLightIntensity * lightIntensity * shadow;

    // Calculate specular
    float NdotH = max(0.0, dot(normal, normalize(lightDir + viewDir)));
    float3 specular = pow(NdotH, SpecularPower) * SpecularIntensity * lightIntensity * DirectionalLightColor;

    float3 finalColor = terrainColor * lighting + specular;

    // Apply atmospheric scattering fog
    float3 atmosphericFog = CalculateAtmosphericFog(input.WorldPosition, viewDir);
    float distanceToCamera = length(input.WorldPosition - CameraPosition);
    float fogAmount = 1.0 - exp(-distanceToCamera * 0.001); // Exponential fog falloff
    fogAmount = saturate(fogAmount);

    // Add atmospheric fog to terrain (additive blending for scattering)
    finalColor += atmosphericFog * fogAmount;

    return float4(finalColor * Brightness, 1.0);
}

technique Unlit
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
