using Unity.Entities;
using UnityEngine;

namespace DotsAnimator.Samples
{
    public class SampleUI : MonoBehaviour
    {
        private bool _boolValue = false;
        private float _floatValue = 0;
        private int _intValue = 0;
        

        public Animator Animator;

        private const string AnimationSpeed = "AnimationSpeed";
        private const string Win = "Win";
        private const string Walk = "Walk";
        private const string Jump = "Jump";
        
        
        public void OnReset()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();

            _boolValue = false;
            _floatValue = 1f;
            _intValue = 0;
            
            system.SetAnimatorParam(AnimationSpeed, ParameterType.Float, _floatValue);
            Animator.SetFloat(AnimationSpeed, _floatValue);
            
             system.SetAnimatorParam(Walk, ParameterType.Bool, _boolValue);
            Animator.SetBool(Walk, _boolValue);
            
            system.SetAnimatorParam(Jump, ParameterType.Int, _intValue);
            Animator.SetInteger(Jump, _intValue);
        }

        public void OnBoolParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _boolValue = !_boolValue;
            system.SetAnimatorParam(Walk, ParameterType.Bool, _boolValue);
            Animator.SetBool(Walk, _boolValue);
        }

        public void OnFloatParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _floatValue = Mathf.Approximately(_floatValue, 1f) ? 4f : 1f;
            system.SetAnimatorParam(AnimationSpeed, ParameterType.Float, _floatValue);
            Animator.SetFloat(AnimationSpeed, _floatValue);
        }

        public void OnIntParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _intValue = _intValue == 0 ? 1 : 0;
            system.SetAnimatorParam(Jump, ParameterType.Int, _intValue);
            Animator.SetInteger(Jump, _intValue);
        }

        public void OnTriggerParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            system.SetAnimatorParam(Win, ParameterType.Trigger, true);
            Animator.SetTrigger(Win);
        }

        public void OnNoConditionTransition()
        {
        }

        public void OnAnyStateTransition()
        {
        }
    }
}