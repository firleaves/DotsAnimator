using System;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace DotsAnimator.GpuAnimation.Runtime
{
    [RequireComponent(typeof(GpuAnimation))]
    public class DotsAnimator : MonoBehaviour
    {
        public string AnimatorName;

        private Entity _animatorEntity;

        private async void Start()
        {
            
            var dotsAnimatorFactory = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DotsAnimatorFactory>();
            _animatorEntity = dotsAnimatorFactory.Instantiate(AnimatorName);

            if (_animatorEntity == Entity.Null)
            {
                Debug.LogError($"{AnimatorName} not found");
                return;
            }


            var gpuAnimation = GetComponent<GpuAnimation>();
            var animatorGpuAnimationSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AnimatorGpuAnimationSystem>();
            animatorGpuAnimationSystem.Bind(_animatorEntity, gpuAnimation);
        }

        private void OnDestroy()
        {
            if (_animatorEntity == Entity.Null || World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            var animatorGpuAnimationSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<AnimatorGpuAnimationSystem>();
            if (animatorGpuAnimationSystem == null) return;
            animatorGpuAnimationSystem.Unbind(_animatorEntity);

            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(_animatorEntity);
        }
    }
}