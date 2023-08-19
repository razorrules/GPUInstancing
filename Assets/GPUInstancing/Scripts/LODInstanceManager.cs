using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

//TODO: Create a pooling system

namespace GPUInstancing
{

    /// <summary>
    /// This class can handle spawning a mesh with various levels of LOD.
    /// Handles Culling and supports real time lighting all using instancing
    /// for incredible performance. Objects are not real would so will not
    /// be able to attach components to them, but can be modified based on 
    /// a matrix.
    /// 
    /// If you override OnDestroy, ensure that you deallocate everything.
    /// </summary>
    public class LODInstanceManager : MonoBehaviour
    {
        //TODO: Handle culling if it is incredibly close
        //TODO: Investigate why CPU time is high, probably some easy optimization that can be done.
        public const float CAMERA_CULL_OFFSET_PIXELS = 100;

        [Header("Settings")]
        [SerializeField] protected int numInstances = 100;
        [SerializeField] protected bool constructInAwake = false;
        [SerializeField] private InstanceMeshSet _meshSet;
        [SerializeField] protected Camera _camera;

        //List of all positions on the grid
        [NativeDisableParallelForRestriction]
        protected NativeArray<float3> _positions;

        /// <summary> Byte for the LOD group a given position belongs too. </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<byte> _lodGroup;

        /// <summary> Should a given position render? </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<bool> _doRender;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        /// <summary> Length of the array for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<int> _matrixLength;

        /// <summary> Render distance for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<float> _renderDistance;

        private RenderParams[] RenderParams;

        private RenderArea _renderArea;
#if UNITY_EDITOR
        private Stopwatch _prerenderTimer;
#endif

        //========== Properties
        public int AvailableInstances { get; private set; } = 0;
        public long CpuTimeMilliseconds { get; private set; } = 0;
        public float AllocatedKB { get; protected set; } = 0;
        public bool IsSetup { get; private set; } = false;
        public int MeshesCount { get => Meshes.Length; }
        public InstanceMesh[] Meshes { get; protected set; }
        public float[] RenderDistance;

        private void Awake()
        {
            if (constructInAwake)
                Allocate(numInstances);
        }

        private void OnDestroy()
        {
            //Dispose all of the arrays whenever object is destroy4ed
            Deallocate();
        }

        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected virtual void Deallocate()
        {
            _positions.Dispose();
            _doRender.Dispose();
            _lodGroup.Dispose();
            _matrixData.Dispose();
            _matrixLength.Dispose();
            _renderDistance.Dispose();
        }

        /// <summary>
        /// Update render distance constantly, that way you are not forced to restart
        /// </summary>
        private void OnValidate()
        {
            if (_renderDistance == null || Meshes == null)
                return;
            for (int i = 0; i < MeshesCount; i++)
                _renderDistance[i] = RenderDistance[i];
        }

