#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif

// Matrices
float4x4 World;
float4x4 View;
float4x4 Projection;

// Atmosphere parameters
float3 CameraPosition;
float3 PlanetCenter = float3(0, 0, 0);
float PlanetRadius = 50.0;
float AtmosphereRadius = 60.0;
float3 SunDirection = float3(0.0, 0.5, 0.866);

// Scattering coefficients (physically based for Earth-like atmosphere)
static const float3 BetaRayleigh = float3(5.8e-3, 13.5e-3, 33.1e-3); // Rayleigh scattering coefficients
static const float BetaMie = 21e-3;                                    // Mie scattering coefficient

static const float HRayleigh = 8000.0;  // Rayleigh scale height (in meters, will be normalized)
static const float HMie = 1200.0;       // Mie scale height

static const float g = 0.76;            // Mie scattering direction (anisotropy)

// Intensity multipliers
float SunIntensity = 20.0;
float AtmosphereIntensity = 1.0;

// Sampling quality
static const int NumSamples = 16;
static const int NumSamplesLight = 8;

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
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    output.WorldPosition = worldPosition.xyz;
    output.Normal = normalize(mul(input.Normal, (float3x3)World));

    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // No depth modification - let atmosphere render at its natural position
    // The double-sided sphere will render correctly with alpha blending

    return output;
}

// Ray-sphere intersection
bool RaySphereIntersection(float3 rayOrigin, float3 rayDir, float3 sphereCenter, float sphereRadius, out float t0, out float t1)
{
    float3 L = sphereCenter - rayOrigin;
    float tca = dot(L, rayDir);
    float d2 = dot(L, L) - tca * tca;
    float radius2 = sphereRadius * sphereRadius;

    if (d2 > radius2)
        return false;

    float thc = sqrt(radius2 - d2);
    t0 = tca - thc;
    t1 = tca + thc;

    return true;
}

// Atmospheric density at height (exponential falloff)
float DensityRayleigh(float height)
{
    // Normalize scale height to our planet scale
    float scaleHeight = (AtmosphereRadius - PlanetRadius) * (HRayleigh / 8000.0);
    return exp(-height / scaleHeight);
}

float DensityMie(float height)
{
    float scaleHeight = (AtmosphereRadius - PlanetRadius) * (HMie / 8000.0);
    return exp(-height / scaleHeight);
}

// Rayleigh phase function
float PhaseRayleigh(float cosTheta)
{
    return (3.0 / (16.0 * 3.14159)) * (1.0 + cosTheta * cosTheta);
}

// Mie phase function (Cornette-Shanks)
float PhaseMie(float cosTheta)
{
    float g2 = g * g;
    float numerator = (1.0 - g2) * (1.0 + cosTheta * cosTheta);
    float denominator = (2.0 + g2) * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5);
    return (3.0 / (8.0 * 3.14159)) * numerator / denominator;
}

// Calculate optical depth (integral of density along path)
void CalculateOpticalDepth(float3 rayStart, float3 rayEnd, out float depthRayleigh, out float depthMie)
{
    float3 step = (rayEnd - rayStart) / float(NumSamplesLight);
    float stepLength = length(step);

    depthRayleigh = 0.0;
    depthMie = 0.0;

    float3 pos = rayStart + step * 0.5;

    [unroll]
    for (int i = 0; i < NumSamplesLight; i++)
    {
        float height = length(pos - PlanetCenter) - PlanetRadius;

        if (height < 0.0)
            return; // Below surface, in shadow

        depthRayleigh += DensityRayleigh(height) * stepLength;
        depthMie += DensityMie(height) * stepLength;

        pos += step;
    }
}

