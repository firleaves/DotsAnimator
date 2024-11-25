using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator.GpuAnimation.Runtime
{
    public partial class AnimatorGpuAnimationSystem : SystemBase
    {
        private Dictionary<Entity, GpuAnimation> _bindingGpuAnimations = new Dictionary<Entity, GpuAnimation>();


        public void Bind(Entity entity, GpuAnimation gpuAnimation)
        {
            _bindingGpuAnimations.Add(entity, gpuAnimation);
        }

        public void Unbind(Entity entity)
        {
            _bindingGpuAnimations.Remove(entity);
        }

        protected override void OnUpdate()
        {
            foreach (var keyValue in _bindingGpuAnimations)
            {
                var animatorComponent = World.EntityManager.GetComponentData<AnimatorComponent>(keyValue.Key);
                var gpuAnimation = keyValue.Value;
                gpuAnimation.Play(animatorComponent.AnimatorState.SrcState.NameHash, animatorComponent.AnimatorState.SrcState.NormalizedTime);
            }
        }
    }
}