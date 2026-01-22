struct VSInput
{
    float3 pos : POSITION;   // clip-space (-1..1)
    float2 uv  : TEXCOORD0;  // 0..1
};

struct VSOutput
{
    float4 pos : SV_POSITION;
    float2 uv  : TEXCOORD0;
};

VSOutput VSMain(VSInput vin)
{
    VSOutput o;
    o.pos = float4(vin.pos, 1.0f);
    o.uv  = vin.uv;
    return o;
}