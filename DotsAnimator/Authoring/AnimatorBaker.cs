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
                //TODO any状态切过来没时间做先不支持
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
                    Debug.LogError("找不到 animator controller");
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var runtimeController = animator.runtimeAnimatorController;
                var animatorController = runtimeController as UnityEditor.Animations.AnimatorController;

                _animatorControllerInfo = CollectAnimatorInfo(animatorController);
                var layerBlob = CreateLayerBlob();
                // var parameterBlob = CreateParameterBlob();


                var component = new AnimatorComponent()
                {
                    Layer = layerBlob,
                    // Parameters = parameterBlob,
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
                result.DefaultState = layer.stateMachine.defaultState;
                result.States = layer.stateMachine.states.Select(state => state.state).ToList();
                result.Parameters = controller.parameters.ToList();
                result.AnyTransitions = layer.stateMachine.anyStateTransitions.ToList();
                return result;
            }


            // private void AddNameHash2IndexBlob(Entity entity, List<int2> indexTable)
            // {
            //     var builder = new BlobBuilder(Allocator.Temp);
            //     ref ParameterNameHash2IndexBlob blob = ref builder.ConstructRoot<ParameterNameHash2IndexBlob>();
            //
            //     var arrayBuilder = builder.Allocate(ref blob.Value, indexTable.Count);
            //     for (var i = 0; i < indexTable.Count; i++)
            //     {
            //         arrayBuilder[i] = indexTable[i];
            //     }
            //
            //     var result = builder.CreateBlobAssetReference<ParameterNameHash2IndexBlob>(Allocator.Persistent);
            //
            //     AddComponent<ParameterNameHash2IndexComponent>(entity, new ParameterNameHash2IndexComponent()
            //     {
            //         Table = result
            //     });
            //
            //     builder.Dispose();
            // }

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
                stateBuilder.AllocateString(ref stateBlob.Name, animatorState.name);

                if (animatorState.motion is not AnimationClip clip)
                {
                    throw new Exception($"状态{animatorState.name}里面没有动画片段文件，请检查animator文件");
                }

                stateBlob.AnimationLength = clip.length;
                stateBlob.NameHash = animatorState.nameHash;
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


                builder.AllocateString(ref transitionBlob.Name, animatorStateTransition.name);

                transitionBlob.Hash = animatorStateTransition.GetHashCode();
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


                // var indexTable = new List<int2>();
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
                        Name = new FixedString512Bytes(sourceParam.name),
                        Parameter = parameter,
                    };

                    buffer.Add(component);

                    // indexTable.Add(new int2(parameter.NameHash, i));
                }

                // AddNameHash2IndexBlob(entity, indexTable);
            }
        }
    }
}
#endif