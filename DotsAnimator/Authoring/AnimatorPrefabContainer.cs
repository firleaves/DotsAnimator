using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DotsAnimator.Hybrid
{
    public class AnimatorPrefabContainer : MonoBehaviour
    {
        public List<GameObject> AnimatorGameObjects;

        private class AnimatorPrefabContainerBaker : Baker<AnimatorPrefabContainer>
        {
            public override void Bake(AnimatorPrefabContainer authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var buffer = AddBuffer<AnimatorEntityComponent>(entity);
                
                foreach (var go in authoring.AnimatorGameObjects)
                {
                    buffer.Add(new AnimatorEntityComponent() { Entity = GetEntity(go, TransformUsageFlags.None)});
                }
            }
        }
    }


    public struct AnimatorEntityComponent : IBufferElementData
    {
        public Entity Entity;
    }
}