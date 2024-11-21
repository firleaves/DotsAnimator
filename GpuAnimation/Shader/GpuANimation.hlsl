
#ifndef GPU_ANIMATION_INCLUDED
#define GPU_ANIMATION_INCLUDED



float3 SampleGpuAnimationPosition(Texture2D animationTex, uint vertexId,float4 _AnimationInfo)
{
    return animationTex.Load(int3(_AnimationInfo.x , vertexId, 0));
}

#endif
