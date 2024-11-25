using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator
{
    public partial struct AnimatorControllerSystem : ISystem
    {
        private EntityQuery _entityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<AnimatorComponent>();
            _entityQuery = state.GetEntityQuery(entityQueryBuilder);
            state.RequireForUpdate(_entityQuery);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            var animatorComponentTypeHandle = SystemAPI.GetComponentTypeHandle<AnimatorComponent>();
            var parametersBufferHandle = SystemAPI.GetBufferTypeHandle<AnimatorParameterComponent>();
            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();

            state.Dependency = new AnimatorStateMachineJob()
            {
                DeltaTime = dt,
                AnimatorComponentTypeHandle = animatorComponentTypeHandle,
                ParametersBufferHandle = parametersBufferHandle,
                EntityTypeHandle = entityTypeHandle
            }.ScheduleParallel(_entityQuery, state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}