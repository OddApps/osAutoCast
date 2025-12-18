sampler2D inputSampler : register(s0);

float2 TexelSize      : register(c0); // (1/width, 1/height)
float  Thickness      : register(c1); // stroke radius in pixels
float  Spread         : register(c2); // edge hardness
float  Fade           : register(c3); // 0..1 fade
float4 GlowColor      : register(c4); // RGBA
float  StrokeStrength : register(c5);
float  GlowStrength   : register(c6);

// ------------------------------------------------------------
// Safe alpha sampling with UV clamp
// ------------------------------------------------------------
float sampleA(float2 uv, float2 offset)
{
    float2 sUV = clamp(uv + offset, 0.0, 1.0);
    return tex2D(inputSampler, sUV).a;
}

// ------------------------------------------------------------
// Pixel shader
// ------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : COLOR
{
    float4 src = tex2D(inputSampler, uv);
    float  a0  = src.a;

    float safeThickness = max(Thickness, 0.0001);
    float2 step = TexelSize * safeThickness;

    // Base directions
    float2 d1 = float2( step.x,  0.0);
    float2 d2 = float2( 0.0,  step.y);
    float2 d3 = -d1;
    float2 d4 = -d2;

    float2 d5 = step * 0.70710678;
    float2 d6 = float2(-d5.x,  d5.y);
    float2 d7 = float2( d5.x, -d5.y);
    float2 d8 = -d5;

    // Extra angles to kill octagon artifacts
    float2 d9  = step * float2( 0.9239,  0.3827);
    float2 d10 = step * float2(-0.9239,  0.3827);
    float2 d11 = -d9;
    float2 d12 = -d10;

    // --------------------------------------------------------
    // OUTLINE (smooth morphological edge)
    // --------------------------------------------------------
    float maxA = a0;
    maxA = max(maxA, sampleA(uv, d1));
    maxA = max(maxA, sampleA(uv, d2));
    maxA = max(maxA, sampleA(uv, d3));
    maxA = max(maxA, sampleA(uv, d4));
    maxA = max(maxA, sampleA(uv, d5));
    maxA = max(maxA, sampleA(uv, d6));
    maxA = max(maxA, sampleA(uv, d7));
    maxA = max(maxA, sampleA(uv, d8));

    float edge = maxA - a0;
    float outline = smoothstep(0.0, 1.0 / max(Spread, 0.0001), edge);

    // --------------------------------------------------------
    // SOFT GLOW (radial, distance weighted)
    // --------------------------------------------------------
    float glowAccum = 0.0;
    float weightSum = 0.0;

    #define ADD_GLOW(off, w) \
    { \
        float a = sampleA(uv, off); \
        glowAccum += a * w; \
        weightSum += w; \
    }

    // Ring 1
    ADD_GLOW(d1  * 1.0, 1.0);
    ADD_GLOW(d2  * 1.0, 1.0);
    ADD_GLOW(d3  * 1.0, 1.0);
    ADD_GLOW(d4  * 1.0, 1.0);
    ADD_GLOW(d5  * 1.0, 0.9);
    ADD_GLOW(d6  * 1.0, 0.9);
    ADD_GLOW(d7  * 1.0, 0.9);
    ADD_GLOW(d8  * 1.0, 0.9);

    // Ring 2
    ADD_GLOW(d1  * 2.0, 0.45);
    ADD_GLOW(d2  * 2.0, 0.45);
    ADD_GLOW(d3  * 2.0, 0.45);
    ADD_GLOW(d4  * 2.0, 0.45);
    ADD_GLOW(d5  * 2.0, 0.4);
    ADD_GLOW(d6  * 2.0, 0.4);
    ADD_GLOW(d7  * 2.0, 0.4);
    ADD_GLOW(d8  * 2.0, 0.4);

    // Ring 3 (angle smoothing)
    ADD_GLOW(d9  * 1.5, 0.6);
    ADD_GLOW(d10 * 1.5, 0.6);
    ADD_GLOW(d11 * 1.5, 0.6);
    ADD_GLOW(d12 * 1.5, 0.6);

    float glow = glowAccum / max(weightSum, 0.0001);

    // Gaussian-like falloff
    glow = 1.0 - exp(-glow * 2.2);

    // --------------------------------------------------------
    // FINAL COMBINE (WPF-safe premultiplied)
    // --------------------------------------------------------
    float intensity = (outline * StrokeStrength + glow * GlowStrength) * Fade;

    float4 add = float4(GlowColor.rgb * GlowColor.a, GlowColor.a) * intensity;

    float4 result;
    result.rgb = src.rgb + add.rgb * (1.0 - src.a);
    result.a   = saturate(src.a + add.a);

    result.rgb = saturate(result.rgb);
    return result;
}

// ------------------------------------------------------------
// Technique
// ------------------------------------------------------------
technique t0
{
    pass P0
    {
        PixelShader = compile ps_3_0 main();
    }
}
