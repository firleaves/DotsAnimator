using System;
using System.Collections.Generic;
using UnityEngine;

namespace DotsAnimator.GpuAnimation.Runtime
{
    public class GpuAnimation : MonoBehaviour
    {
        private static readonly int AnimTexturesProp = Shader.PropertyToID("_AnimationTexture");
        
        
        
        
        public AnimationData[] AnimationDatas;

        private Dictionary<int, AnimationData> _animationDict;

        private AnimationData _currentAnimationData;
        
        private MaterialPropertyBlock _materialPropertyBlock;
        private MeshRenderer _meshRenderer;

        private void Start()
        {
            _animationDict = new();
            foreach (var animationData in AnimationDatas)
            {
                _animationDict.Add(animationData.AnimatorStateId, animationData);
            }
            
            _meshRenderer = GetComponent<MeshRenderer>();
            _materialPropertyBlock = new();

        }

        private void Update()
        {
            if (_currentAnimationData != null)
            {
            }
        }


        public void Play(int animationId, float normalizedTime)
        {
            if (animationId != _currentAnimationData?.AnimatorStateId)
            {
                if (_animationDict.TryGetValue(animationId, out var animationData))
                {
                    _currentAnimationData = animationData;
                }
            }
            
        }
    }
}