        private void Update()
        {
            if (!IsSetup)
                return;

            //Calculate what the camera sees
            CalculateCameraBounds();

            //Do pre rendering calculations like culling.
            PreRender();

            //Actually render the meshes.
            Render();
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        protected virtual void Setup()
        {
            if (_camera == null)
            {
                _camera = FindObjectOfType<Camera>();
                if (_camera == null)
                    Debug.LogError("No camera set for InstanceSpawningManager and no camera found in scene to default to. Ensure a camera is setup.");
                else
                    Debug.LogWarning("<color=orange>No camera set for InstancingSpawningManager. Set do default camera: " + _camera.name + "</color>");
            }
            Meshes = _meshSet.Meshes;
            _renderArea = new RenderArea();
#if UNITY_EDITOR
            _prerenderTimer = new Stopwatch();
#endif

        }

        public void Allocate(int instancesCount, Camera camera)
        {
            SetCamera(camera);
            Allocate(instancesCount);
        }

        public virtual void Allocate(int instancesCount)
        {
            Setup();
            if (Meshes == null || Meshes.Length == 0)
            {
                Debug.Log("InstanceSpawningManager cannot allocate and setup without meshes.");
                return;
            }

            RenderParams = new RenderParams[MeshesCount];

            for (int i = 0; i < RenderParams.Length; i++)
            {
                RenderParams[i] = new RenderParams(Meshes[i].material);
                RenderParams[i].layer = Meshes[i].layer;
                RenderParams[i].shadowCastingMode = Meshes[i].shadowCastingMode;
                RenderParams[i].receiveShadows = Meshes[i].receiveShadows;
                RenderParams[i].camera = _camera;
            }

            //Setup the render distance into an array so we can pass it to jobs
            _renderDistance = new NativeArray<float>(MeshesCount, Allocator.Persistent);
            for (int i = 0; i < MeshesCount; i++)
                _renderDistance[i] = RenderDistance[i];

            AvailableInstances = instancesCount;

            //Lets allocate all of the arrays, we will also track how much we allocated
            //Float 3 does not have a predefined size, but it contains 3 floats
            //Matrix4x4 does not have a predefined size, but it contains 16 floats
            AllocatedKB = 0;
            int floatSize = sizeof(float);
            int matrixSize = floatSize * 16;
            int float3Size = floatSize * 3;
            int boolSize = sizeof(bool);

            //Ensure all of the native arrays are setup
            _positions = new NativeArray<float3>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += float3Size * AvailableInstances;

            //Allocate LOD groups
            _lodGroup = new NativeArray<byte>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (sizeof(byte) * AvailableInstances);

            //Allocate doRender
            _doRender = new NativeArray<bool>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += boolSize * AvailableInstances;

            //Allocate matrix data
            _matrixData = new NativeArray<Matrix4x4>(AvailableInstances * MeshesCount, Allocator.Persistent);
            AllocatedKB += (matrixSize * AvailableInstances) * MeshesCount;

            //Allocate matrix length
            _matrixLength = new NativeArray<int>(MeshesCount, Allocator.Persistent);
            AllocatedKB += sizeof(int) * MeshesCount;

            AllocatedKB /= 1024;
            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");

            IsSetup = true;
        }

        public void MovePoint(int index, Vector3 position)
        {
            //TODO: Implement
        }

        public void RotatePoint(int index, Quaternion position)
        {
            //TODO: Implement
        }

        public void AddPoint(int index, Vector3 position)
        {
            //TODO: Implement
        }

        public void AddPoint(int index, Vector3 position, Quaternion rotation)
        {
            //TODO: Implement
        }

        public void AddPoint(int index, Vector3 position, Vector3 scale)
        {
            //TODO: Implement
        }

        public void AddPoint(int index, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            //TODO: Implement
        }

        public void ScalePoint(int index, Vector3 scale)
        {
            //TODO: Implement
        }

        protected virtual void CalculateCameraBounds()
        {
            //TODO: Calculate bounds of the camera, extend a little bit extra, and then only render if they are within the bounds
            Ray botLeft = _camera.ScreenPointToRay(new Vector3(-CAMERA_CULL_OFFSET_PIXELS, -CAMERA_CULL_OFFSET_PIXELS, 0));
            Ray topLeft = _camera.ScreenPointToRay(new Vector3(-CAMERA_CULL_OFFSET_PIXELS, Screen.height + CAMERA_CULL_OFFSET_PIXELS, 0));

            Ray botRight = _camera.ScreenPointToRay(new Vector3(Screen.width + CAMERA_CULL_OFFSET_PIXELS, -CAMERA_CULL_OFFSET_PIXELS, 0));
            Ray topRight = _camera.ScreenPointToRay(new Vector3(Screen.width + CAMERA_CULL_OFFSET_PIXELS, Screen.height + CAMERA_CULL_OFFSET_PIXELS, 0));

            Vector2 tl = IntersectionY(topLeft, 0);
            Vector2 tr = IntersectionY(topRight, 0);
            Vector2 bl = IntersectionY(botLeft, 0);
            Vector2 br = IntersectionY(botRight, 0);

            _renderArea.UpdatePoints(tl, tr, bl, br);
        }

        private Vector2 IntersectionY(Ray ray, float targetY)
        {
            //TODO: Looking upside renders everythings
            Vector3 value = ray.origin;
            if (ray.direction.y > 0)
                ray.direction = new Vector3(ray.direction.x, -.01f, ray.direction.z);
            float multiplication = -((ray.origin.y - targetY) / ray.direction.y);
            value += ray.direction * multiplication;
            return new Vector2(value.x, value.z);
        }

        //TODO: Are these needed?
        private Matrix4x4 tmp;
        private Vector4 pos;

        protected virtual void PreRender(bool stopTimer = true)
        {
#if UNITY_EDITOR
            _prerenderTimer.Restart();
#endif
            //First, lets check if we should render the objects. This is culling
            ShouldRenderJob renderCheck = new ShouldRenderJob()
            {
                doRender = _doRender,
                positions = _positions,
                cameraBounds = _renderArea
            };

            JobHandle renderCheckJob = renderCheck.Schedule(_positions.Length, 1);
            renderCheckJob.Complete();

            //Calculate the LOD groups and what different points should use
            CalculateLODGroups lodCheck = new CalculateLODGroups()
            {
                origin = _camera.transform.position,
                renderDistance = _renderDistance,
                doRender = _doRender,
                positions = _positions,
                lodGroup = _lodGroup,
            };

            JobHandle lodCheckHandle = lodCheck.Schedule(_positions.Length, 1);
            lodCheckHandle.Complete();

            UpdateMatrixJob updateMatrix = new UpdateMatrixJob()
            {
                lodGroups = _lodGroup,
                doRender = _doRender,
                matrixLengths = _matrixLength,
                matrixData = _matrixData,
                positions = _positions,
                tmp = tmp,
                pos = pos,
            };

            JobHandle updateMatrixHandle = updateMatrix.Schedule();
            updateMatrixHandle.Complete();

            if (stopTimer)
                FinishPreRender();
        }

        protected void FinishPreRender()
        {
#if UNITY_EDITOR
            _prerenderTimer.Stop();
            CpuTimeMilliseconds = _prerenderTimer.ElapsedMilliseconds;

            if (_prerenderTimer.ElapsedMilliseconds > 1)
                Debug.Log("Took: " + _prerenderTimer.ElapsedMilliseconds + "ms in prerender.");
#endif
        }

        protected virtual void Render()
        {
            for (int i = 0; i < MeshesCount; i++)
            {
                if (_matrixLength[i] == 0)
                    continue;

                Graphics.RenderMeshInstanced(RenderParams[i],
                    Meshes[i].mesh,
                    Meshes[i].submeshIndex,
                    _matrixData.GetSubArray(i * AvailableInstances, _matrixLength[i])
                    );
            }
        }


        [BurstCompile]
        protected struct ShouldRenderJob : IJobParallelFor
        {
            public NativeArray<float3> positions;
            public NativeArray<bool> doRender;
            public RenderArea cameraBounds;

            [BurstCompile]
            public void Execute(int index)
            {
                doRender[index] = true;
                //doRender[index] = cameraBounds.PointInBounds(positions[index]);
            }
        }

        [BurstCompile]
        protected struct UpdateMatrixJob : IJob
        {
            public NativeArray<byte> lodGroups;
            public NativeArray<bool> doRender;

            public NativeArray<int> matrixLengths;
            public NativeArray<Matrix4x4> matrixData;
            public NativeArray<float3> positions;

            public Matrix4x4 tmp;
            public Vector4 pos;

            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < matrixLengths.Length; i++)
                    matrixLengths[i] = 0;

                for (int i = 0; i < positions.Length; i++)
                {
                    if (doRender[i] == false)
                        continue;

                    tmp = matrixData[positions.Length * lodGroups[i] + matrixLengths[lodGroups[i]]];
                    tmp.SetColumn(3, new Vector4(positions[i].x, positions[i].y, positions[i].z, 1));
                    matrixData[positions.Length * lodGroups[i] + matrixLengths[lodGroups[i]]] = tmp;

                    matrixLengths[lodGroups[i]]++;
                }
            }
        }

