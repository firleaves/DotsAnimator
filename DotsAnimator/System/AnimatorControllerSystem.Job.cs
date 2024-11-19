using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
                if (!transitionBlob.FixedDuration)
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
                    animatorComponent.AnimatorState.SrcState.Id = layerBlob.DefaultStateIndex;
                    animatorComponent.AnimatorState.SrcState.Name = layerBlob.States[layerBlob.DefaultStateIndex].Name.ToString();
                    animatorComponent.AnimatorState.SrcState.NormalizedTime = 0;
                    currentStateIndex = layerBlob.DefaultStateIndex;

                    // ref var state = ref layerBlob.States[animatorComponent.AnimatorState.SrcState.Id];
                }

                ref var currentState = ref layerBlob.States[currentStateIndex];
                var currentStateDuration = CalcStateDuration(ref currentState, parameterBuffer);
                var dt = DeltaTime;
                animatorComponent.AnimatorState.SrcState.NormalizedTime += dt / currentStateDuration;

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

                TryExitTransition(ref animatorComponent, ref layerBlob);
                TryEnterTransition(ref animatorComponent, ref layerBlob, ref parameterBuffer);
                TryExitTransition(ref animatorComponent, ref layerBlob);
            }

            private void TryEnterTransition(ref AnimatorComponent animatorComponent, ref LayerBlob layerBlob, ref DynamicBuffer<AnimatorParameterComponent> parameterBuffer)
            {
                //如果当前正在转换就停止
                if (animatorComponent.AnimatorState.TransitionState.Id >= 0) return;

                ref var curState = ref layerBlob.States[animatorComponent.AnimatorState.SrcState.Id];
                for (int i = 0; i < curState.Transitions.Length; i++)
                {
                    ref var transition = ref curState.Transitions[i];


                    var complete = CheckTransitionTimeCompleted(ref transition, animatorComponent.AnimatorState.SrcState);
                    var conditionComplete = CheckTransitionCondition(ref transition, ref parameterBuffer);
                    if (complete && conditionComplete)
                    {

                        animatorComponent.AnimatorState.TransitionState.Id = i;
                        animatorComponent.AnimatorState.TransitionState.Name = transition.Name.ToString();
                        animatorComponent.AnimatorState.TransitionState.NormalizedTime = 0;


                        animatorComponent.AnimatorState.DstState.Id = transition.DestinationStateIndex;
                        animatorComponent.AnimatorState.DstState.NormalizedTime = transition.Offset;
                        animatorComponent.AnimatorState.DstState.Name = layerBlob.States[animatorComponent.AnimatorState.DstState.Id].Name.ToString();
                        break;
                    }
                }
            }


            private void TryExitTransition(ref AnimatorComponent animatorComponent, ref LayerBlob layerBlob)
            {
                if (animatorComponent.AnimatorState.TransitionState.Id < 0) return;

                ref var curState = ref layerBlob.States[animatorComponent.AnimatorState.SrcState.Id];
                ref var transition = ref curState.Transitions[animatorComponent.AnimatorState.TransitionState.Id];

                var stateDuration = curState.AnimationLength;

                var duration = transition.TransitionDuration;
                if (transition.FixedDuration)
                {
                    duration = stateDuration * transition.TransitionDuration;
                }

                if (animatorComponent.AnimatorState.TransitionState.NormalizedTime >= duration)
                {
                    //transition complete
                    animatorComponent.AnimatorState.SrcState = animatorComponent.AnimatorState.DstState;

                    animatorComponent.AnimatorState.DstState = StateData.MakeDefault();
                    animatorComponent.AnimatorState.TransitionState = StateData.MakeDefault();
                }
                
            }

            private bool CheckTransitionTimeCompleted(ref TransitionBlob transitionBlob, in StateData curState)
            {
                
                if (!transitionBlob.HasExitTime)
                {
                    return transitionBlob.Conditions.Length != 0;
                }

                //TODO transition 
                return curState.NormalizedTime >= transitionBlob.ExitTime;
            }


            private bool CheckTransitionCondition(ref TransitionBlob transitionBlob, ref DynamicBuffer<AnimatorParameterComponent> parameterBuffer)
            {
                if (transitionBlob.Conditions.Length == 0) return true;

                var result = true;

                var name = transitionBlob.Name.ToString();
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
                        return parameter.IntValue < condition.Threshold;
                    case ConditionMode.Less:
                        return parameter.IntValue > condition.Threshold;
                    case ConditionMode.Equals:
                        return parameter.IntValue == (int)condition.Threshold;
                    case ConditionMode.NotEqual:
                        return parameter.IntValue != (int)condition.Threshold;
                }

                return true;
            }

            private bool CheckFloatCondition(AnimatorParameterComponent parameter, ref Condition condition)
            {
                switch (condition.ConditionMode)
                {
                    case ConditionMode.Greater:
                        return parameter.FloatValue >= condition.Threshold;
                    case ConditionMode.Less:
                        return parameter.FloatValue <= condition.Threshold;
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