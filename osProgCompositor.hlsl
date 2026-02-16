Texture2D baseTex : register(t0);
Texture2D textTex : register(t1);
SamplerState samp : register(s0);

struct VSOut
{
    float4 pos : SV_Position;
    float2 uv  : TEXCOORD0;
};

float4 main(VSOut i) : SV_Target
{
    float4 baseC = baseTex.Sample(samp, i.uv);
    float4 textC = textTex.Sample(samp, i.uv);

    float3 outRgb = textC.rgb + baseC.rgb * (1.0 - textC.a);
    float outA = textC.a + baseC.a * (1.0 - textC.a);

    return float4(outRgb, outA);
}