        /// <summary>
        /// Checks the distance between camera and current position to 
        /// see if it should be an LOD. 
        /// </summary>
        [BurstCompile]
        protected struct CalculateLODGroups : IJobParallelFor
        {
            public float3 origin;
            public NativeArray<float3> positions;
            public NativeArray<byte> lodGroup;
            public NativeArray<bool> doRender;
            [ReadOnly] public NativeArray<float> renderDistance;

            [BurstCompile]
            public void Execute(int index)
            {
                //If we are not rendering, ignroe
                if (doRender[index] == false)
                    return;

                //Set the base LOD to 0, in case we fail to find the correct one.
                lodGroup[index] = 0;
                float dist = math.distance(origin, positions[index]);

                //Loop through all of the render distances to find the lowest
                for (byte i = 0; i < renderDistance.Length; i++)
                {
                    //Compare the render distance, and use I as the lod group. 
                    //Any values below or at 0 will default to always.
                    if (dist < renderDistance[i] || renderDistance[i] <= 0)
                    {
                        lodGroup[index] = i;
                        return;
                    }
                }

            }
        }

        /// <summary>
        /// This is the area that we are rendering with the camera. 
        /// </summary>
        [BurstCompile]
        protected struct RenderArea
        {
            public Vector2 TopLeft { get; private set; }
            public Vector2 TopRight { get; private set; }
            public Vector2 BotLeft { get; private set; }
            public Vector2 BotRight { get; private set; }

            [BurstCompile]
            public void UpdatePoints(Vector2 topLeft, Vector2 topRight, Vector2 botLeft, Vector2 botRight)
            {
                TopLeft = topLeft;
                TopRight = topRight;
                BotLeft = botLeft;
                BotRight = botRight;
            }

            [BurstCompile]
            public bool PointInBounds(Vector3 point)
            {
                Vector2 p = new Vector2(point.x, point.z);
                if (PointInTriangle(TopLeft, BotRight, TopRight, p))
                    return true;
                if (PointInTriangle(TopLeft, BotRight, BotLeft, p))
                    return true;
                return false;
            }

            [BurstCompile]
            private bool PointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
            {
                float w1 = (A.x * (C.y - A.y) + (P.y - A.y) * (C.x - A.x) - P.x * (C.y - A.y)) / ((B.y - A.y) * (C.x - A.x) - (B.x - A.x) * (C.y - A.y));
                float w2 = ((P.y - A.y) - w1 * (B.y - A.y)) / (C.y - A.y);
                return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
            }

        }
    }
}