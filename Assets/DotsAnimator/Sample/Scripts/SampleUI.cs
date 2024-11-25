using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DotsAnimator.Samples
{
    public class SampleUI : MonoBehaviour
    {
        private bool _boolValue = false;
        private float _floatValue = 0;
        private int _intValue = 0;


        // public Animator Animator;

        private const string AnimationSpeed = "AnimationSpeed";
        private const string Win = "Win";
        private const string Walk = "Walk";
        private const string Pickup = "Pickup";


        public GameObject Prefab;


        public void OnGenerate()
        {
            var random = new Random(100);
            var count = 1000;
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(Prefab);
                go.transform.position = random.NextFloat3(float3.zero, new float3(10, 0, 10));
            }
        }


        public void Play()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            system.Play("jump-up");
        }

        public void OnReset()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();

            _boolValue = false;
            _floatValue = 1f;
            _intValue = 0;

            system.SetAnimatorParam(AnimationSpeed, ParameterType.Float, _floatValue);
            // Animator.SetFloat(AnimationSpeed, _floatValue);
            system.SetAnimatorParam(Walk, ParameterType.Bool, _boolValue);
            // Animator.SetBool(Walk, _boolValue);
            system.SetAnimatorParam(Pickup, ParameterType.Int, _intValue);
            // Animator.SetInteger(Jump, _intValue);
        }

        public void OnBoolParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _boolValue = !_boolValue;
            system.SetAnimatorParam(Walk, ParameterType.Bool, _boolValue);
            // Animator.SetBool(Walk, _boolValue);
        }

        public void OnFloatParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _floatValue = Mathf.Approximately(_floatValue, 1f) ? 4f : 1f;
            system.SetAnimatorParam(AnimationSpeed, ParameterType.Float, _floatValue);
            // Animator.SetFloat(AnimationSpeed, _floatValue);
        }

        public void OnIntParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            _intValue = _intValue == 0 ? 1 : 0;
            system.SetAnimatorParam(Pickup, ParameterType.Int, _intValue);
            // Animator.SetInteger(Jump, _intValue);
        }

        public void OnTriggerParam()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SampleControllerSystem>();
            system.SetAnimatorParam(Win, ParameterType.Trigger, true);
            // Animator.SetTrigger(Win);
        }

      
    }
}