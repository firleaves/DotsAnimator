using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace DotsAnimator.GpuAnimation
{
    public class GpuAnimationBakerTools
    {
        public GameObject TargetObject;

        public AnimationClip SampleAnimationClip;

        [MenuItem("Assets/DotsAnimator/Bake Gpu Animation")]
        public static void ConvertSelectObject()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null)
                    {
                        Convert(prefab);
                        return;
                    }
                }
            }

            Debug.LogError("Please select a prefab.");
        }

        private static void Convert(GameObject go)
        {
            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                throw new System.Exception("GameObject must have an Animator.");
            }

            Debug.LogWarning("Only supports baking the default layer animation.");


            var animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (animatorController == null)
            {
                throw new System.Exception("Animator must have an AnimatorController.");
            }

            var layer = animatorController.layers[0];

            var animationClips = new List<AnimationClip>();
            foreach (var childAnimatorState in layer.stateMachine.states)
            {
                var animationClip = childAnimatorState.state.motion as AnimationClip;
                if (animationClip == null)
                {
                    Debug.LogError($"{childAnimatorState.state.name} does not have an animation file. Please check the animator configuration.");
                    continue;
                }

                animationClips.Add(animationClip);
            }


            // BakeAnimationTextureArray(animationClips);
        }


        // private static Texture2DArray BakeAnimationTextureArray(List<AnimationClip> clips)
        // {
        // }


        private static Texture2D BakeAnimationTexture(GameObject go, SkinnedMeshRenderer skinnedMeshRenderer, AnimationClip clip, out int textureWidth, out int textureHeight)
        {
            // row = frame, col = vertex
            Mesh bakedMesh = new Mesh();
            int vertexCount = skinnedMeshRenderer.sharedMesh.vertexCount;
            textureHeight = vertexCount;

            float frameInterval = 1.0f / clip.frameRate;
            textureWidth = Mathf.CeilToInt(clip.length * clip.frameRate);
            Texture2D animationTexture = new Texture2D(textureHeight, textureHeight, TextureFormat.RGBAHalf, false);
            animationTexture.filterMode = FilterMode.Point;

            for (int frame = 0; frame < textureWidth; frame++)
            {
                float time = frame * frameInterval;
                clip.SampleAnimation(go, time);

                skinnedMeshRenderer.BakeMesh(bakedMesh);
                Vector3[] vertices = bakedMesh.vertices;

                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    Vector3 vertex = vertices[vertexIndex];
                    animationTexture.SetPixel(frame, vertexIndex, new Color(vertex.x, vertex.y, vertex.z, 1.0f));
                }
            }

            animationTexture.Apply();
            return animationTexture;
        }


        private static void SampleAnimation(GameObject gameObject)
        {
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();


            var combineInstances = new List<CombineInstance>();

            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                var length = skinnedMeshRenderer.sharedMaterials.Length;
                for (int i = 0; i < length; i++)
                {
                    var ci = new CombineInstance();
                    ci.mesh = skinnedMeshRenderer.sharedMesh;
                    ci.subMeshIndex = i;
                    ci.transform = skinnedMeshRenderer.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }
            }
        }


        // [Button]
        // private void BakerSkinMeshRender()
        // {
        //     var skinnedMeshRenderers = TargetObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        //
        //     //材质球数组
        //     List<Material> materials = new List<Material>();
        //     foreach (var i in skinnedMeshRenderers)
        //     {
        //         foreach (var j in i.sharedMaterials)
        //         {
        //             materials.Add(j);
        //         }
        //     }
        //
        //     var combines = new List<CombineInstance>();
        //
        //
        //     foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        //     {
        //         var length = skinnedMeshRenderer.sharedMaterials.Length;
        //         for (int j = 0; j < length; j++)
        //         {
        //             var ci = new CombineInstance();
        //             ci.mesh = skinnedMeshRenderer.sharedMesh;
        //             ci.subMeshIndex = j; // 设置材质球索引
        //             ci.transform = skinnedMeshRenderer.transform.localToWorldMatrix; // 坐标系转换
        //             combines.Add(ci);
        //         }
        //
        //         // meshFilter.gameObject.SetActive(false);
        //     }
        //
        //     //材质合并
        //     var newMaterial = new Material(Shader.Find("Unlit/GpuAnimation"));
        //
        //     List<Texture2D> MainTexs = new List<Texture2D>();
        //     for (int i = 0; i < materials.Count; i++)
        //     {
        //         MainTexs.Add(materials[i].GetTexture("_MainTex") as Texture2D);
        //     }
        //
        //     //所有贴图合并到newDiffuseTex这张大贴图上
        //     var newMainTex = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);
        //
        //     Rect[] uvs = newMainTex.PackTextures(MainTexs.ToArray(), 0);
        //
        //     newMaterial.SetTexture("_MainTex", newMainTex);
        //     AssetDatabase.CreateAsset(newMainTex, $"Assets/GPUMeshAnimation/CombineTexture.asset");
        //
        //
        //     // 把计算出来的uv 赋值给网格上面
        //     // 计算好uvb赋值到到combineInstances[j].mesh.uv
        //     var oldUV = new List<Vector2[]>();
        //     Vector2[] uva, uvb;
        //     for (int j = 0; j < combines.Count; j++)
        //     {
        //         //uva = (Vector2[])(combineInstances[j].mesh.uv);
        //         uva = combines[j].mesh.uv;
        //         uvb = new Vector2[uva.Length];
        //         for (int k = 0; k < uva.Length; k++)
        //         {
        //             uvb[k] = new Vector2((uva[k].x * uvs[j].width) + uvs[j].x, (uva[k].y * uvs[j].height) + uvs[j].y);
        //         }
        //
        //         //oldUV.Add(combineInstances[j].mesh.uv);
        //         oldUV.Add(uva);
        //         combines[j].mesh.uv = uvb;
        //     }
        //
        //     AssetDatabase.CreateAsset(newMaterial, $"Assets/GPUMeshAnimation/Mat.mat");
        //
        //     var selfMeshFilter = GetComponent<MeshFilter>();
        //     selfMeshFilter.sharedMesh = new Mesh();
        //     selfMeshFilter.sharedMesh.CombineMeshes(combines.ToArray(), true, false);
        //
        //     MeshRenderer meshRender = transform.GetComponent<MeshRenderer>();
        //     if (meshRender == null)
        //     {
        //         meshRender = gameObject.AddComponent<MeshRenderer>();
        //     }
        //
        //     // meshRender.sharedMaterials = materials.ToArray();
        //     meshRender.sharedMaterial = newMaterial;
        //
        //     AssetDatabase.CreateAsset(selfMeshFilter.sharedMesh, $"Assets/GPUMeshAnimation/CombineMesh.asset");
        //
        //     //重新赋值，以免影响其他对象的Mesh
        //     for (int i = 0; i < combines.Count; i++)
        //     {
        //         combines[i].mesh.uv = oldUV[i];
        //     }
        //
        //     BakeAnimation(selfMeshFilter, newMaterial);
        // }

        // [Button]
        // private void BakerMeshRender()
        // {
        //     var meshFilters = TargetObject.GetComponentsInChildren<MeshFilter>();
        //     var meshRenders = TargetObject.GetComponentsInChildren<MeshRenderer>();
        //
        //     //材质球数组
        //     List<Material> materials = new List<Material>();
        //     foreach (var i in meshRenders)
        //     {
        //         foreach (var j in i.sharedMaterials)
        //         {
        //             materials.Add(j);
        //         }
        //     }
        //
        //     //网格合并
        //
        //     var combines = new List<CombineInstance>();
        //
        //
        //     foreach (var meshFilter in meshFilters)
        //     {
        //         var length = meshFilter.GetComponent<MeshRenderer>().sharedMaterials.Length;
        //         for (int j = 0; j < length; j++)
        //         {
        //             var ci = new CombineInstance();
        //             ci.mesh = meshFilter.sharedMesh;
        //             ci.subMeshIndex = j; // 设置材质球索引
        //             ci.transform = meshFilter.transform.localToWorldMatrix; // 坐标系转换
        //             combines.Add(ci);
        //         }
        //
        //         meshFilter.gameObject.SetActive(false);
        //     }
        //
        //     var selfMeshFilter = GetComponent<MeshFilter>();
        //     selfMeshFilter.sharedMesh = new Mesh();
        //     selfMeshFilter.sharedMesh.CombineMeshes(combines.ToArray(), false);
        //
        //     MeshRenderer meshRender = transform.GetComponent<MeshRenderer>();
        //     if (meshRender == null)
        //     {
        //         meshRender = gameObject.AddComponent<MeshRenderer>();
        //     }
        //
        //     meshRender.sharedMaterials = materials.ToArray();
        //
        //     AssetDatabase.CreateAsset(selfMeshFilter.sharedMesh, $"Assets/GPUMeshAnimation/CombineMesh.asset");
        // }

        // private void BakeMesh(GameObject source, Mesh targetMesh)
        // {
        //     var skinnedMeshRenderer = source.GetComponentInChildren<SkinnedMeshRenderer>();
        //     skinnedMeshRenderer.BakeMesh(targetMesh);
        // }

