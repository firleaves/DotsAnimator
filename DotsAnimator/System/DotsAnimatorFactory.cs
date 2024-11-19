using System.Collections.Generic;
using DotsAnimator.Hybrid;
using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator
{
    public partial class DotsAnimatorFactory : SystemBase
    {
        private NativeHashMap<FixedString512Bytes, Entity> _entityMap;

        public Entity Instantiate(FixedString512Bytes name)
        {
            if (_entityMap.TryGetValue(name, out var entity))
            {
                return World.EntityManager.Instantiate(entity);
            }

            return Entity.Null;
        }


        protected override void OnCreate()
        {
            base.OnCreate();

            var buffer = SystemAPI.GetSingletonBuffer<AnimatorEntityComponent>();

            _entityMap = new NativeHashMap<FixedString512Bytes, Entity>(buffer.Length, Allocator.Persistent);

            foreach (var animatorEntityComponent in buffer)
            {
                var name = SystemAPI.GetComponent<AnimatorComponent>(animatorEntityComponent.Entity).Name;
                _entityMap.Add(name, animatorEntityComponent.Entity);
            }
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();

            _entityMap.Dispose();
        }

        protected override void OnUpdate()
        {
        }
    }
}