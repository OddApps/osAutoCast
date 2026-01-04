#pragma pack_matrix(row_major)

struct osProgVertex_Output
{
    float4 pos : SV_Position;
};

osProgVertex_Output osProgVertex(uint vid : SV_VertexID)
{
    // Fullscreen triangle in NDC
    // Table lookup avoids FXC flow-control heuristics
    static const float2 kVerts[3] =
    {
        float2(-1.0, -1.0),
        float2(-1.0,  3.0),
        float2( 3.0, -1.0)
    };

    osProgVertex_Output o;
    o.pos = float4(kVerts[vid], 0.0, 1.0);
    return o;
}