sampler2D inputSampler : register(s0);

float2 TexelSize     : register(c0); // (1/width, 1/height) of the rendered input
float  Thickness     : register(c1); // stroke radius in *pixels*
float  Spread        : register(c2); // edge hardness (2..8 typical). Higher => tighter edge
float  Fade          : register(c3); // animate 0..1 for fade in/out
float4 GlowColor     : register(c4); // RGB + A for the glow color
float  StrokeStrength: register(c5); // multiply outline intensity
float  GlowStrength  : register(c6); // multiply soft glow intensity

// Helper: safe clamp of UVs and return sampled alpha
float sampleA(float2 uv, float2 offset)
{
    float2 sUV = uv + offset;
    sUV = clamp(sUV, 0.0, 1.0);       // clamp to edge to avoid border zeros/cutoff
    return tex2D(inputSampler, sUV).a;
}

float4 main(float2 uv : TEXCOORD0) : COLOR
{
    float4 src = tex2D(inputSampler, uv);
    float a0   = src.a;

    // Prevent zero step when Thickness==0
    float safeThickness = max(Thickness, 0.0001);
    float2 step = TexelSize * safeThickness;

    // 8-direction taps
    float2 d1 = float2(step.x, 0.0);
    float2 d2 = float2(0.0, step.y);
    float2 d3 = -d1;
    float2 d4 = -d2;
    float2 d5 = step * 0.70710678;        // diagonal (~1/sqrt(2))
    float2 d6 = float2(-d5.x,  d5.y);
    float2 d7 = float2( d5.x, -d5.y);
    float2 d8 = -d5;

    // Outline (max-morphology) to find how close we are to the opaque glyphs
    float maxA = a0;
    float a;
    a = sampleA(uv, d1); maxA = max(maxA, a);
    a = sampleA(uv, d2); maxA = max(maxA, a);
    a = sampleA(uv, d3); maxA = max(maxA, a);
    a = sampleA(uv, d4); maxA = max(maxA, a);
    a = sampleA(uv, d5); maxA = max(maxA, a);
    a = sampleA(uv, d6); maxA = max(maxA, a);
    a = sampleA(uv, d7); maxA = max(maxA, a);
    a = sampleA(uv, d8); maxA = max(maxA, a);

    // Outline alpha: higher when center is transparent but neighbours are opaque
    float outline = saturate((maxA - a0) * Spread);

    // Soft glow: 16 taps at 0.5R and 1.5R along the same directions
    float glowAccum = 0.0;
    float count     = 0.0;

    glowAccum += sampleA(uv, d1*0.5); count += 1.0;
    glowAccum += sampleA(uv, d1*1.5); count += 1.0;
    glowAccum += sampleA(uv, d2*0.5); count += 1.0;
    glowAccum += sampleA(uv, d2*1.5); count += 1.0;
    glowAccum += sampleA(uv, d3*0.5); count += 1.0;
    glowAccum += sampleA(uv, d3*1.5); count += 1.0;
    glowAccum += sampleA(uv, d4*0.5); count += 1.0;
    glowAccum += sampleA(uv, d4*1.5); count += 1.0;

    glowAccum += sampleA(uv, d5*0.5); count += 1.0;
    glowAccum += sampleA(uv, d5*1.5); count += 1.0;
    glowAccum += sampleA(uv, d6*0.5); count += 1.0;
    glowAccum += sampleA(uv, d6*1.5); count += 1.0;
    glowAccum += sampleA(uv, d7*0.5); count += 1.0;
    glowAccum += sampleA(uv, d7*1.5); count += 1.0;
    glowAccum += sampleA(uv, d8*0.5); count += 1.0;
    glowAccum += sampleA(uv, d8*1.5); count += 1.0;

    float glow = glowAccum / count;

    // Combine outline + glow, then scale by strengths and fade
    float intensity = saturate(outline * StrokeStrength + glow * GlowStrength);
    float factor    = intensity * Fade;

    // Premultiply glow color to blend correctly in WPF
    float4 add = float4(GlowColor.rgb * GlowColor.a, GlowColor.a) * factor;

    float4 result = src + add;
    result.rgb = saturate(result.rgb);
    result.a   = saturate(result.a);
    return result;
}

technique t0
{
    pass P0
    {
        PixelShader = compile ps_3_0 main();
    }
}
