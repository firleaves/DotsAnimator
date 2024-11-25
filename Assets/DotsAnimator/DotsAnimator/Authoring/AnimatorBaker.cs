#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.Animations;
using UnityEngine;


namespace DotsAnimator.Hybrid
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorBaker : MonoBehaviour
    {
        public class Baker : Baker<AnimatorBaker>
        {
            internal class AnimatorControllerInfo
            {
                public string AnimatorName;
                public List<AnimatorStateTransition> AnyTransitions;
                public List<AnimatorState> States;
                public AnimatorState DefaultState;
                public List<AnimatorControllerParameter> Parameters;
            }

            private AnimatorControllerInfo _animatorControllerInfo;

            public override void Bake(AnimatorBaker authoring)
            {
                var animator = authoring.GetComponent<Animator>();

                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogError("does not found animator controller");
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var runtimeController = animator.runtimeAnimatorController;
                var animatorController = runtimeController as UnityEditor.Animations.AnimatorController;

                _animatorControllerInfo = CollectAnimatorInfo(animatorController);
                var layerBlob = CreateLayerBlob();

                var component = new AnimatorComponent()
                {
#if DOTANIMTOR_DEBUG
                    Name = runtimeController.name,
#endif
                    NameHash = new FixedString512Bytes(runtimeController.name).GetHashCode(),
                    Layer = layerBlob,
                    AnimatorState = AnimatorStateData.MakeDefault()
                };
                AddComponent<AnimatorComponent>(entity, component);
                AddAnimatorControllerParameterComponent(entity);
                Debug.Log("Animator baker complete");
            }


            private AnimatorControllerInfo CollectAnimatorInfo(UnityEditor.Animations.AnimatorController controller)
            {
                var result = new AnimatorControllerInfo();
                //这里就用base layer
                var layer = controller.layers[0];
                result.AnimatorName = controller.name;
                result.DefaultState = layer.stateMachine.defaultState;
                result.States = layer.stateMachine.states.Select(state => state.state).ToList();
                result.Parameters = controller.parameters.ToList();
                result.AnyTransitions = layer.stateMachine.anyStateTransitions.ToList();
                return result;
            }

            private BlobAssetReference<LayerBlob> CreateLayerBlob()
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref LayerBlob layerBlob = ref builder.ConstructRoot<LayerBlob>();

                layerBlob.DefaultStateIndex = _animatorControllerInfo.DefaultState == null ? -1 : _animatorControllerInfo.States.IndexOf(_animatorControllerInfo.DefaultState);
                var arrayBuilder = builder.Allocate(ref layerBlob.States, _animatorControllerInfo.States.Count);
                for (var i = 0; i < _animatorControllerInfo.States.Count; i++)
                {
                    var childState = _animatorControllerInfo.States[i];
                    CreateStateBlob(childState, ref builder, ref arrayBuilder[i]);
                }

                var result = builder.CreateBlobAssetReference<LayerBlob>(Allocator.Persistent);
                builder.Dispose();

                return result;
            }

            private void CreateStateBlob(AnimatorState animatorState, ref BlobBuilder stateBuilder, ref StateBlob stateBlob)
            {
#if DOTANIMTOR_DEBUG
                stateBuilder.AllocateString(ref stateBlob.Name, animatorState.name);
#endif
                if (animatorState.motion is not AnimationClip clip)
                {
                    throw new Exception($"{animatorState.name} does not has animation clip");
                }

                stateBlob.NameHash = new FixedString128Bytes(animatorState.name).GetHashCode();
                stateBlob.AnimationLength = clip.length;
                stateBlob.Speed = animatorState.speed;
                stateBlob.SpeedMultiplierParameterIndex = _animatorControllerInfo.Parameters.FindIndex(parameter => parameter.name == animatorState.speedParameter);


                var arrayTransitions = stateBuilder.Allocate(ref stateBlob.Transitions, animatorState.transitions.Length);
                for (int i = 0; i < animatorState.transitions.Length; i++)
                {
                    var animatorStateTransition = animatorState.transitions[i];
                    CreateTransitionBlob(animatorStateTransition, ref stateBuilder, ref arrayTransitions[i]);
                }
            }

            private void CreateTransitionBlob(AnimatorStateTransition animatorStateTransition, ref BlobBuilder builder, ref TransitionBlob transitionBlob)
            {
                var conditionArray = builder.Allocate(ref transitionBlob.Conditions, animatorStateTransition.conditions.Length);
                for (int i = 0; i < animatorStateTransition.conditions.Length; i++)
                {
                    ref var condition = ref conditionArray[i];
                    var originCondition = animatorStateTransition.conditions[i];
                    condition.ConditionMode = (ConditionMode)originCondition.mode;
                    condition.ParamIndex = _animatorControllerInfo.Parameters.FindIndex(parameter => parameter.name == originCondition.parameter);
                    condition.Threshold = originCondition.threshold;
                }

#if DOTSANIMTOR_DEBUG
                builder.AllocateString(ref transitionBlob.Name, animatorStateTransition.name);
#endif
                transitionBlob.NameHash = new FixedString512Bytes(animatorStateTransition.name).GetHashCode();
                transitionBlob.TransitionDuration = animatorStateTransition.duration;
                transitionBlob.ExitTime = animatorStateTransition.exitTime;
                transitionBlob.HasExitTime = animatorStateTransition.hasExitTime;
                transitionBlob.FixedDuration = animatorStateTransition.hasFixedDuration;
                transitionBlob.Offset = animatorStateTransition.offset;
                transitionBlob.DestinationStateIndex = _animatorControllerInfo.States.IndexOf(animatorStateTransition.destinationState);
            }


            private BlobAssetReference<ParameterBlob> CreateParameterBlob()
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var parameterBlob = ref builder.ConstructRoot<ParameterBlob>();
                var parameterArray = builder.Allocate(ref parameterBlob.Parameters, _animatorControllerInfo.Parameters.Count);
                for (int i = 0; i < _animatorControllerInfo.Parameters.Count; i++)
                {
                    ref var parameter = ref parameterArray[i];
                    var sourceParam = _animatorControllerInfo.Parameters[i];

                    switch (sourceParam.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            parameter.Type = ParameterType.Float;
                            parameter.Value.FloatValue = sourceParam.defaultFloat;
                            break;
                        case AnimatorControllerParameterType.Int:
                            parameter.Type = ParameterType.Int;
                            parameter.Value.IntValue = sourceParam.defaultInt;
                            break;
                        case AnimatorControllerParameterType.Bool:
                            parameter.Type = ParameterType.Bool;
                            parameter.Value.BoolValue = sourceParam.defaultBool;
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            parameter.Type = ParameterType.Trigger;
                            parameter.Value.BoolValue = sourceParam.defaultBool;
                            break;
                    }

                    parameter.NameHash = new FixedString512Bytes(sourceParam.name).GetHashCode();
                }

                var result = builder.CreateBlobAssetReference<ParameterBlob>(Allocator.Persistent);
                builder.Dispose();

                return result;
            }


            private void AddAnimatorControllerParameterComponent(Entity entity)
            {
                var buffer = AddBuffer<AnimatorParameterComponent>(entity);

                for (int i = 0; i < _animatorControllerInfo.Parameters.Count; i++)
                {
                    var sourceParam = _animatorControllerInfo.Parameters[i];
                    var parameter = new ControllerParameter();
                    switch (sourceParam.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            parameter.Type = ParameterType.Float;
                            parameter.Value.FloatValue = sourceParam.defaultFloat;
                            break;
                        case AnimatorControllerParameterType.Int:
                            parameter.Type = ParameterType.Int;
                            parameter.Value.IntValue = sourceParam.defaultInt;
                            break;
                        case AnimatorControllerParameterType.Bool:
                            parameter.Type = ParameterType.Bool;
                            parameter.Value.BoolValue = sourceParam.defaultBool;
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            parameter.Type = ParameterType.Trigger;
                            parameter.Value.BoolValue = sourceParam.defaultBool;
                            break;
                    }

                    parameter.NameHash = new FixedString512Bytes(sourceParam.name).GetHashCode();
                    var component = new AnimatorParameterComponent()
                    {
#if DOTANIMTOR_DEBUG
                        Name = new FixedString512Bytes(sourceParam.name),
#endif
                        Parameter = parameter,
                    };
                    buffer.Add(component);
                }
            }
        }
    }
}
#endif