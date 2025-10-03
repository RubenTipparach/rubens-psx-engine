#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

// Matrices
float4x4 World;
float4x4 View;
float4x4 Projection;

// Atmosphere parameters
float3 CameraPosition;
float3 PlanetCenter;
float PlanetRadius = 50.0;
float AtmosphereRadius = 60.0;
float3 SunDirection = float3(0.0, 0.5, 0.866); // Normalized direction to sun

// Atmospheric scattering colors
float3 RayleighColor = float3(0.26, 0.41, 0.58); // Blue sky
float3 MieColor = float3(1.0, 0.9, 0.8); // Sunset/sunrise warm colors
float3 SunColor = float3(1.0, 0.9, 0.7);

// Scattering coefficients
float RayleighStrength = 2.0;
float MieStrength = 0.8;
float SunIntensity = 20.0;
float AtmosphereThickness = 1.0;
float DensityFalloff = 4.0;

// Fog parameters
float FogDensity = 0.5;
float FogHeight = 1.0;

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
    float3 ViewDirection : TEXCOORD2;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    output.WorldPosition = worldPosition.xyz;
    output.ViewDirection = normalize(CameraPosition - worldPosition.xyz);
    output.Normal = normalize(mul(input.Normal, (float3x3)World));

    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    return output;
}

// Ray-sphere intersection
float2 RaySphereIntersection(float3 rayOrigin, float3 rayDir, float3 sphereCenter, float sphereRadius)
{
    float3 offset = rayOrigin - sphereCenter;
    float a = dot(rayDir, rayDir);
    float b = 2.0 * dot(offset, rayDir);
    float c = dot(offset, offset) - sphereRadius * sphereRadius;
    float discriminant = b * b - 4.0 * a * c;

    if (discriminant < 0.0)
        return float2(-1.0, -1.0); // No intersection

    float sqrtDisc = sqrt(discriminant);
    float t1 = (-b - sqrtDisc) / (2.0 * a);
    float t2 = (-b + sqrtDisc) / (2.0 * a);

    return float2(t1, t2);
}

// Atmospheric density at a given height
float AtmosphereDensity(float3 position)
{
    float height = length(position - PlanetCenter) - PlanetRadius;
    float normalizedHeight = saturate(height / (AtmosphereRadius - PlanetRadius));
    return exp(-normalizedHeight * DensityFalloff) * AtmosphereThickness;
}

// Rayleigh phase function (wavelength-dependent scattering)
float RayleighPhase(float cosTheta)
{
    return (3.0 / (16.0 * 3.14159)) * (1.0 + cosTheta * cosTheta);
}

// Mie phase function (forward scattering for particles)
float MiePhase(float cosTheta, float g)
{
    float g2 = g * g;
    float num = (1.0 - g2);
    float denom = 4.0 * 3.14159 * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5);
    return num / denom;
}

// Simple atmospheric scattering calculation
float3 CalculateAtmosphericScattering(float3 rayOrigin, float3 rayDir, float rayLength)
{
    const int numSamples = 16;
    float stepSize = rayLength / float(numSamples);

    float3 rayleighAccum = float3(0.0, 0.0, 0.0);
    float3 mieAccum = float3(0.0, 0.0, 0.0);

    float cosTheta = dot(rayDir, SunDirection);
    float rayleighPhase = RayleighPhase(cosTheta);
    float miePhase = MiePhase(cosTheta, 0.76); // g = 0.76 for Earth-like atmosphere

    for (int i = 0; i < numSamples; i++)
    {
        float t = (float(i) + 0.5) * stepSize;
        float3 samplePos = rayOrigin + rayDir * t;

        float density = AtmosphereDensity(samplePos);

        // Calculate light transmittance from sun to sample point
        float3 sunDir = SunDirection;
        float2 sunIntersect = RaySphereIntersection(samplePos, sunDir, PlanetCenter, AtmosphereRadius);

        float sunRayLength = sunIntersect.y;
        float opticalDepth = density * stepSize;

        // Accumulate scattering
        rayleighAccum += density * rayleighPhase * exp(-opticalDepth * 0.1);
        mieAccum += density * miePhase * exp(-opticalDepth * 0.05);
    }

    rayleighAccum *= stepSize * RayleighStrength;
    mieAccum *= stepSize * MieStrength;

    // Combine Rayleigh (blue sky) and Mie (sunset/sunrise) scattering
    float3 scattering = rayleighAccum * RayleighColor + mieAccum * MieColor;

    // Add sun disk
    float sunDisk = pow(saturate(cosTheta), 512.0) * SunIntensity;
    scattering += SunColor * sunDisk;

    // Dusk/dawn transition (more warm colors near horizon)
    float horizonFactor = 1.0 - abs(rayDir.y);
    scattering += MieColor * horizonFactor * horizonFactor * 0.5;

    return scattering;
}

// Volumetric fog based on height
float CalculateFog(float3 worldPos)
{
    float height = length(worldPos - PlanetCenter) - PlanetRadius;
    float heightFog = exp(-height / FogHeight) * FogDensity;

    float distanceFromCamera = length(worldPos - CameraPosition);
    float distanceFog = 1.0 - exp(-distanceFromCamera * 0.001);

    return saturate(heightFog * distanceFog);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float3 rayOrigin = CameraPosition;
    float3 rayDir = -input.ViewDirection;

    // Check if ray intersects atmosphere
    float2 atmosphereIntersect = RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, AtmosphereRadius);
    float2 planetIntersect = RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, PlanetRadius);

    // If we're looking at the planet, don't render atmosphere there
    float rayLength = 0.0;
    if (atmosphereIntersect.x > 0.0)
    {
        rayLength = atmosphereIntersect.y - max(0.0, atmosphereIntersect.x);

        // If ray hits planet, limit atmosphere ray to before planet surface
        if (planetIntersect.x > 0.0)
        {
            rayLength = min(rayLength, planetIntersect.x - max(0.0, atmosphereIntersect.x));
        }
    }

    if (rayLength <= 0.0)
        discard;

    // Calculate atmospheric scattering
    float3 scattering = CalculateAtmosphericScattering(rayOrigin, rayDir, rayLength);

    // Calculate fog
    float fog = CalculateFog(input.WorldPosition);

    // Fade based on view angle (more transparent when looking straight at it)
    float viewDot = abs(dot(input.Normal, input.ViewDirection));
    float fresnel = pow(1.0 - viewDot, 3.0);

    float alpha = saturate(fresnel * 0.8 + fog * 0.3);

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
