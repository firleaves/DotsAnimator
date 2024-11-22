using Unity.Entities;

namespace DotsAnimator.Samples
{
    public partial class SampleControllerSystem : SystemBase
    {

        public void SetAnimatorParam(string paramName, ParameterType parameterType, ParameterValue value)
        {
            Entities.ForEach(( DynamicBuffer<AnimatorParameterComponent> parameterComponents) =>
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

        protected override void OnUpdate()
        {
        }
    }
}