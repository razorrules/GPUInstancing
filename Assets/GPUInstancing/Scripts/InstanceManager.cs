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
    public class InstanceManager : MonoBehaviour
    {
        //TODO: Handle culling if it is incredibly close
        //TODO: Investigate why CPU time is high, probably some easy optimization that can be done.
        public const float CAMERA_CULL_OFFSET_PIXELS = 100;

        [Header("Settings")]
        [SerializeField] protected int numInstances = 100;
        [SerializeField] protected bool constructInAwake = false;
        [SerializeField] private InstanceMeshSet _meshSet;
        [SerializeField] protected Camera _camera;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        private RenderParams RenderParams;

#if UNITY_EDITOR
        private Stopwatch _prerenderTimer;
#endif

        //========== Properties
        public int AvailableInstances { get; private set; } = 0;
        public long CpuTimeMilliseconds { get; private set; } = 0;
        public float AllocatedKB { get; protected set; } = 0;
        public bool IsSetup { get; private set; } = false;
        public InstanceMesh Mesh { get; protected set; }

        public float[] renderDist;

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
            _matrixData.Dispose();
        }

        private void Update()
        {
            if (!IsSetup)
                return;

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
            Mesh = _meshSet.Meshes[0];
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

            if (Mesh == null)
            {
                Debug.Log("InstanceSpawningManager cannot allocate and setup without meshes.");
                return;
            }

            RenderParams = new RenderParams(Mesh.material);
            RenderParams.layer = Mesh.layer;
            RenderParams.shadowCastingMode = Mesh.shadowCastingMode;
            RenderParams.receiveShadows = Mesh.receiveShadows;
            RenderParams.camera = _camera;

            AvailableInstances = instancesCount;

            //Lets allocate all of the arrays, we will also track how much we allocated
            //Float 3 does not have a predefined size, but it contains 3 floats
            //Matrix4x4 does not have a predefined size, but it contains 16 floats
            AllocatedKB = 0;
            int floatSize = sizeof(float);
            int matrixSize = floatSize * 16;

            //Allocate matrix data
            _matrixData = new NativeArray<Matrix4x4>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (matrixSize * AvailableInstances);

            AllocatedKB /= 1024;
            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");

            IsSetup = true;
        }

        protected virtual void PreRender(bool stopTimer = true)
        {
#if UNITY_EDITOR
            _prerenderTimer.Restart();
#endif
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
            Graphics.RenderMeshInstanced(RenderParams,
                Mesh.mesh,
                Mesh.submeshIndex,
                _matrixData);
        }
    }
}