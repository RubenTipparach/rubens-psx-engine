matrix World;
matrix View;
matrix Projection;

// Star field parameters
float StarDensity = 8.0;          // Higher = more stars
float StarBrightness = 2.0;       // Star brightness multiplier
float StarSize = 0.02;            // Star size threshold (smaller = smaller stars)
float StarTwinkle = 0.3;          // Star twinkling amount
float Time = 0.0;                 // Time for animation
float3 StarColor = float3(1.0, 0.95, 0.9); // Slightly warm white

// Nebula/background parameters
float NebulaBrightness = 0.1;     // Background nebula glow
float3 NebulaColor1 = float3(0.1, 0.05, 0.2); // Deep purple
float3 NebulaColor2 = float3(0.05, 0.1, 0.15); // Deep blue

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : TEXCOORD0;
};

// Hash function for random values
float hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// 3D Voronoi noise - returns distance to nearest cell point
float voronoi(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);

    float minDist = 1.0;

    // Check neighboring cells
    for (int k = -1; k <= 1; k++)
    {
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                float3 b = float3(i, j, k);
                float3 r = b - f + hash(p + b);
                float d = dot(r, r);
                minDist = min(minDist, d);
            }
        }
    }

    return sqrt(minDist);
}

// Multi-octave Voronoi for more complex patterns
float voronoiFbm(float3 p, int octaves)
{
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++)
    {
        value += voronoi(p * frequency) * amplitude;
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Simple noise for nebula
float noise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(
            lerp(hash(i + float3(0, 0, 0)), hash(i + float3(1, 0, 0)), f.x),
            lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), f.x),
            f.y
        ),
        lerp(
            lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), f.x),
            lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), f.x),
            f.y
        ),
        f.z
    );
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Use world position for star field sampling
    output.WorldPos = worldPosition.xyz;

    return output;
}

float4 PS(VertexShaderOutput input) : SV_Target0
{
    // Normalize world position to get direction vector
    float3 dir = normalize(input.WorldPos);

    // Sample Voronoi noise at multiple scales for stars
    float3 starSample = dir * StarDensity;

    // Add subtle rotation/animation
    float timeOffset = Time * 0.1;
    starSample += float3(sin(timeOffset) * 0.1, cos(timeOffset) * 0.1, 0.0);

    // Get Voronoi cell distance
    float voronoiDist = voronoi(starSample);

    // Create stars from Voronoi cells
    // Stars appear at cell centers (where distance is smallest)
    float starMask = 1.0 - smoothstep(0.0, StarSize, voronoiDist);

    // Add star brightness variation based on cell hash
    float starVariation = hash(floor(starSample));
    starMask *= starVariation * starVariation; // Square for more contrast

    // Add subtle twinkling
    float twinkle = sin(Time * 2.0 + starVariation * 100.0) * 0.5 + 0.5;
    starMask *= lerp(1.0, twinkle, StarTwinkle);

    // Generate different sized stars
    float largeStar = 1.0 - smoothstep(0.0, StarSize * 2.0, voronoi(starSample * 0.5));
    largeStar *= hash(floor(starSample * 0.5));
    largeStar *= 0.5; // Make large stars dimmer

    // Combine star layers
    float finalStarMask = saturate(starMask + largeStar);

    // Create nebula background using multi-octave noise
    float nebula1 = noise(dir * 2.0 + Time * 0.01);
    float nebula2 = noise(dir * 3.0 - Time * 0.015);
    float nebula3 = noise(dir * 5.0 + Time * 0.02);

    // Mix nebula colors
    float3 nebulaColor = lerp(NebulaColor1, NebulaColor2, nebula1);
    nebulaColor += NebulaColor2 * nebula2 * 0.3;
    nebulaColor *= (nebula3 * 0.5 + 0.5) * NebulaBrightness;

    // Combine stars and nebula
    float3 finalColor = nebulaColor;
    finalColor += StarColor * finalStarMask * StarBrightness;

    return float4(finalColor, 1.0);
}

technique StarField
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}
