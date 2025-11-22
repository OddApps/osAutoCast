struct osProgVertex_Output {
    float4 pos : SV_Position;
};

osProgVertex_Output osProgVertex(uint vid : SV_VertexID) {
	float2 p = (vid == 0) ? float2(-1,-1)
               : (vid == 1) ? float2(-1, 3)
                             : float2( 3,-1);

    osProgVertex_Output o;
	
    o.pos = float4(p, 0.0, 1.0);
    return o;
}