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

// Cloud parameters
float3 CameraPosition;
float3 PlanetCenter;
float PlanetRadius = 50.0;
float CloudLayerStart = 52.0; // Start height of cloud layer
float CloudLayerEnd = 56.0;   // End height of cloud layer
float Time = 0.0;

// Cloud appearance
float3 CloudColor = float3(1.0, 1.0, 1.0);
float3 CloudShadowColor = float3(0.3, 0.35, 0.4);
float3 SunDirection = float3(0.0, 0.5, 0.866);
float3 SunColor = float3(1.0, 0.95, 0.9);

// Cloud shape parameters
float CloudCoverage = 0.5;
float CloudDensity = 1.5; // Increased from 0.8 to make clouds denser
float CloudScale = 5.0;
float CloudSpeed = 0.1;
float CloudDetailScale = 20.0;
float CloudDetailStrength = 0.3;

// Ray marching
int MaxSteps = 48; // Increased from 32 to 48 for better quality
float StepSize = 0.5;

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

// 3D Perlin-like noise (simplified for performance)
float Hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float Noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Smooth interpolation

    float n000 = Hash(i + float3(0, 0, 0));
    float n100 = Hash(i + float3(1, 0, 0));
    float n010 = Hash(i + float3(0, 1, 0));
    float n110 = Hash(i + float3(1, 1, 0));
    float n001 = Hash(i + float3(0, 0, 1));
    float n101 = Hash(i + float3(1, 0, 1));
    float n011 = Hash(i + float3(0, 1, 1));
    float n111 = Hash(i + float3(1, 1, 1));

    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);

    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);

    return lerp(nxy0, nxy1, f.z);
}

// Fractal Brownian Motion for cloud shapes - fixed octave count for ps_4_0
float FBM(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    // Unrolled loop for 4 octaves
    value += amplitude * Noise3D(p * frequency);
    frequency *= 2.0;
    amplitude *= 0.5;

    value += amplitude * Noise3D(p * frequency);
    frequency *= 2.0;
    amplitude *= 0.5;

    value += amplitude * Noise3D(p * frequency);
    frequency *= 2.0;
    amplitude *= 0.5;

    value += amplitude * Noise3D(p * frequency);

    return value;
}

// Worley-like noise for cloud detail (simplified) - unrolled for ps_4_0
float WorleyNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);

    float minDist = 1.0;

    // Manually unroll the 3x3x3 loop
    [unroll]
    for (int z = -1; z <= 1; z++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int x = -1; x <= 1; x++)
            {
                float3 neighbor = float3(x, y, z);
                float3 cellPoint = Hash(i + neighbor) * float3(1, 1, 1);
                float3 diff = neighbor + cellPoint - f;
                float dist = length(diff);
                minDist = min(minDist, dist);
            }
        }
    }

    return minDist;
}

