using System.Collections.Generic;
using System.IO;
using DotsAnimator.GpuAnimation.Runtime;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace DotsAnimator.GpuAnimation.Editor
{
    public class GpuAnimationBakerTools
    {
        public const string ConvertShaderName = "DotsAnimator/GpuAnimationUnlit";

        private class AnimatorClipInfo
        {
            public string Name;
            public AnimationClip Clip;
        }

        [MenuItem("Assets/DotsAnimator/Bake Gpu Animation",false,0)]
        public static void ConvertSelectObject()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                string folderPath = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (go != null)
                    {
                        Convert(go, folderPath);
                        return;
                    }
                }
            }

            Debug.LogError("Please select a prefab.");
        }

        private static void Convert(GameObject go, string saveDirectory)
        {
            var animator = go.GetComponent<Animator>();

            if (animator == null)
            {
                throw new System.Exception("GameObject must have an Animator Component.");
            }

            Debug.Log("DotsAnimator.GpuAnimation only support baking the default layer .");

            var animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (animatorController == null)
            {
                throw new System.Exception("Animator must have an AnimatorController.");
            }

            var layer = animatorController.layers[0];

            var animatorClipInfos = new List<AnimatorClipInfo>();
            foreach (var childAnimatorState in layer.stateMachine.states)
            {
                var animationClip = childAnimatorState.state.motion as AnimationClip;
                if (animationClip == null)
                {
                    Debug.LogError($"{childAnimatorState.state.name} does not have an animation file. Please check the animator configuration.");
                    continue;
                }

                animatorClipInfos.Add(new AnimatorClipInfo()
                {
                    Name = childAnimatorState.state.name,
                    Clip = animationClip
                });
            }

            saveDirectory += $"/{go.name}_GPUAnimation";
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            var animationAssets = new List<AnimationData>();
            foreach (var animatorClipInfo in animatorClipInfos)
            {
                animationAssets.Add(BakeAnimationTexture(go, go.GetComponentInChildren<SkinnedMeshRenderer>(), animatorClipInfo, saveDirectory));
            }

            ExportGpuAnimationPrefab(go, animationAssets, saveDirectory);
        }


        private static Material CreateGpuMaterial(Material originMaterial, string savePath)
        {
            //材质合并
            var newMaterial = new Material(Shader.Find(ConvertShaderName));

            newMaterial.mainTexture = originMaterial.mainTexture;

            AssetDatabase.CreateAsset(newMaterial, $"{savePath}/{originMaterial.name}_GpuAnimation.mat");
            AssetDatabase.SaveAssets();
            return newMaterial;
        }

        private static void ExportGpuAnimationPrefab(GameObject go, List<AnimationData> assets, string savePath)
        {
            var target = new GameObject(go.name);
            target.transform.position = Vector3.zero;
            target.transform.localScale = go.transform.localScale;
            target.transform.rotation = go.transform.rotation;


            var originalAnimator = go.GetComponent<Animator>();
            if (originalAnimator == null)
            {
                Debug.LogWarning("does not found animator controller");
            }

            var meshFilter = target.AddComponent<MeshFilter>();
            var meshRenderer = target.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = go.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

            var newMaterials = new List<Material>();
            foreach (var material in go.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials)
            {
                newMaterials.Add(CreateGpuMaterial(material, savePath));
            }

            meshRenderer.materials = newMaterials.ToArray();

            var gpuAnimation = target.AddComponent<Runtime.GpuAnimation>();
            gpuAnimation.AnimationDatas = assets.ToArray();

            var dotsAnimator = target.AddComponent<Runtime.DotsAnimator>();
            dotsAnimator.AnimatorName = originalAnimator.runtimeAnimatorController.name;
            PrefabUtility.SaveAsPrefabAsset(target, savePath + $"/{go.name}.prefab");
            GameObject.DestroyImmediate(target);


            var targetAnimatorPrefab = new GameObject(go.name + "_DotsAnimator");
            targetAnimatorPrefab.transform.position = Vector3.zero;
            targetAnimatorPrefab.transform.localScale = go.transform.localScale;
            targetAnimatorPrefab.transform.rotation = go.transform.rotation;
            var targetAnimator = targetAnimatorPrefab.AddComponent<Animator>();

            targetAnimator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
            PrefabUtility.SaveAsPrefabAsset(targetAnimatorPrefab, savePath + $"/{targetAnimatorPrefab.name}.prefab");
            GameObject.DestroyImmediate(targetAnimatorPrefab);
        }


        private static AnimationData BakeAnimationTexture(GameObject go, SkinnedMeshRenderer skinnedMeshRenderer, AnimatorClipInfo clipInfo, string savePath)
        {
            // row = frame, col = vertex
            Mesh bakedMesh = new Mesh();
            int vertexCount = skinnedMeshRenderer.sharedMesh.vertexCount;
            var textureHeight = vertexCount;

            var clip = clipInfo.Clip;
            float frameInterval = 1.0f / clip.frameRate;
            var textureWidth = Mathf.CeilToInt(clip.length * clip.frameRate);
            Texture2D animationTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);
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

            var asset = ScriptableObject.CreateInstance<AnimationData>();
            asset.Name = clipInfo.Name;
            asset.NameHash = new FixedString512Bytes(clip.name).GetHashCode();
            asset.ClipTexture = animationTexture;
            asset.FrameCount = textureWidth;
            asset.Loop = clip.isLooping;
            var path = $"{savePath}/{clipInfo.Name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(animationTexture, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AnimationData>(path);
        }
    }
}