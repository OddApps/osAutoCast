sampler2D inputSampler : register(s0);

float2 TexelSize      : register(c0);
float  Thickness      : register(c1);
float  Spread         : register(c2);
float  Fade           : register(c3);
float4 StrokeColor      : register(c4);
float  StrokeStrength : register(c5);

float sampleA(float2 uv, float2 off)
{
    return tex2D(inputSampler, clamp(uv + off, 0.0, 1.0)).a;
}

float4 main(float2 uv : TEXCOORD0) : COLOR
{
    float4 src = tex2D(inputSampler, uv);
    float  a0  = src.a;

    float2 step = TexelSize * max(Thickness, 0.001);

    float maxA = a0;

    float2 dirs[8] =
    {
        float2(1,0), float2(-1,0),
        float2(0,1), float2(0,-1),
        float2(0.7071,0.7071), float2(-0.7071,0.7071),
        float2(0.7071,-0.7071), float2(-0.7071,-0.7071)
    };

    [unroll]
    for (int i = 0; i < 8; i++)
        maxA = max(maxA, sampleA(uv, dirs[i] * step));

    float edge = maxA - a0;

    float outline =
        smoothstep(0.0, 1.0 / max(Spread, 0.001), edge);

    outline = pow(outline, 0.65); 

    float intensity = outline * StrokeStrength * Fade;

    float4 add;
    add.rgb = StrokeColor.rgb * StrokeColor.a * intensity;
    add.a   = StrokeColor.a * intensity;

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
