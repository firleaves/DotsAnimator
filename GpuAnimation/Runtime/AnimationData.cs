using System;
using UnityEngine;

namespace DotsAnimator.GpuAnimation.Runtime
{
    [Serializable]
    public class AnimationData : ScriptableObject
    {
        public string Name;

        public Texture2D ClipTexture;

        public int FrameCount;

        public bool Loop;
    }
}