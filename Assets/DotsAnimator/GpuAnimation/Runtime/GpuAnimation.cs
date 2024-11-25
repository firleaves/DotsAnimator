using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DotsAnimator.GpuAnimation.Runtime
{
    public class GpuAnimation : MonoBehaviour
    {
        public AnimationData[] AnimationDatas;

        private static readonly int AnimTexturesProp = Shader.PropertyToID("_AnimationTex");
        private static readonly int AnimationInfoProp = Shader.PropertyToID("_AnimationInfo");

        private Dictionary<int, AnimationData> _animationDict;

        private AnimationData _currentAnimationData;
        private MaterialPropertyBlock _materialPropertyBlock;
        private MeshRenderer _meshRenderer;
        private Vector4 _animationInfo;

        private void Start()
        {
            _animationDict = new();
            foreach (var animationData in AnimationDatas)
            {
                _animationDict.Add(animationData.NameHash, animationData);
            }

            _meshRenderer = GetComponent<MeshRenderer>();
            _materialPropertyBlock = new();
        }


        public void Play(int nameHash, float normalizedTime)
        {
            // 
            if (nameHash != _currentAnimationData?.NameHash)
            {
                if (_animationDict.TryGetValue(nameHash, out var animationData))
                {
                    _currentAnimationData = animationData;

                    foreach (var material in _meshRenderer.materials)
                    {
                        material.SetTexture(AnimTexturesProp, _currentAnimationData.ClipTexture);
                    }
                }
            }

            if (_currentAnimationData == null)
            {
                Debug.LogError($"does not have animation({nameHash}) data");
                return;
            }

            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);

            var time = normalizedTime;
            if (_currentAnimationData.Loop)
            {
                time = math.frac(normalizedTime);
            }
            else if (normalizedTime >= 1f)
            {
                time = 1f;
            }

            _animationInfo.x = Mathf.Min((int)(time * _currentAnimationData.FrameCount), _currentAnimationData.FrameCount - 1);
            _materialPropertyBlock.SetVector(AnimationInfoProp, _animationInfo);
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }
}