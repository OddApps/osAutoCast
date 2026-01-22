// ProgressBarPS.hlsl
// Compile target: ps_5_0

cbuffer ProgressCB : register(b0)
{
    float startTime;      // seconds
    float currentTime;    // seconds
    float duration;       // seconds
    int   easingType;     // 0=linear,1=inQuad,2=outQuad,3=inOutQuad,4=inSine
    float4 fillColor;
    float4 bgColor;
    float2 resolution;
    float2 padding;      // 16-byte alignment
};

struct PSInput
{
    float4 pos : SV_POSITION;
    float2 uv  : TEXCOORD0;
};

float Ease(float t, int type)
{
    t = saturate(t);

    if (type == 0) return t;                    // Linear
    if (type == 1) return t * t;                // EaseInQuad
    if (type == 2) return 1.0 - (1.0 - t) * (1.0 - t); // EaseOutQuad
    if (type == 3)
    {
        if (t < 0.5) return 2.0 * t * t;
        return -1.0 + (4.0 - 2.0 * t) * t;
    }
    if (type == 4) return 1.0 - cos(t * 1.5707963); // EaseInSine

    return t;
}

float4 osProgShader_Main(PSInput IN) : SV_TARGET
{
    float rawT = (currentTime - startTime) / max(duration, 0.0001);
    float progress = Ease(rawT, easingType);

    float edgeAA = 1.0 / max(1.0, resolution.x);
    float fillMask = smoothstep(progress - edgeAA, progress + edgeAA, IN.uv.x);

    float4 color = lerp(bgColor, fillColor, fillMask);

    // Moving sheen inside filled region
    if (IN.uv.x <= progress)
    {
        float wave =
            sin((IN.uv.x * 12.0 - currentTime * 6.0) * 6.2831853) * 0.5 + 0.5;
        color.rgb += wave * 0.10 * (1.0 - IN.uv.x / max(progress, 0.001));
    }

    color.a = lerp(bgColor.a, fillColor.a, fillMask);
    return color;
}