// Cloud density function
float CloudDensityFunction(float3 position)
{
    float3 spherePos = position - PlanetCenter;
    float height = length(spherePos);

    // Check if we're in cloud layer
    if (height < CloudLayerStart || height > CloudLayerEnd)
        return 0.0;

    // Normalize height within cloud layer
    float heightFraction = (height - CloudLayerStart) / (CloudLayerEnd - CloudLayerStart);

    // Vertical density gradient (more dense in middle of layer)
    float verticalGradient = 1.0 - abs(heightFraction * 2.0 - 1.0);

    // Convert to spherical coordinates for proper cloud sampling
    float3 normalizedPos = normalize(spherePos);

    // Use spherical coordinates (theta, phi) to avoid stretching at poles
    float theta = atan2(normalizedPos.x, normalizedPos.z); // Longitude
    float phi = asin(normalizedPos.y); // Latitude

    // Convert to 2D tangent space coordinates with proper scaling
    // Scale by radius to maintain proportional cloud sizes
    float2 tangentCoords = float2(theta, phi) * height;

    // Add wind motion in tangent space
    float2 windOffset = float2(Time * CloudSpeed, Time * CloudSpeed * 0.5);
    float2 cloudPos2D = tangentCoords * CloudScale + windOffset;

    // Sample noise in 3D but with proper tangent space mapping
    // This prevents the aurora-like stretching at poles
    float3 cloudPos = float3(cloudPos2D.x, height * CloudScale, cloudPos2D.y);

    // Base cloud shape (FBM for large formations)
    float baseNoise = FBM(cloudPos);

    // Add detail
    float detailNoise = FBM(cloudPos * CloudDetailScale);
    float cloudShape = baseNoise - (1.0 - detailNoise) * CloudDetailStrength;

    // Apply coverage
    cloudShape = saturate((cloudShape - (1.0 - CloudCoverage)) / CloudCoverage);

    // Combine with vertical gradient
    float density = cloudShape * verticalGradient * CloudDensity;

    return density;
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
        return float2(-1.0, -1.0);

    float sqrtDisc = sqrt(discriminant);
    float t1 = (-b - sqrtDisc) / (2.0 * a);
    float t2 = (-b + sqrtDisc) / (2.0 * a);

    return float2(t1, t2);
}

// Lighting calculation for clouds
float3 CalculateCloudLighting(float3 position, float density)
{
    // Sample density towards sun for shadow
    float lightStepSize = 2.0;
    float lightDensity = 0.0;

    // Unroll 6 light samples
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float3 samplePos = position + SunDirection * (float(i) + 0.5) * lightStepSize;
        lightDensity += CloudDensityFunction(samplePos);
    }

    float transmittance = exp(-lightDensity * 0.5);

    // Ambient + direct lighting
    float3 ambient = CloudShadowColor * 0.3;
    float3 direct = SunColor * transmittance;

    return ambient + direct;
}

// Ray marching through cloud layer
float4 RayMarchClouds(float3 rayOrigin, float3 rayDir)
{
    // Find intersection with cloud layer
    float2 cloudStartIntersect = RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, CloudLayerStart);
    float2 cloudEndIntersect = RaySphereIntersection(rayOrigin, rayDir, PlanetCenter, CloudLayerEnd);

    // Determine ray march start and end
    float tStart = max(0.0, cloudStartIntersect.x > 0.0 ? cloudStartIntersect.x : cloudStartIntersect.y);
    float tEnd = cloudEndIntersect.y;

    if (tStart >= tEnd || tEnd < 0.0)
        return float4(0, 0, 0, 0);

    float rayLength = tEnd - tStart;
    float stepSize = rayLength / 48.0; // Updated to use 48 steps

    float3 accumulatedColor = float3(0, 0, 0);
    float accumulatedAlpha = 0.0;

    // Ray march through cloud layer - increased to 48 steps for better quality
    [loop]
    for (int i = 0; i < 48; i++)
    {
        if (accumulatedAlpha > 0.99)
            break;

        float t = tStart + (float(i) + 0.5) * stepSize;
        float3 samplePos = rayOrigin + rayDir * t;

        float density = CloudDensityFunction(samplePos);

        if (density > 0.01)
        {
            float3 lighting = CalculateCloudLighting(samplePos, density);

            // Accumulate color and alpha
            float sampleAlpha = saturate(density * stepSize);
            float3 sampleColor = CloudColor * lighting;

            accumulatedColor += sampleColor * sampleAlpha * (1.0 - accumulatedAlpha);
            accumulatedAlpha += sampleAlpha * (1.0 - accumulatedAlpha);
        }
    }

    return float4(accumulatedColor, accumulatedAlpha);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float3 rayOrigin = CameraPosition;
    float3 rayDir = -input.ViewDirection;

    // Ray march through clouds
    float4 cloudColor = RayMarchClouds(rayOrigin, rayDir);

    if (cloudColor.a < 0.01)
        discard;

    return cloudColor;
}

technique Clouds
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
