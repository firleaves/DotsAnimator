using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace DotsAnimator
{
    public struct StateData
    {
        public FixedString512Bytes Name;
        public int Id;
        public float NormalizedTime;

        public static StateData MakeDefault()
        {
            return new StateData()
            {
                Id = -1,
                NormalizedTime = 0,
                Name = new FixedString512Bytes()
            };
        }
    }

    public struct AnimatorStateData
    {
        public StateData SrcState;
        public StateData DstState;
        public StateData TransitionState;

        public static AnimatorStateData MakeDefault()
        {
            return new AnimatorStateData()
            {
                SrcState = StateData.MakeDefault(),
                DstState = StateData.MakeDefault(),
                TransitionState = StateData.MakeDefault()
            };
        }
    }

    #region AnimatorParamter

    public enum ParameterType
    {
        Int,
        Float,
        Bool,
        Trigger
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ParameterValue
    {
        [FieldOffset(0)] public float FloatValue;

        [FieldOffset(0)] public int IntValue;

        [FieldOffset(0)] public bool BoolValue;

        public static implicit operator ParameterValue(float f) => new() { FloatValue = f };
        public static implicit operator ParameterValue(int i) => new() { IntValue = i };
        public static implicit operator ParameterValue(bool b) => new() { BoolValue = b };
    }

    public struct ControllerParameter
    {
        public int NameHash;
        public ParameterType Type;
        public ParameterValue Value;
    }

    #endregion


    public struct ParameterBlob
    {
        public BlobArray<ControllerParameter> Parameters;
    }


    #region Translation

    public enum ConditionMode
    {
        If = 1,
        IfNot = 2,
        Greater = 3,
        Less = 4,
        Equals = 6,
        NotEqual = 7,
    }

    public struct Condition
    {
        public int ParamIndex;
        public float Threshold;
        public ConditionMode ConditionMode;
    }

    public struct TransitionBlob
    {
        public BlobString Name;
        public int Hash;
        public int DestinationStateIndex;

        public bool HasExitTime;
        public float ExitTime;
        public bool FixedDuration;
        public float TransitionDuration;
        public float Offset;
        public BlobArray<Condition> Conditions;
    }

    #endregion


    #region LayerInfo

    public struct StateBlob
    {
        public BlobString Name;
        public int NameHash;
        public float Speed;
        public int SpeedMultiplierParameterIndex;

        //默认写入动画时间长度，原来animator里面长度有多种类型，这里就用动画时间长度
        public float AnimationLength;
        public BlobArray<TransitionBlob> Transitions;
    }

    public struct LayerBlob
    {
        public int DefaultStateIndex;
        public BlobArray<StateBlob> States;
    }

    #endregion


    //现在就支持base layer
    public struct AnimatorComponent : IComponentData, IEnableableComponent
    {
        public FixedString512Bytes Name;
        public BlobAssetReference<LayerBlob> Layer;
        public AnimatorStateData AnimatorState;
    }


    public struct AnimatorParameterComponent : IBufferElementData
    {
        public FixedString512Bytes Name;
        public ControllerParameter Parameter;

        public float FloatValue
        {
            get => Parameter.Value.FloatValue;
            set => Parameter.Value.FloatValue = value;
        }

        public int IntValue
        {
            get => Parameter.Value.IntValue;
            set => Parameter.Value.IntValue = value;
        }

        public bool BoolValue
        {
            get => Parameter.Value.BoolValue;
            set => Parameter.Value.BoolValue = value;
        }

        public void SetTrigger()
        {
            Parameter.Value.BoolValue = true;
        }
    }
}