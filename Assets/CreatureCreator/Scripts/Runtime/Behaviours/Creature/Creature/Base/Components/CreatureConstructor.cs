﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class CreatureConstructor : MonoBehaviour
    {
        #region Fields
        [Header("Setup")]
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Material bodyPartMaterial;
        [Space]
        [SerializeField] private Transform body;
        [SerializeField] private Transform model;
        [SerializeField] private Transform root;

        [Header("Settings")]
        [SerializeField] private float maxHeight = 2f;
        [SerializeField] private float maxRadius = 1.25f;
        [SerializeField] private int maxComplexity = 50;
        [SerializeField] private float mergeThreshold = 0.04f;
        [SerializeField] private CreatureQuality boneSettings;
        [SerializeField] private MinMax minMaxBones = new MinMax(2, 20);
        [SerializeField] private float density = 500f;
        [SerializeField] private int maxLightSources = 3;
        [SerializeField] private MinMax minMaxBodySpeed = new MinMax(-1f, 0f);
        [SerializeField] private MinMax minMaxBodyHealth = new MinMax(0, 250);
        [SerializeField] private MinMax minMaxBodyWeight = new MinMax(10f, 1000f);

        [Header("Debug")]
        [SerializeField, ReadOnly] private CreatureData data;
        [SerializeField, ReadOnly] private CreatureStatistics statistics;
        [SerializeField, ReadOnly] private CreatureDimensions dimensions;

        private bool isTextured;
        private Rigidbody rb;
        private Mesh bodyMesh, tmpBodyMesh;
        #endregion

        #region Properties
        public Rigidbody Rigidbody => rb;
        public Transform Body => body;
        public Transform Root => root;
        public Transform Model => model;
        public Material BodyPartMaterial => bodyPartMaterial;

        public CreatureQuality BoneSettings => boneSettings;
        public float MergeThreshold => mergeThreshold;
        public float MaxHeight => maxHeight;
        public float MaxRadius => maxRadius;
        public int MaxComplexity => maxComplexity;
        public MinMax MinMaxBones => minMaxBones;
        public CreatureData Data => data;
        public CreatureStatistics Statistics => statistics;
        public CreatureDimensions Dimensions => dimensions;
        public float Density => density;
        public int MaxLightSources
        {
            get => maxLightSources;
            set => maxLightSources = value;
        }

        public SkinnedMeshRenderer SkinnedMeshRenderer { get; private set; }
        public Material BodyMat { get; private set; }
        public Material BodyPrimaryMat { get; private set; }
        public Material BodySecondaryMat { get; private set; }

        public Action OnPreDemolish { get; set; }
        public Action OnConstructBody { get; set; }
        public Action OnFlip { get; set; }
        public Action<int> OnSetupBone { get; set; }
        public Action<int> OnAddBone { get; set; }
        public Action<int> OnRemoveBone { get; set; }
        public Action<int> OnPreRemoveBone { get; set; }
        public Action<int, float> OnSetWeight { get; set; }
        public Action<int, float> OnAddWeight { get; set; }
        public Action<int, float> OnRemoveWeight { get; set; }
        public Action<GameObject, GameObject> OnAddBodyPartPrefab { get; set; }
        public Action<BodyPart> OnAddBodyPartData { get; set; }
        public Action<BodyPart> OnRemoveBodyPartData { get; set; }
        public Action<Color> OnSetPrimaryColour { get; set; }
        public Action<Color> OnSetSecondaryColour { get; set; }
        public Action<string> OnSetPattern { get; set; }
        public Action<Vector2> OnSetTiling { get; set; }
        public Action<Vector2> OnSetOffset { get; set; }
        public Action<float> OnSetShine { get; set; }
        public Action<float> OnSetMetallic { get; set; }
        public Func<BodyPart, GameObject> OnBodyPartPrefabOverride { get; set; }

        public List<Transform> Bones { get; set; } = new List<Transform>();
        public List<BodyPartConstructor> BodyParts { get; set; } = new List<BodyPartConstructor>();
        public List<LimbConstructor> Limbs { get; set; } = new List<LimbConstructor>();
        public List<LegConstructor> Legs { get; set; } = new List<LegConstructor>();

        public bool IsTextured
        {
            get => isTextured;
            set
            {
                isTextured = value;
                bodyMesh.uv = isTextured ? bodyMesh.uv8 : null;
            }
        }
        public int LightSources
        {
            get; set;
        }
        #endregion

        #region Methods
        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            rb = GetComponent<Rigidbody>();

            SkinnedMeshRenderer = Model.GetComponent<SkinnedMeshRenderer>();
            SkinnedMeshRenderer.sharedMaterial = BodyMat = new Material(bodyMaterial)
            {
                name = "Body"
            };
            SkinnedMeshRenderer.sharedMesh = bodyMesh = new Mesh()
            {
                name = "Body"
            };
            tmpBodyMesh = new Mesh();

            BodyPrimaryMat = new Material(bodyPartMaterial)
            {
                name = "Body_Primary"
            };
            BodySecondaryMat = new Material(bodyPartMaterial)
            {
                name = "Body_Secondary"
            };
        }

        public void Construct(CreatureData data, bool debug = false)
        {
            if (debug) Timer.Start("Creature");

            SetName(data.Name);
            for (int i = 0; i < data.Bones.Count; i++)
            {
                Bone bone = data.Bones[i];
                AddBone(i, bone.position, bone.rotation, bone.weight, i == data.Bones.Count - 1);
            }
            for (int i = 0; i < data.AttachedBodyParts.Count; i++)
            {
                AttachedBodyPart attachedBodyPart = data.AttachedBodyParts[i];
                BodyPartConstructor main = AddBodyPart(attachedBodyPart.bodyPartID);
                main.Attach(attachedBodyPart);
            }
            SetPrimaryColour(data.PrimaryColour);
            SetSecondaryColour(data.SecondaryColour);
            SetPattern(data.PatternID);
            SetTiling(data.Tiling);
            SetOffset(data.Offset);
            SetShine(data.Shine);
            SetMetallic(data.Metallic);

            if (debug) Timer.Stop("Creature");
        }
        public void Demolish()
        {
            OnPreDemolish?.Invoke();

            while (BodyParts.Count > 0)
            {
                RemoveBodyPart(BodyParts[0]);
            }
            Root.DestroyChildren();
            Root.localPosition = Vector3.zero;
            Bones.Clear();

            data.Reset();
            statistics.Reset();
            dimensions.Reset();
        }

        private void ConstructBody()
        {
            // Mesh Generation
            bodyMesh.Clear();
            bodyMesh.ClearBlendShapes();

            #region Vertices
            List<Vector3> vertices = new List<Vector3>();
            List<BoneWeight> boneWeights = new List<BoneWeight>();

            // Top Hemisphere
            for (int ringIndex = 0; ringIndex < boneSettings.Segments / 2; ringIndex++)
            {
                float percent = (float)ringIndex / (boneSettings.Segments / 2);
                float ringRadius = boneSettings.Radius * Mathf.Sin(90f * percent * Mathf.Deg2Rad);
                float ringDistance = boneSettings.Radius * (-Mathf.Cos(90f * percent * Mathf.Deg2Rad) + 1f);

                for (int i = 0; i < boneSettings.Segments + 1; i++)
                {
                    float angle = i * 360f / boneSettings.Segments;

                    float x = ringRadius * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float y = ringRadius * Mathf.Sin(angle * Mathf.Deg2Rad);
                    float z = ringDistance;

                    vertices.Add(new Vector3(x, y, z));
                    boneWeights.Add(new BoneWeight() { boneIndex0 = 0, weight0 = 1f });
                }
            }

            // Middle Cylinder
            for (int ringIndex = 0; ringIndex < boneSettings.Rings * data.Bones.Count; ringIndex++)
            {
                float boneIndexFloat = (float)ringIndex / boneSettings.Rings;
                int boneIndex = Mathf.FloorToInt(boneIndexFloat);

                float bonePercent = boneIndexFloat - boneIndex;

                int boneIndex0 = (boneIndex > 0) ? boneIndex - 1 : 0;
                int boneIndex2 = (boneIndex < data.Bones.Count - 1) ? boneIndex + 1 : data.Bones.Count - 1;
                int boneIndex1 = boneIndex;

                float weight0 = (boneIndex > 0) ? (1f - bonePercent) * 0.5f : 0f;
                float weight2 = (boneIndex < data.Bones.Count - 1) ? bonePercent * 0.5f : 0f;
                float weight1 = 1f - (weight0 + weight2);

                for (int i = 0; i < boneSettings.Segments + 1; i++)
                {
                    float angle = i * 360f / boneSettings.Segments;

                    float x = boneSettings.Radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float y = boneSettings.Radius * Mathf.Sin(angle * Mathf.Deg2Rad);
                    float z = ringIndex * boneSettings.Length / boneSettings.Rings;

                    vertices.Add(new Vector3(x, y, boneSettings.Radius + z));
                    boneWeights.Add(new BoneWeight()
                    {
                        boneIndex0 = boneIndex0,
                        boneIndex1 = boneIndex1,
                        boneIndex2 = boneIndex2,
                        weight0 = weight0,
                        weight1 = weight1,
                        weight2 = weight2
                    });
                }
            }

            // Bottom Hemisphere
            for (int ringIndex = 0; ringIndex < boneSettings.Segments / 2 + 1; ringIndex++)
            {
                float percent = (float)ringIndex / (boneSettings.Segments / 2);
                float ringRadius = boneSettings.Radius * Mathf.Cos(90f * percent * Mathf.Deg2Rad);
                float ringDistance = boneSettings.Radius * Mathf.Sin(90f * percent * Mathf.Deg2Rad);

                for (int i = 0; i < boneSettings.Segments + 1; i++)
                {
                    float angle = i * 360f / boneSettings.Segments;

                    float x = ringRadius * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float y = ringRadius * Mathf.Sin(angle * Mathf.Deg2Rad);
                    float z = ringDistance;

                    vertices.Add(new Vector3(x, y, boneSettings.Radius + (boneSettings.Length * data.Bones.Count) + z));
                    boneWeights.Add(new BoneWeight() { boneIndex0 = data.Bones.Count - 1, weight0 = 1 });
                }
            }

            bodyMesh.SetVertices(vertices);
            bodyMesh.boneWeights = boneWeights.ToArray();
            #endregion

            #region Triangles
            List<int> triangles = new List<int>();

            // Top Cap
            for (int i = 0; i < boneSettings.Segments; i++)
            {
                triangles.Add(i);
                triangles.Add(i + (boneSettings.Segments + 1) + 1);
                triangles.Add(i + (boneSettings.Segments + 1));
            }

            // Main
            int midRings = (2 * (boneSettings.Segments / 2) - 2) + (boneSettings.Rings * data.Bones.Count + 1);
            for (int ringIndex = 1; ringIndex < midRings; ringIndex++)
            {
                int ringOffset = ringIndex * (boneSettings.Segments + 1);
                for (int i = 0; i < boneSettings.Segments; i++)
                {
                    triangles.Add(ringOffset + i);
                    triangles.Add(ringOffset + i + 1);
                    triangles.Add(ringOffset + i + 1 + (boneSettings.Segments + 1));

                    triangles.Add(ringOffset + i + 1 + (boneSettings.Segments + 1));
                    triangles.Add(ringOffset + i + (boneSettings.Segments + 1));
                    triangles.Add(ringOffset + i);
                }
            }

            // Bottom Cap
            int topOffset = (midRings) * (boneSettings.Segments + 1);
            for (int i = 0; i < boneSettings.Segments; i++)
            {
                triangles.Add(topOffset + i);
                triangles.Add(topOffset + i + 1);
                triangles.Add(topOffset + i + (boneSettings.Segments + 1));
            }

            bodyMesh.SetTriangles(triangles, 0);
            #endregion

            #region UVs
            List<Vector2> uv = new List<Vector2>();

            float vScale = 1f;
            float uScale = 1f;

            float avgWeight = 0f;
            foreach (Bone bone in Data.Bones)
            {
                avgWeight += bone.weight;
            }
            avgWeight /= Data.Bones.Count;
            float radius = Mathf.Lerp(boneSettings.Radius, boneSettings.Radius * 4f, (avgWeight / 100f));
            float length = (2 * boneSettings.Radius) + (data.Bones.Count * boneSettings.Length);
            float circumference = 2f * Mathf.PI * radius;

            if (length > circumference)
            {
                vScale = Mathf.FloorToInt(length / circumference);
            }
            else
            {
                uScale = Mathf.FloorToInt(circumference / length);
            }

            int totalRings = midRings + 2;
            for (int ringIndex = 0; ringIndex < totalRings; ringIndex++)
            {
                float v = (ringIndex / (float)totalRings);
                for (int i = 0; i < boneSettings.Segments + 1; i++)
                {
                    float u = i / (float)(boneSettings.Segments);
                    uv.Add(new Vector2(u * uScale, v * vScale));
                }
            }

            bodyMesh.SetUVs(7, uv); // Store copy of UVs in mesh.
            bodyMesh.uv = bodyMesh.uv8;
            #endregion

            #region Normals
            Vector3[] normals = new Vector3[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 xyDir = Vector3.ProjectOnPlane(vertices[i], Vector3.forward);
                Vector3 zDir = ((i > vertices.Count / 2) ? 1 : -1) * (boneSettings.Radius - xyDir.magnitude) * Vector3.forward;

                normals[i] = (xyDir + zDir).normalized;
            }

            bodyMesh.SetNormals(normals);
            #endregion

            #region Bones
            Transform[] boneTransforms = new Transform[data.Bones.Count];
            Matrix4x4[] bindPoses = new Matrix4x4[data.Bones.Count];
            Vector3[] deltaZeroArray = new Vector3[vertices.Count];
            for (int vertIndex = 0; vertIndex < vertices.Count; vertIndex++)
            {
                deltaZeroArray[vertIndex] = Vector3.zero;
            }

            for (int boneIndex = 0; boneIndex < data.Bones.Count; boneIndex++)
            {
                boneTransforms[boneIndex] = Bones[boneIndex];

                // Bind Pose
                boneTransforms[boneIndex].localPosition = Vector3.forward * (boneSettings.Radius + boneSettings.Length * (0.5f + boneIndex));
                boneTransforms[boneIndex].localRotation = Quaternion.identity;
                bindPoses[boneIndex] = boneTransforms[boneIndex].worldToLocalMatrix * Body.localToWorldMatrix;

                // Blend Shapes
                Vector3[] deltaVertices = new Vector3[vertices.Count];
                for (int vertIndex = 0; vertIndex < vertices.Count; vertIndex++)
                {
                    // Round
                    //float distanceToBone = Mathf.Clamp(Vector3.Distance(vertices[vertIndex], boneTransforms[boneIndex].localPosition), 0, 2f * boneSettings.Length);
                    //Vector3 directionToBone = (vertices[vertIndex] - boneTransforms[boneIndex].localPosition).normalized;

                    //deltaVertices[vertIndex] = directionToBone * (2f * boneSettings.Length - distanceToBone);


                    // Smooth - https://www.desmos.com/calculator/wmpvvtmor8
                    float maxDistanceAlongBone = boneSettings.Length * 2f;
                    float maxHeightAboveBone = boneSettings.Radius * 2f;

                    float displacementAlongBone = vertices[vertIndex].z - boneTransforms[boneIndex].localPosition.z;

                    float x = Mathf.Clamp(displacementAlongBone / maxDistanceAlongBone, -1, 1);
                    float a = maxHeightAboveBone;
                    float b = 1f / a;

                    float heightAboveBone = (Mathf.Cos(x * Mathf.PI) / b + a) / 2f;

                    deltaVertices[vertIndex] = new Vector2(vertices[vertIndex].x, vertices[vertIndex].y).normalized * heightAboveBone;
                }
                bodyMesh.AddBlendShapeFrame("Bone." + boneIndex, 0, deltaZeroArray, deltaZeroArray, deltaZeroArray);
                bodyMesh.AddBlendShapeFrame("Bone." + boneIndex, 100, deltaVertices, deltaZeroArray, deltaZeroArray);

                OnSetupBone?.Invoke(boneIndex);
            }

            bodyMesh.bindposes = bindPoses;
            SkinnedMeshRenderer.bones = boneTransforms;
            #endregion

            // Mesh Deformation
            for (int boneIndex = 0; boneIndex < data.Bones.Count; boneIndex++)
            {
                boneTransforms[boneIndex].position = transform.TransformPoint(data.Bones[boneIndex].position);
                boneTransforms[boneIndex].rotation = transform.rotation * data.Bones[boneIndex].rotation;
                SetWeight(boneIndex, data.Bones[boneIndex].weight);
            }
            UpdateOrigin();

            // Mesh Bounds
            if (!SkinnedMeshRenderer.updateWhenOffscreen)
            {
                SkinnedMeshRenderer.BakeMesh(tmpBodyMesh);
                UpdateBounds(tmpBodyMesh);
            }

            OnConstructBody?.Invoke();
        }

        public void AddBone(int index, Vector3 position, Quaternion rotation, float weight, bool apply = true)
        {
            if (apply)
            {
                DetachBodyParts();
            }

            Transform bone = new GameObject("Bone." + data.Bones.Count).transform;
            bone.SetParent(Root, false);
            Bones.Add(bone);
            Statistics.Complexity++;
            OnAddBone?.Invoke(index);
            data.Bones.Insert(index, new Bone()
            {
                position = position,
                rotation = rotation,
                weight = weight
            });

            if (apply)
            {
                ConstructBody();

                ReattachBodyParts();
            }
        }
        public void AddBoneToFront()
        {
            Vector3 position = Bones[0].position - Bones[0].forward * boneSettings.Length;
            Quaternion rotation = Bones[0].rotation;

            position = transform.InverseTransformPoint(position);
            rotation = Quaternion.Inverse(transform.rotation) * rotation;

            AddBone(0, position, rotation, Mathf.Clamp(GetWeight(0) * 0.75f, 0f, 100f));
        }
        public void AddBoneToBack()
        {
            Vector3 position = Bones[Bones.Count - 1].position + Bones[Bones.Count - 1].forward * boneSettings.Length;
            Quaternion rotation = Bones[Bones.Count - 1].rotation;

            position = transform.InverseTransformPoint(position);
            rotation = Quaternion.Inverse(transform.rotation) * rotation;

            AddBone(Bones.Count, position, rotation, Mathf.Clamp(GetWeight(Bones.Count - 1) * 0.75f, 0f, 100f));
        }
        public void RemoveBone(int index, bool apply = true)
        {
            if (apply)
            {
                DetachBodyParts();
            }

            OnPreRemoveBone?.Invoke(index);

            Transform bone = Bones[Bones.Count - 1];
            Bones.Remove(bone);
            DestroyImmediate(bone.gameObject);
            Statistics.Complexity--;
            OnRemoveBone?.Invoke(index);
            data.Bones.RemoveAt(index);

            if (apply)
            {
                ConstructBody();

                ReattachBodyParts();
            }
        }
        public void RemoveBoneFromFront()
        {
            RemoveBone(0);
        }
        public void RemoveBoneFromBack()
        {
            RemoveBone(Bones.Count - 1);
        }
        private void DetachBodyParts()
        {
            foreach (BodyPartConstructor bodyPart in BodyParts)
            {
                bodyPart.transform.parent = bodyPart.Flipped.transform.parent = Dynamic.Transform;
            }
        }
        private void ReattachBodyParts()
        {
            foreach (BodyPartConstructor bodyPart in BodyParts)
            {
                int index = bodyPart.NearestBone;
                bodyPart.transform.parent = bodyPart.Flipped.transform.parent = Bones[index];
                bodyPart.AttachedBodyPart.boneIndex = index;
            }
        }

        public float GetWeight(int index)
        {
            return SkinnedMeshRenderer.GetBlendShapeWeight(index);
        }
        public void SetWeight(int index, float weight)
        {
            weight = Mathf.Clamp(weight, 0f, 100f);

            data.Bones[index].weight = weight;
            SkinnedMeshRenderer.SetBlendShapeWeight(index, weight);

            OnSetWeight?.Invoke(index, weight);
        }
        public void AddWeight(int index, float weight, int dir = 0)
        {
            SetWeight(index, GetWeight(index) + weight);

            if (index > 0 && (dir == -1 || dir == 0)) { AddWeight(index - 1, weight / 2f, -1); }
            if (index < data.Bones.Count - 1 && (dir == 1 || dir == 0)) { AddWeight(index + 1, weight / 2f, 1); }

            OnAddWeight?.Invoke(index, weight);
        }
        public void RemoveWeight(int index, float weight)
        {
            SetWeight(index, GetWeight(index) - weight);

            if (index > 0) { SetWeight(index - 1, GetWeight(index - 1) - weight / 2f); }
            if (index < data.Bones.Count - 1) { SetWeight(index + 1, GetWeight(index + 1) - weight / 2f); }

            OnRemoveWeight?.Invoke(index, weight);
        }

        public BodyPartConstructor AddBodyPart(string bodyPartID)
        {
            BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartID);
            bodyPart.Add(statistics);

            if (bodyPart.IsLightSource)
            {
                LightSources++;
            }

            GameObject bodyPartPrefab = OnBodyPartPrefabOverride?.Invoke(bodyPart) ?? bodyPart.GetPrefab(BodyPart.PrefabType.Constructible);

            // Main
            BodyPartConstructor main = Instantiate(bodyPartPrefab).GetComponent<BodyPartConstructor>();
            main.Setup(this);
            main.Add();

            // Flipped
            BodyPartConstructor flipped = Instantiate(bodyPartPrefab).GetComponent<BodyPartConstructor>();
            flipped.SetFlipped(main);
            flipped.Setup(this);

            OnAddBodyPartPrefab?.Invoke(main.gameObject, main.Flipped.gameObject);
            OnAddBodyPartData?.Invoke(bodyPart);

            UpdateWeight();

            return main;
        }
        public void RemoveBodyPart(BodyPartConstructor main)
        {
            BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", main.AttachedBodyPart.bodyPartID);
            bodyPart.Remove(statistics);

            if (bodyPart.IsLightSource)
            {
                LightSources--;
            }

            main.Remove();
            Data.AttachedBodyParts.Remove(main.AttachedBodyPart);
            UpdateWeight();

            OnRemoveBodyPartData?.Invoke(bodyPart);

            if (main != null)
            {
                DestroyImmediate(main.Flipped.gameObject);
                DestroyImmediate(main.gameObject);
            }
        }

        public void SetName(string creatureName)
        {
            data.Name = creatureName;
        }
        public void SetPrimaryColour(Color colour)
        {
            data.PrimaryColour = colour;
            BodyMat.SetColor("_PrimaryCol", colour);
            BodyPrimaryMat.color = colour;

            OnSetPrimaryColour?.Invoke(colour);
        }
        public void SetSecondaryColour(Color colour)
        {
            data.SecondaryColour = colour;
            BodyMat.SetColor("_SecondaryCol", colour);
            BodySecondaryMat.color = colour;

            OnSetSecondaryColour?.Invoke(colour);
        }
        public void SetPattern(string patternID)
        {
            data.PatternID = patternID;

            BodyMat.SetTexture("_PatternTex", DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", patternID)?.Texture);

            OnSetPattern?.Invoke(patternID);
        }
        public void SetTiling(Vector2 tiling)
        {
            data.Tiling = tiling;

            BodyMat.SetTextureScale("_PatternTex", tiling);

            OnSetTiling?.Invoke(tiling);
        }
        public void SetOffset(Vector2 offset)
        {
            data.Offset = offset;

            BodyMat.SetTextureOffset("_PatternTex", offset);

            OnSetOffset?.Invoke(offset);
        }
        public void SetShine(float shine)
        {
            data.Shine = shine;

            BodyMat.SetFloat("_Glossiness", shine);

            foreach (BodyPartConstructor bpc in BodyParts)
            {
                bpc.SetShine(shine);
            }

            OnSetShine?.Invoke(shine);
        }
        public void SetMetallic(float metallic)
        {
            data.Metallic = metallic;

            BodyMat.SetFloat("_Metallic", metallic);

            foreach (BodyPartConstructor bpc in BodyParts)
            {
                bpc.SetMetallic(metallic);
            }

            OnSetMetallic?.Invoke(metallic);
        }

        public void Flip()
        {
            Transform tmp = new GameObject("_TMP").transform;
            tmp.SetZeroParent(Body);

            foreach (Transform bone in Bones)
            {
                bone.parent = tmp;
            }
            tmp.localRotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (Transform bone in Bones)
            {
                bone.parent = Root;
            }

            Destroy(tmp.gameObject);

            OnFlip?.Invoke();
        }
        public void Recenter()
        {
            Vector3 displacement = Vector3.ProjectOnPlane(Body.position - transform.position, transform.up);

            transform.position += displacement;
            Body.localPosition = new Vector3(0, Body.localPosition.y, 0);
        }
        public void Realign()
        {
            if (Legs.Count == 0)
            {
                Body.localPosition = new Vector3(0f, (SkinnedMeshRenderer.localBounds.size.y / 2f) - SkinnedMeshRenderer.localBounds.center.y, 0f);
            }
        }

        public void UpdateOrigin()
        {
            Vector3 sum = Vector3.zero;
            float totalWeights = 0;
            for (int i = 0; i < Bones.Count; i++)
            {
                float weight = data.Bones[i].weight + 1; // can't be zero

                sum += Bones[i].position * weight;
                totalWeights += weight;
            }

            Vector3 mean = sum / totalWeights;
            Vector3 displacement = mean - Body.position;
            foreach (Transform bone in Bones)
            {
                bone.position -= displacement;
            }
            Body.position = mean;
        }
        public void UpdateBounds(Mesh mesh)
        {
            mesh.RecalculateBounds();
            SkinnedMeshRenderer.localBounds = mesh.bounds;

            UpdateDimensions();
        }
        public void UpdateDimensions()
        {
            Dimensions.Body.Width  = SkinnedMeshRenderer.localBounds.size.x;
            Dimensions.Body.Height = SkinnedMeshRenderer.localBounds.size.y;
            Dimensions.Body.Length = SkinnedMeshRenderer.localBounds.size.z;

            if (Legs.Count > 0)
            {
                Dimensions.Height = Body.localPosition.y - SkinnedMeshRenderer.localBounds.center.y + SkinnedMeshRenderer.localBounds.size.y / 2f;
            }
            else
            {
                Dimensions.Height = Dimensions.Body.Height;
            }

            UpdateWeight();
        }
        public void UpdateWeight()
        {
            float avgWeight = 0f;
            foreach (Bone bone in Data.Bones)
            {
                avgWeight += bone.weight;
            }
            avgWeight /= Data.Bones.Count;
            float radius = Mathf.Lerp(boneSettings.Radius, boneSettings.Radius * 4f, (avgWeight / 100f));
            float length = (2 * boneSettings.Radius) + (data.Bones.Count * boneSettings.Length);

            float volume = Mathf.PI * Mathf.Pow(radius, 2) * length;
            float weight = volume * density;
            statistics.WeightBody = weight;

            float w = Mathf.InverseLerp(minMaxBodyWeight.min, minMaxBodyWeight.max, weight);
            statistics.SpeedBody = Mathf.Lerp(minMaxBodySpeed.min, minMaxBodySpeed.max, 1 - w);
            statistics.HealthBody = (int)Mathf.Lerp(minMaxBodyHealth.min, minMaxBodyHealth.max, w);

            rb.mass = statistics.Weight;
        }

        public void UpdateConfiguration()
        {
            UpdateBoneConfiguration();
            UpdateAttachedBodyPartConfiguration();
        }
        public void UpdateBoneConfiguration()
        {
            for (int boneIndex = 0; boneIndex < Bones.Count; boneIndex++)
            {
                data.Bones[boneIndex].position = transform.InverseTransformPoint(Bones[boneIndex].position);
                data.Bones[boneIndex].rotation = Quaternion.Inverse(transform.rotation) * Bones[boneIndex].rotation;
                data.Bones[boneIndex].weight = GetWeight(boneIndex);
            }
        }
        public void UpdateAttachedBodyPartConfiguration()
        {
            foreach (BodyPartConstructor bodyPart in BodyParts)
            {
                bodyPart.UpdateAttachmentConfiguration();
            }
        }
        #endregion
    }
}