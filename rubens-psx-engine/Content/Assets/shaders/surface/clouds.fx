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

// 3D Simplex Noise implementation
// Based on Stefan Gustavson's implementation
float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 permute(float4 x) { return mod289(((x * 34.0) + 1.0) * x); }
float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

float SimplexNoise3D(float3 v)
{
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);

    // First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - D.yyy;

    // Permutations
    i = mod289(i);
    float4 p = permute(permute(permute(
        i.z + float4(0.0, i1.z, i2.z, 1.0))
        + i.y + float4(0.0, i1.y, i2.y, 1.0))
        + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    float n_ = 0.142857142857; // 1.0/7.0
    float3 ns = n_ * D.wyz - D.xzx;

    float4 j = p - 49.0 * floor(p * ns.z * ns.z);

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_);

    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, float4(0, 0, 0, 0));

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);

    // Normalize gradients
    float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}

// Keep hash for Worley noise
float Hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Fractal Brownian Motion using Simplex noise for cloud shapes
float FBM(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    // Unrolled loop for 5 octaves using Simplex noise
    value += amplitude * SimplexNoise3D(p * frequency);
    frequency *= 2.07; // Slightly irregular to avoid artifacts
    amplitude *= 0.5;

    value += amplitude * SimplexNoise3D(p * frequency);
    frequency *= 2.03;
    amplitude *= 0.5;

    value += amplitude * SimplexNoise3D(p * frequency);
    frequency *= 2.01;
    amplitude *= 0.5;

    value += amplitude * SimplexNoise3D(p * frequency);
    frequency *= 2.05;
    amplitude *= 0.5;

    value += amplitude * SimplexNoise3D(p * frequency);

    return value * 0.5 + 0.5; // Remap from [-1,1] to [0,1]
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

// Multi-scale fractal cloud density function
float CloudDensityFunction(float3 position)
{
    float3 spherePos = position - PlanetCenter;
    float height = length(spherePos);

    // Check if we're in cloud layer
    if (height < CloudLayerStart || height > CloudLayerEnd)
        return 0.0;

    // Normalize height within cloud layer
    float heightFraction = (height - CloudLayerStart) / (CloudLayerEnd - CloudLayerStart);

    // Vertical density gradient (more dense in middle of layer, softer at top)
    // Use cubic curve for more realistic falloff at cloud tops
    float verticalGradient = heightFraction < 0.5
        ? 1.0 - pow(heightFraction * 2.0, 0.5)  // Bottom: sharp rise
        : pow(1.0 - (heightFraction - 0.5) * 2.0, 1.5); // Top: soft falloff

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

    // MULTI-SCALE FRACTAL APPROACH using Simplex Noise - BIGGER, FLUFFIER clouds
    // Layer 1: Very large-scale cloud formations (bigger clouds)
    float largeScale = FBM(cloudPos * 0.15); // Reduced from 0.3 for bigger shapes

    // Layer 2: Large fluffy structures
    float mediumScale = FBM(cloudPos * 0.6); // Reduced from 1.0 for fluffier look

    // Layer 3: Medium-scale puffs (less aggressive)
    float smallScale = FBM(cloudPos * 1.5); // Reduced from 3.0

    // Layer 4: Subtle wispy edges - now using Simplex noise
    float fineDetail = SimplexNoise3D(cloudPos * 4.0) * 0.5 + 0.5; // Reduced from 8.0

    // Combine scales for big fluffy clouds
    // More weight on large scales, less detail erosion
    float baseShape = largeScale * 0.7 + mediumScale * 0.3;

    // Minimal detail subtraction for fluffier appearance
    float cloudShape = baseShape - (1.0 - smallScale) * 0.1 - (1.0 - fineDetail) * 0.05;

    // Apply coverage threshold to create distinct cloud formations
    cloudShape = saturate((cloudShape - (1.0 - CloudCoverage)) / CloudCoverage);

    // Use Worley noise to erode cloud edges for realistic shapes
    float erosion = WorleyNoise(cloudPos * CloudDetailScale * 0.5);
    cloudShape *= saturate(erosion + 0.3); // Soften erosion to avoid over-eroding

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

// Self-shadowing via ray marching toward sun with powder effect - optimized
float CalculateCloudShadow(float3 position, float density)
{
    // Ray march towards the sun to accumulate shadow density
    float shadowStepSize = 1.8; // Larger steps for performance
    float shadowDensity = 0.0;

    // March only 4 steps toward sun (performance optimized)
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float3 samplePos = position + SunDirection * (float(i) + 0.5) * shadowStepSize;
        float sampleDensity = CloudDensityFunction(samplePos);
        shadowDensity += sampleDensity * shadowStepSize;

        // Early exit if fully shadowed
        if (shadowDensity > 2.0)
            break;
    }

    // Beer's law for light transmission through clouds
    float transmittance = exp(-shadowDensity * 0.7);

    // Powder sugar effect: thin clouds at edges scatter more light
    float powderEffect = 1.0 - exp(-density * 2.0);

    return lerp(transmittance, 1.0, powderEffect * 0.5);
}

// Lighting calculation for clouds with self-shadowing and advanced scattering
float3 CalculateCloudLighting(float3 position, float density)
{
    // Calculate self-shadowing with powder effect
    float shadow = CalculateCloudShadow(position, density);

    // Ambient lighting (sky color) - stronger for thin clouds
    float ambientStrength = lerp(0.3, 0.6, 1.0 - saturate(density));
    float3 ambient = CloudShadowColor * ambientStrength;

    // Direct sun lighting with shadow
    float3 direct = SunColor * shadow * 1.2;

    // HenyeyGreenstein phase function for realistic scattering
    float3 viewDir = normalize(position - CameraPosition);
    float cosAngle = dot(viewDir, SunDirection);

    // Forward scattering (silver lining effect)
    float g = 0.6; // Anisotropy factor
    float g2 = g * g;
    float phase = (1.0 - g2) / pow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
    float3 forwardScatter = SunColor * phase * 0.4;

    // Back scattering (darker edges)
    float backScatter = saturate(-cosAngle) * 0.2;

    return ambient + direct + forwardScatter + backScatter;
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

    float3 accumulatedColor = float3(0, 0, 0);
    float accumulatedAlpha = 0.0;

    // Much lower sample count for better performance
    float distanceToStart = length(rayOrigin + rayDir * tStart - CameraPosition);
    float adaptiveSteps = distanceToStart < 80.0 ? 24.0 : 16.0; // Reduced from 64/32 to 24/16
    float stepSize = rayLength / adaptiveSteps;

    // Ray march through cloud layer with low sample count
    [loop]
    for (int i = 0; i < 24; i++)
    {
        if (i >= adaptiveSteps || accumulatedAlpha > 0.96)
            break;

        float t = tStart + (float(i) + 0.5) * stepSize;
        float3 samplePos = rayOrigin + rayDir * t;

        float density = CloudDensityFunction(samplePos);

        if (density > 0.01) // Higher threshold to skip more samples
        {
            float3 lighting = CalculateCloudLighting(samplePos, density);

            // Improved alpha blending with energy conservation
            float sampleAlpha = 1.0 - exp(-density * stepSize * 2.0); // Stronger alpha for fewer samples
            float3 sampleColor = CloudColor * lighting;

            // Front-to-back compositing
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
