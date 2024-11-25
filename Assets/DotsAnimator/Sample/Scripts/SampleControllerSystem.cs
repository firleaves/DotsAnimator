using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator.Samples
{
    public partial class SampleControllerSystem : SystemBase
    {
        public void SetAnimatorParam(string paramName, ParameterType parameterType, ParameterValue value)
        {
            Entities.ForEach((DynamicBuffer<AnimatorParameterComponent> parameterComponents) =>
                {
                    var parameter = new AnimatorParameter(paramName);
                    if (parameterType == ParameterType.Trigger)
                    {
                        parameter.SetTrigger(parameterComponents);
                    }
                    else
                    {
                        parameter.SetRuntimeParameterData(parameterComponents, value);
                    }
                })
                .WithoutBurst().Run();
        }

        public void Play(string name)
        {
            var nameHash = new FixedString512Bytes(name).GetHashCode();
            Entities.ForEach((ref AnimatorComponent animatorComponent) =>
                {
                    ref var states = ref animatorComponent.Layer.Value.States;
                    var length = states.Length;
                    for (int i = 0; i < length; i++)
                    {
                        ref var state = ref states[i];
                        if (nameHash == state.NameHash)
                        {
                            animatorComponent.AnimatorState.SrcState.Id = i;
                            animatorComponent.AnimatorState.SrcState.NameHash = nameHash;
#if DOTANIMTOR_DEBUG
                            animatorComponent.AnimatorState.SrcState.Name = name;
#endif
                            animatorComponent.AnimatorState.SrcState.NormalizedTime = 0;

                            animatorComponent.AnimatorState.DstState = StateData.MakeDefault();
                            animatorComponent.AnimatorState.TransitionState = StateData.MakeDefault();
                            break;
                        }
                    }
                })
                .WithoutBurst().Run();
        }


        protected override void OnUpdate()
        {
        }
    }
}