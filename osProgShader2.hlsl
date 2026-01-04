#pragma pack_matrix(row_major)

cbuffer ProgBarCB : register(b0) {
    float prevValue;
    float currValue;
    float invSize;
    float flags;
    float4 pcoloractive;
    float4 pcolorbg;
    float barOffset;
    float pad0;
    float pad1;
    float pad2;
};

struct osProgShader_Input {
    float4 pos : SV_Position;
};

// --- SM6-safe derivative width ---
float PixelWidth(float x)
{
    // Explicit derivative magnitude (preferred in SM6)
    float dx = abs(ddx(x));
    float dy = abs(ddy(x));
    return max(dx + dy, 1e-6);
}

// --- Pixel snapping ---
float SnapToPx(float t, float invSize)
{
    float px = rcp(invSize);
    return round(t * px) * invSize;
}

// --- Full bar AA blend ---
float ProgBlend_Full(float edge, float x, float invSize)
{
    float d = x - edge;

    float w = max(
        0.5 * invSize,
        0.5 * PixelWidth(x)
    );

    return saturate(0.5 - d * rcp(2.0 * w));
}

// --- Step transition blend ---
float ProgBlend_Step(float a, float b, float x, float invSize, float dv)
{
    float lo = min(a, b);
    float hi = max(a, b);

    hi = max(hi, lo + invSize);

    const float FEATHER_PER_UNIT_DV_PX = 0.75;
    const float FEATHER_EXTRA_MAX_PX   = 0.5;

    float addHalfPx =
        min(dv * FEATHER_PER_UNIT_DV_PX, FEATHER_EXTRA_MAX_PX);

    float halfAA =
        max(0.5 * invSize, 0.5 * PixelWidth(x))
        + addHalfPx * invSize;

    float left  = smoothstep(lo - halfAA, lo + halfAA, x);
    float right = 1.0 - smoothstep(hi - halfAA, hi + halfAA, x);

    return saturate(left * right);
}

// --- Main ---
float4 osProgShader_Main(osProgShader_Input pin) : SV_Target
{
    float u = (pin.pos.x - barOffset) * invSize;

    if (u < 0.0 || u > 1.0)
        discard;

    float a = SnapToPx(prevValue, invSize);
    float b = SnapToPx(currValue, invSize);

    bool fullCompose = (((uint)flags & 4u) != 0u);

    if (fullCompose)
    {
        float filled = ProgBlend_Full(b, u, invSize);
        float4 col   = lerp(pcolorbg, pcoloractive, filled);
        col.a = 1.0;
        return col;
    }
    else
    {
        if (a == b)
            discard;

        float dv = abs(b - a);
        float m  = ProgBlend_Step(a, b, u, invSize, dv);

        if (m <= 0.0)
            discard;

        float4 baseCol = (b >= a) ? pcoloractive : pcolorbg;
        return baseCol;
    }
}
