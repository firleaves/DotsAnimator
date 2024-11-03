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
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public int IntValue;

        [FieldOffset(0)]
        public bool BoolValue;

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
        /// <summary>
        ///   <para>The condition is true when the parameter value is true.</para>
        /// </summary>
        If = 1,

        /// <summary>
        ///   <para>The condition is true when the parameter value is false.</para>
        /// </summary>
        IfNot = 2,

        /// <summary>
        ///   <para>The condition is true when parameter value is greater than the threshold.</para>
        /// </summary>
        Greater = 3,

        /// <summary>
        ///   <para>The condition is true when the parameter value is less than the threshold.</para>
        /// </summary>
        Less = 4,

        /// <summary>
        ///   <para>The condition is true when parameter value is equal to the threshold.</para>
        /// </summary>
        Equals = 6,

        /// <summary>
        ///   <para>The condition is true when the parameter value is not equal to the threshold.</para>
        /// </summary>
        NotEqual = 7,
    }

    public struct Condition
    {
        public int ParamIndex;
        public ParameterValue Threshold;
        public ConditionMode ConditionMode;
    }

    public struct TransitionBlob
    {
// #if UNITY_EDITOR
        public BlobString Name;
// #endif

        public int Hash;

        public int DestinationStateIndex;

        //有些属性没时间做，是不生效的
        public bool HasExitTime;
        public float ExitTime;
        public bool HasFixedDuration;
        public float TransitionDuration;
        public float Offset;
        public BlobArray<Condition> Conditions;
    }

    #endregion


    #region LayerInfo

    //暂时就支持速度更改
    public struct StateBlob
    {
// #if UNITY_EDITOR
        public BlobString Name;
// #endif

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


    //现在就支持base layer，后面如果需要多层layer再搞，weight现在也不需要
    public struct AnimatorComponent : IComponentData, IEnableableComponent
    {
        // public BlobAssetReference<ParameterBlob> Parameters;
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

    // public struct ParameterNameHash2IndexBlob
    // {
    //     //x 名字hash值
    //     //y 参数所在buff的索引
    //     public BlobArray<int2> Value;
    // }

    // public struct ParameterNameHash2IndexComponent : IComponentData
    // {
    //     public BlobAssetReference<ParameterNameHash2IndexBlob> Table;
    // }


    // public struct GpuAnimationId2AnimatorStateIndexBlob : IComponentData
    // {
    //     //x gpuanimation id
    //     //y animtor state name hash
    //     public BlobArray<int2> Index2BlobIndex;
    // }
}