// Main atmospheric scattering calculation
float3 CalculateScattering(float3 rayOrigin, float3 rayDir, float rayLength, float3 sunDir)
{
    float3 step = rayDir * (rayLength / float(NumSamples));
    float stepLength = length(step);

    float3 pos = rayOrigin + step * 0.5;

    // Accumulated scattering
    float3 sumRayleigh = float3(0, 0, 0);
    float3 sumMie = float3(0, 0, 0);

    // Optical depth along view ray
    float opticalDepthRayleigh = 0.0;
    float opticalDepthMie = 0.0;

    float cosTheta = dot(rayDir, sunDir);
    float phaseRayleigh = PhaseRayleigh(cosTheta);
    float phaseMie = PhaseMie(cosTheta);

    [loop]
    for (int i = 0; i < NumSamples; i++)
    {
        float height = length(pos - PlanetCenter) - PlanetRadius;

        // Density at current sample point
        float densityRayleigh = DensityRayleigh(height);
        float densityMie = DensityMie(height);

        // Accumulate optical depth along view ray
        opticalDepthRayleigh += densityRayleigh * stepLength;
        opticalDepthMie += densityMie * stepLength;

        // Calculate optical depth to sun from this point
        float t0Sun, t1Sun;
        RaySphereIntersection(pos, sunDir, PlanetCenter, AtmosphereRadius, t0Sun, t1Sun);

        float sunRayLength = t1Sun;
        float lightDepthRayleigh, lightDepthMie;
        CalculateOpticalDepth(pos, pos + sunDir * sunRayLength, lightDepthRayleigh, lightDepthMie);

        // Calculate transmittance (Beer's law)
        float3 tau = BetaRayleigh * (opticalDepthRayleigh + lightDepthRayleigh) +
                     BetaMie * (opticalDepthMie + lightDepthMie);
        float3 attenuation = exp(-tau);

        // Accumulate scattered light
        sumRayleigh += attenuation * densityRayleigh * stepLength;
        sumMie += attenuation * densityMie * stepLength;

        pos += step;
    }

    // Apply scattering coefficients and phase functions
    float3 scatteredLight = sumRayleigh * BetaRayleigh * phaseRayleigh +
                           sumMie * BetaMie * phaseMie;

    return scatteredLight * SunIntensity * AtmosphereIntensity;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float3 rayOrigin = CameraPosition;
    float3 rayDir = normalize(input.WorldPosition - CameraPosition);

    // Check if camera is inside or outside atmosphere
    float cameraHeight = length(rayOrigin - PlanetCenter);
    bool insideAtmosphere = cameraHeight < AtmosphereRadius;

    // Find intersection with atmosphere
    float t0Atmos, t1Atmos;
    if (!RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, AtmosphereRadius, t0Atmos, t1Atmos))
    {
        discard; // No intersection with atmosphere
        return float4(0, 0, 0, 0);
    }

    // Find intersection with planet surface
    float t0Planet, t1Planet;
    bool hitsPlanet = RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, PlanetRadius, t0Planet, t1Planet);

    // Determine ray marching start and end points
    float tStart, tEnd;

    if (insideAtmosphere)
    {
        // Camera inside atmosphere - start from camera
        tStart = 0.0;

        if (hitsPlanet && t0Planet > 0.0)
        {
            // Ray hits planet - end at planet surface
            tEnd = t0Planet;
        }
        else
        {
            // Ray exits atmosphere - end at outer boundary
            tEnd = t1Atmos;
        }
    }
    else
    {
        // Camera outside atmosphere - start from atmosphere entry
        tStart = max(0.0, t0Atmos);

        if (hitsPlanet && t0Planet > tStart)
        {
            // Ray hits planet - end at planet surface
            tEnd = t0Planet;
        }
        else
        {
            // Ray exits atmosphere - end at atmosphere exit
            tEnd = t1Atmos;
        }
    }

    if (tStart >= tEnd)
    {
        discard;
        return float4(0, 0, 0, 0);
    }

    // Calculate scattering along the ray
    float rayLength = tEnd - tStart;
    float3 scatteringStart = rayOrigin + rayDir * tStart;

    float3 scattering = CalculateScattering(scatteringStart, rayDir, rayLength, SunDirection);

    // Calculate alpha based on optical depth
    float opticalDepth = rayLength / (AtmosphereRadius - PlanetRadius);
    float alpha = saturate(opticalDepth * 0.8);

    // Increase alpha when viewing from grazing angles
    float3 viewDir = normalize(rayOrigin - input.WorldPosition);
    float NdotV = abs(dot(input.Normal, viewDir));
    float fresnel = pow(1.0 - NdotV, 3.0);
    alpha = saturate(alpha + fresnel * 0.3);

    return float4(scattering, alpha);
}

technique Atmosphere
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