//     [Button]
//     private void BakeAnimation(MeshFilter mesh, Material material)
//     {
//         //水平方向 记录每个顶点的位置信息
//         //垂直方向记录每个时间节点所有信息
//
//         var frameRate = 30f;
//         int animLength = (int)(frameRate * SampleAnimationClip.length);
//         material.SetFloat("_AnimLen", SampleAnimationClip.length);
//         //animLength = Mathf.NextPowerOfTwo(animLength);
//         int texwidth = mesh.sharedMesh.vertexCount;
//
//         material.SetVector("_AnimMap_TexelSize", new Vector4(1f / texwidth, 1, 1, 1));
//
//         texwidth = Mathf.NextPowerOfTwo(texwidth);
//
//         Texture2D tex = new Texture2D(texwidth, animLength, TextureFormat.RGBAHalf, false);
//
//         material.SetTexture("_AnimMap", tex);
//         for (int i = 0; i < animLength; i++)
//         {
//             float time = i / frameRate;
//             // Thread.Sleep(10);
//             // Debug.Log($"Time  {time}");
//             SampleAnimationClip.SampleAnimation(TargetObject, time);
//             Mesh bakeMesh = Instantiate<Mesh>(mesh.sharedMesh);
//             BakeMesh(TargetObject, bakeMesh);
//             for (var j = 0; j < bakeMesh.vertices.Length; j++)
//             {
//                 var vertex = bakeMesh.vertices[j];
//                 // Debug.Log(vertex);
//                 tex.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
//             }
//         }
//
//         SampleAnimationClip.SampleAnimation(TargetObject, 0);
//         tex.Apply();
//
//         AssetDatabase.CreateAsset(tex, $"Assets/GPUMeshAnimation/animation.asset");
//     }
// }
    }
}
// }