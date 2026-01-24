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
        // base AA fill factor (same as original)
        float filled = ProgBlend_Full(b, u, invSize);

        // Gamma-correct blend: convert to linear, lerp, convert back to sRGB.
        // This produces perceptually better blends for color gradients.
        // Use 2.2 gamma approximation (fast and visually pleasant).
        float3 bg_lin  = pow(pcolorbg.rgb, 2.2f);
        float3 act_lin = pow(pcoloractive.rgb, 2.2f);
        float3 col_lin = lerp(bg_lin, act_lin, filled);
        float3 col_srgb = pow(col_lin, 1.0f / 2.2f);

        // Subtle rim/highlight at the fill edge to give depth.
        // Compute an edge-aware width (same AA-aware width logic).
        float w = max(0.5f * invSize, 0.5f * PixelWidth(u));
        // local distance from the exact edge in units of that width
        float local = (u - b) / w;

        // Gaussian-like narrow highlight centered on the edge, only on the filled side.
        float inside = step(u, b); // 1 when inside filled region (u <= b), else 0
        // sharpness: larger value -> narrower highlight. 64 chosen to keep it tight.
        float rim = exp(-local * local * 64.0f) * 0.12f * inside;
        // reduce rim strength where the fill is very small (avoid overwhelming tiny fills)
        rim *= saturate(filled * 6.0f);

        // apply rim as an additive highlight (clamped)
        float3 final_rgb = saturate(col_srgb + rim);

        float4 col;
        col.rgb = final_rgb;
        col.a = 1.0f;
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
