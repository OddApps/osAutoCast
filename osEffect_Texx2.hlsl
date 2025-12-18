sampler2D inputSampler : register(s0);

float2 TexelSize      : register(c0); // 1/width, 1/height
float  Thickness      : register(c1); // radius in pixels
float  Spread         : register(c2); // stroke softness
float  Fade           : register(c3); // 0..1
float4 GlowColor      : register(c4); // RGBA
float  StrokeStrength : register(c5);
float  GlowStrength   : register(c6);

// ------------------------------------------------------------
float sampleA(float2 uv, float2 off)
{
    return tex2D(inputSampler, clamp(uv + off, 0.0, 1.0)).a;
}

// ------------------------------------------------------------
float4 main(float2 uv : TEXCOORD0) : SV_Target
{
    float4 src = tex2D(inputSampler, uv);
    float  a0  = src.a;

    float2 step = TexelSize * max(Thickness, 0.001);

    // --------------------------------------------------------
    // RADIAL SAMPLING (32 directions, 3 rings)
    // --------------------------------------------------------
    float glowSum = 0.0;
    float weight  = 0.0;
    float maxA    = a0;

    [unroll]
    for (int i = 0; i < 32; i++)
    {
        float ang = (6.28318530718 / 32.0) * i;
        float2 dir = float2(cos(ang), sin(ang));

        float a1 = sampleA(uv, dir * step * 1.0);
        float a2 = sampleA(uv, dir * step * 2.2);
        float a3 = sampleA(uv, dir * step * 4.0);

        maxA = max(maxA, a1);

        glowSum += a1 * 1.0;
        glowSum += a2 * 0.55;
        glowSum += a3 * 0.22;

        weight  += 1.77;
    }

    float glow = glowSum / weight;

    // --------------------------------------------------------
    // PHOTOSHOP-LIKE CURVE (Gaussian-ish)
    // --------------------------------------------------------
    glow = 1.0 - exp(-glow * GlowStrength * 2.6);

    // --------------------------------------------------------
    // CRISP ANTI-ALIASED STROKE
    // --------------------------------------------------------
    float edge = maxA - a0;

    float outline =
        smoothstep(0.0, 1.0 / max(Spread, 0.001), edge);

    // Sub-pixel AA boost
    outline = pow(outline, 0.65);

    // Prevent glow from washing out stroke
    glow *= (1.0 - outline);

    float intensity =
        (outline * StrokeStrength + glow) * Fade;

    // --------------------------------------------------------
    // WPF PREMULTIPLIED COMPOSITE
    // --------------------------------------------------------
    float4 add;
    add.rgb = GlowColor.rgb * GlowColor.a * intensity;
    add.a   = GlowColor.a * intensity;

    float4 result;
    result.rgb = src.rgb + add.rgb * (1.0 - src.a);
    result.a   = saturate(src.a + add.a);

    return saturate(result);
}

// ------------------------------------------------------------
technique t0
{
    pass P0
    {
        PixelShader = compile ps_4_0 main();
    }
}
