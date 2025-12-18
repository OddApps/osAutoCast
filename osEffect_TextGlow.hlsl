sampler2D inputSampler : register(s0);

float2 TexelSize      : register(c0);
float  Thickness      : register(c1);
float  Spread         : register(c2);
float  Fade           : register(c3);
float4 GlowColor      : register(c4);
float  StrokeStrength : register(c5);
float  GlowStrength   : register(c6);

float sampleA(float2 uv, float2 off)
{
    return tex2D(inputSampler, clamp(uv + off, 0.0, 1.0)).a;
}

float4 main(float2 uv : TEXCOORD0) : COLOR
{
    float4 src = tex2D(inputSampler, uv);
    float2 step = TexelSize * max(Thickness, 0.001);

    float sum = 0.0;
    float w   = 0.0;

    // 16-direction blur
    float2 dirs[16] =
    {
        float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
        float2(0.7071,0.7071), float2(-0.7071,0.7071),
        float2(0.7071,-0.7071), float2(-0.7071,-0.7071),
        float2(0.9239,0.3827), float2(-0.9239,0.3827),
        float2(0.9239,-0.3827), float2(-0.9239,-0.3827),
        float2(0.3827,0.9239), float2(-0.3827,0.9239),
        float2(0.3827,-0.9239), float2(-0.3827,-0.9239)
    };

    [unroll]
    for (int i = 0; i < 16; i++)
    {
        float2 d = dirs[i];

        float a1 = sampleA(uv, d * step * 1.0);
        float a2 = sampleA(uv, d * step * 2.5);
        float a3 = sampleA(uv, d * step * 5.0);

        sum += a1 * 1.0;
        sum += a2 * 0.6;
        sum += a3 * 0.25;

        w += 1.85;
    }

    float glow = sum / w;
    glow = 1.0 - exp(-glow * GlowStrength * 2.2);

    float intensity = glow * Fade;

    float4 add;
    add.rgb = GlowColor.rgb * GlowColor.a * intensity;
    add.a   = GlowColor.a * intensity;

    float4 result;
    result.rgb = src.rgb + add.rgb * (1.0 - src.a);
    result.a   = saturate(src.a + add.a);

    return result;
}

technique t0
{
    pass P0
    {
        PixelShader = compile ps_3_0 main();
    }
}
