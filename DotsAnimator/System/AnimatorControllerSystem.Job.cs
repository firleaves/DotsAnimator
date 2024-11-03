using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsAnimator
{
    public partial struct AnimatorControllerSystem
    {
        private partial struct AnimatorStateMachineJob : IJobChunk
        {
            public ComponentTypeHandle<AnimatorComponent> AnimatorComponentTypeHandle;
            public BufferTypeHandle<AnimatorParameterComponent> ParametersBufferHandle;
            public EntityTypeHandle EntityTypeHandle;
            public float DeltaTime;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animatorComponents = chunk.GetNativeArray(ref AnimatorComponentTypeHandle);
                var parameterBuffers = chunk.GetBufferAccessor(ref ParametersBufferHandle);
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    var entity = entities[i];
                    AnimatorComponent animatorComponent = animatorComponents[i];
                    var parameterBuffer = parameterBuffers[i];
                    ExecuteEntityAnimatorSM(entity, ref animatorComponent, ref parameterBuffer, ref animatorComponent.Layer.Value);

                    animatorComponents[i] = animatorComponent;
                }
            }

            private float CalcStateDuration(ref StateBlob stateBlob, in DynamicBuffer<AnimatorParameterComponent> parameterBuffer)
            {
                var speed = stateBlob.Speed;
                var speedMultiplier = 1.0f;
                if (stateBlob.SpeedMultiplierParameterIndex >= 0)
                {
                    speedMultiplier = math.max(0.0001f, parameterBuffer[stateBlob.SpeedMultiplierParameterIndex].FloatValue);
                }

                return stateBlob.AnimationLength / (speed * speedMultiplier);
            }

            private float CalcTransitionDuration(ref TransitionBlob transitionBlob, float curStateDuration)
            {
                var duration = transitionBlob.TransitionDuration;
                if (!transitionBlob.HasFixedDuration)
                {
                    //非固定时间就是百分比
                    duration *= curStateDuration;
                }

                return math.max(duration, 0.001f);
            }

            private void ExecuteEntityAnimatorSM(Entity entity,
                ref AnimatorComponent animatorComponent,
                ref DynamicBuffer<AnimatorParameterComponent> parameterBuffer,
                ref LayerBlob layerBlob)
            {
                var currentStateIndex = animatorComponent.AnimatorState.SrcState.Id;
                if (animatorComponent.AnimatorState.SrcState.Id < 0)
                {
                    //进入第一个状态

                    animatorComponent.AnimatorState.SrcState.Id = layerBlob.DefaultStateIndex;
// #if UNITY_EDITOR
                    animatorComponent.AnimatorState.SrcState.Name = layerBlob.States[layerBlob.DefaultStateIndex].Name.ToString();
// #endif
                    // Debug.Log($"初始状态 {animatorComponent.AnimatorState.SrcState.Name.ToString()}");
                    animatorComponent.AnimatorState.SrcState.NormalizedTime = 0;
                    currentStateIndex = layerBlob.DefaultStateIndex;

                    ref var state = ref layerBlob.States[animatorComponent.AnimatorState.SrcState.Id];
                    // Debug.Log($"切换到状态 {state.Name.ToString()}");
                }

                ref var currentState = ref layerBlob.States[currentStateIndex];
                var currentStateDuration = CalcStateDuration(ref currentState, parameterBuffer);
                var dt = DeltaTime;
                animatorComponent.AnimatorState.SrcState.NormalizedTime += dt / currentStateDuration;

                //下个状态
                if (animatorComponent.AnimatorState.DstState.Id >= 0)
                {
                    ref var dstState = ref layerBlob.States[animatorComponent.AnimatorState.DstState.Id];
                    var dstStateDuration = CalcStateDuration(ref dstState, parameterBuffer);
                    animatorComponent.AnimatorState.DstState.NormalizedTime += dt / dstStateDuration;
                }

                if (animatorComponent.AnimatorState.TransitionState.Id >= 0)
                {
                    ref var currentTransitionBlob = ref currentState.Transitions[animatorComponent.AnimatorState.TransitionState.Id];
                    var transitionDuration = CalcTransitionDuration(ref currentTransitionBlob, currentStateDuration);
                    animatorComponent.AnimatorState.TransitionState.NormalizedTime += dt / transitionDuration;
                }

                TryExitTransition(ref animatorComponent);
                TryEnterTransition(ref animatorComponent, ref layerBlob, ref parameterBuffer);
                TryExitTransition(ref animatorComponent);
            }

            private void TryEnterTransition(ref AnimatorComponent animatorComponent, ref LayerBlob layerBlob, ref DynamicBuffer<AnimatorParameterComponent> parameterBuffer)
            {
                //如果当前正在转换就停止
                if (animatorComponent.AnimatorState.TransitionState.Id >= 0) return;

                ref var curState = ref layerBlob.States[animatorComponent.AnimatorState.SrcState.Id];
                for (int i = 0; i < curState.Transitions.Length; i++)
                {
                    ref var transition = ref curState.Transitions[i];

                    if (CheckTransitionTimeCompleted(ref transition, animatorComponent.AnimatorState.SrcState) && CheckTransitionCondition(ref transition, ref parameterBuffer))
                    {
                        // 可以进入新状态转换

                        animatorComponent.AnimatorState.TransitionState.Id = i;
// #if UNITY_EDITOR
                        animatorComponent.AnimatorState.TransitionState.Name = transition.Name.ToString();
// #endif
                        animatorComponent.AnimatorState.TransitionState.NormalizedTime = 0;


                        //直接切状态
                        animatorComponent.AnimatorState.DstState.Id = transition.DestinationStateIndex;
// #if UNITY_EDITOR
                        animatorComponent.AnimatorState.DstState.Name = layerBlob.States[animatorComponent.AnimatorState.DstState.Id].Name.ToString();
// #endif
                        ref var state = ref layerBlob.States[animatorComponent.AnimatorState.DstState.Id];
                        // var dstStateDuration = CalcStateDuration(ref layerBlob.States[ transition.DestinationStateIndex], parameterBuffer);
                        animatorComponent.AnimatorState.DstState.NormalizedTime = 0;
                        break;
                    }
                }
            }


            private void TryExitTransition(ref AnimatorComponent animatorComponent)
            {
                if (animatorComponent.AnimatorState.TransitionState.Id < 0) return;
                if (animatorComponent.AnimatorState.TransitionState.NormalizedTime >= 1)
                {
                    //转换状态完成。设置当前状态信息
                    animatorComponent.AnimatorState.SrcState = animatorComponent.AnimatorState.DstState;

                    //清空下个状态和转换状态
                    animatorComponent.AnimatorState.DstState = StateData.MakeDefault();
                    animatorComponent.AnimatorState.TransitionState = StateData.MakeDefault();
                }
            }

            private bool CheckTransitionTimeCompleted(ref TransitionBlob transitionBlob, in StateData curState)
            {
                var normalizedDuration = curState.NormalizedTime;
                //没有条件，没有退出时间，直接转换成功
                if (!transitionBlob.HasExitTime || transitionBlob.Conditions.Length == 0)
                {
                    return true;
                }

                return normalizedDuration >= 1;
            }


            private bool CheckTransitionCondition(ref TransitionBlob transitionBlob, ref DynamicBuffer<AnimatorParameterComponent> parameterBuffer)
            {
                if (transitionBlob.Conditions.Length == 0) return true;

                var result = true;
// #if UNITY_EDITOR
//                 var name = transitionBlob.Name.ToString();
// #endif
                // Debug.Log($"检查状态切换  {name}");
                for (int i = 0; i < transitionBlob.Conditions.Length && result; i++)
                {
                    ref var condition = ref transitionBlob.Conditions[i];
                    var param = parameterBuffer[condition.ParamIndex];

                    switch (param.Parameter.Type)
                    {
                        case ParameterType.Float:
                            result = CheckFloatCondition(param, ref condition);
                            break;
                        case ParameterType.Int:
                            result = CheckIntCondition(param, ref condition);
                            break;
                        case ParameterType.Bool:
                            result = CheckBoolCondition(param, ref condition);
                            break;
                        case ParameterType.Trigger:
                            result = CheckBoolCondition(param, ref condition);
                            if (result)
                            {
                                //重置状态
                                param.BoolValue = false;
                                parameterBuffer[condition.ParamIndex] = param;
                            }

                            break;
                    }
                }

                return result;
            }

            private bool CheckIntCondition(AnimatorParameterComponent parameter, ref Condition condition)
            {
                switch (condition.ConditionMode)
                {
                    case ConditionMode.Greater:
                        return parameter.IntValue < condition.Threshold.FloatValue;
                    case ConditionMode.Less:
                        return parameter.IntValue > condition.Threshold.FloatValue;
                    case ConditionMode.Equals:
                        return parameter.IntValue == (int)condition.Threshold.FloatValue;
                    case ConditionMode.NotEqual:
                        return parameter.IntValue != (int)condition.Threshold.FloatValue;
                }

                return true;
            }

            private bool CheckFloatCondition(AnimatorParameterComponent parameter, ref Condition condition)
            {
                switch (condition.ConditionMode)
                {
                    case ConditionMode.Greater:
                        return parameter.FloatValue <= condition.Threshold.FloatValue;
                    case ConditionMode.Less:
                        return parameter.FloatValue >= condition.Threshold.FloatValue;
                }

                return true;
            }

            private bool CheckBoolCondition(AnimatorParameterComponent parameter, ref Condition condition)
            {
                switch (condition.ConditionMode)
                {
                    case ConditionMode.If:
                        return parameter.BoolValue;
                    case ConditionMode.IfNot:
                        return !parameter.BoolValue;
                }

                return true;
            }
        }
    }
}