using System.Collections.Generic;
using DotsAnimator.Hybrid;
using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator
{
    [RequireMatchingQueriesForUpdate]
    public partial class DotsAnimatorFactory : SystemBase
    {
        private NativeHashMap<int, Entity> _entityMap;

        public Entity Instantiate(FixedString512Bytes name)
        {
            var nameHash = name.GetHashCode();
            if (_entityMap.TryGetValue(nameHash, out var entity))
            {
                return World.EntityManager.Instantiate(entity);
            }

            return Entity.Null;
        }


        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<AnimatorEntityComponent>();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<AnimatorEntityComponent>();

            _entityMap = new NativeHashMap<int, Entity>(buffer.Length, Allocator.Persistent);

            foreach (var animatorEntityComponent in buffer)
            {
                var nameHash = SystemAPI.GetComponent<AnimatorComponent>(animatorEntityComponent.Entity).NameHash;
                _entityMap.Add(nameHash, animatorEntityComponent.Entity);
            }
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();

            _entityMap.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!_entityMap.IsCreated)
            {
                Initialize();
            }
        }
    }
}