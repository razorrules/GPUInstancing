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
    public class DynamicInstanceManager : InstanceManagerBase
    {
        [Header("Settings")]
        [SerializeField] private InstanceMeshSet _meshSet;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        //List of all positions on the grid
        [NativeDisableParallelForRestriction]
        protected NativeArray<float3> _positions;

        [NativeDisableParallelForRestriction]
        protected NativeArray<Quaternion> _rotations;

        [NativeDisableParallelForRestriction]
        protected NativeArray<float3> _scale;

        protected InstanceMesh Mesh { get; set; }
        protected RenderParams RenderParams;

        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected override void Deallocate()
        {
            _positions.Dispose();
            _rotations.Dispose();
            _scale.Dispose();
            _matrixData.Dispose();
        }

        /// <summary>
        /// Setup the instance manager with X instances available
        /// </summary>
        /// <param name="instances"></param>
        public override void Setup(int instances)
        {
            //Ensure to call base
            base.Setup(instances);

            //Set the mesh then we need to validate everything
            Mesh = _meshSet.Meshes[0];

            if (!Validate())
                return;

            //Setup after validation (Checks if material is valid)
            RenderParams = new RenderParams(Mesh.material);
            RenderParams.layer = Mesh.layer;
            RenderParams.shadowCastingMode = Mesh.shadowCastingMode;
            RenderParams.receiveShadows = Mesh.receiveShadows;
            RenderParams.camera = _camera;

            //Flag that it is setup and allocate
            IsSetup = true;
            Allocate();
        }

        /// <summary>
        /// Validates that everything is setup correctly
        /// </summary>
        /// <returns></returns>
        private bool Validate()
        {
            if (Mesh == null)
            {
                Debug.LogError("No mesh assigned to  " + GetType().Name);
                return false;
            }

            if (Mesh.material == null)
            {
                Debug.LogError("The material assigned to mesh in meshset is null " + GetType().Name);
                return false;
            }

            if (_camera == null)
            {
                Debug.LogError("No camera assigned to " + GetType().Name + ". Could not find valid camera in scene.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Allocate all of the native arrays we plan on using
        /// </summary>
        protected override void Allocate()
        {
            //Keep track of how much data we are allocating
            AllocatedKB = 0;
            int floatSize = sizeof(float);
            int matrixSize = floatSize * 16;
            int float3Size = floatSize * 3;

            //Ensure all of the native arrays are setup
            _positions = new NativeArray<float3>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += float3Size * AvailableInstances;

            //Ensure all of the native arrays are setup
            _rotations = new NativeArray<Quaternion>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (floatSize * 4) * AvailableInstances;

            //Ensure all of the native arrays are setup
            _scale = new NativeArray<float3>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += float3Size * AvailableInstances;

            //Allocate matrix data
            _matrixData = new NativeArray<Matrix4x4>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (matrixSize * AvailableInstances);

            //Finally divide by 1024 so we get KB
            AllocatedKB /= 1024;

            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");
        }

        /// <summary>
        /// Handles prerendering
        /// </summary>
        /// <param name="stopTimer"></param>
        protected override void PreRender(bool stopTimer = true)
        {
            //Ensure to call base as that manages the timer
            base.PreRender(false);

            UpdateMatrixJob updateMatrix = new UpdateMatrixJob()
            {
                matrixData = _matrixData,
                positions = _positions,
                rotations = _rotations,
                scales = _scale,
            };

            JobHandle updateMatrixHandle = updateMatrix.Schedule(_matrixData.Length, 1);
            updateMatrixHandle.Complete();

            //Once we are finished, call finish pre-render so we can track CPU impact
            if (stopTimer)
                FinishPreRender();
        }

        /// <summary>
        /// Render all of the meshes
        /// </summary>
        protected override void Render()
        {
            Graphics.RenderMeshInstanced(RenderParams,
                Mesh.mesh,
                Mesh.submeshIndex,
                _matrixData);
        }

        /// <summary>
        /// Updates each matrix to match the position, rotation, and scale set in native arrays.
        /// </summary>
        [BurstCompile]
        protected struct UpdateMatrixJob : IJobParallelFor
        {

            public NativeArray<Matrix4x4> matrixData;

            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<Quaternion> rotations;
            [ReadOnly] public NativeArray<float3> scales;

            [BurstCompile]
            public void Execute(int index)
            {
                matrixData[index] = Matrix4x4.TRS(positions[index], rotations[index], scales[index]);
            }

        }

    }
}