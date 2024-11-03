using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Collections;

namespace DotsAnimator
{
   public struct AnimatorParameter
    {
        public FixedString512Bytes ParameterName;
        public int Hash;

        public AnimatorParameter(FixedString512Bytes name)
        {
            Hash = name.GetHashCode();
            ParameterName = name;
        }

        public AnimatorParameter(int hash)
        {
            this.Hash = hash;
            ParameterName = default;
        }


        bool GetRuntimeParameterDataInternal(int paramIndex, DynamicBuffer<AnimatorParameterComponent> runtimeParameters, out ParameterValue outData)
        {
            bool isValid = paramIndex >= 0;

            outData = isValid ? runtimeParameters[paramIndex].Parameter.Value : default;

            return isValid;
        }


        public bool GetRuntimeParameterData(DynamicBuffer<AnimatorParameterComponent> runtimeParameters, out ParameterValue outData)
        {
            var paramIndex = GetRuntimeParameterIndex(Hash, runtimeParameters);
            return GetRuntimeParameterDataInternal(paramIndex, runtimeParameters, out outData);
        }


        bool SetRuntimeParameterDataInternal(int paramIndex, DynamicBuffer<AnimatorParameterComponent> runtimeParameters, in ParameterValue paramData)
        {
            bool isValid = paramIndex >= 0;

            if (isValid)
            {
                var p = runtimeParameters[paramIndex];
                p.Parameter.Value = paramData;
                runtimeParameters[paramIndex] = p;
            }

            return isValid;
        }


        public bool SetRuntimeParameterData(DynamicBuffer<AnimatorParameterComponent> runtimeParameters, in ParameterValue paramData)
        {
            var paramIndex = GetRuntimeParameterIndex(Hash, in runtimeParameters);
            if (paramIndex == -1)
            {
                Debug.LogWarning($"没有找到改Parameter位置 {ParameterName.ToString()}");
                return false;
            }

            return SetRuntimeParameterDataInternal(paramIndex, runtimeParameters, paramData);
        }


        public bool SetTrigger(DynamicBuffer<AnimatorParameterComponent> runtimeParameters) =>
            SetRuntimeParameterData(runtimeParameters, new ParameterValue() { BoolValue = true });

        
        public static int GetRuntimeParameterIndex(int hash, in DynamicBuffer<AnimatorParameterComponent> parameters)
        {
            for (int i = 0; i < parameters.Length; ++i)
            {
                var p = parameters[i];
                if (p.Parameter.NameHash == hash)
                    return i;
            }

            return -1;
        }
    }

}