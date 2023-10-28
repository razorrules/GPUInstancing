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
    /// This is a base class that provides no additional functionality other then boilerplate code to 
    /// render a mesh set. 
    /// </summary>
    public class InstanceManager : InstanceManagerBase
    {
        [SerializeField] private InstanceMeshSet _meshSet;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        private RenderParams RenderParams;

        //========== Properties
        public InstanceMesh Mesh { get; protected set; }

        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected override void Deallocate()
        {
            _matrixData.Dispose();
        }

        /// <summary>
        /// Called to setup the instance manager. 
        /// </summary>
        /// <param name="instances"></param>
        public override void Setup(int instances)
        {
            base.Setup(instances);
            Allocate();
        }

        protected virtual void PreAllocate()
        {
            Mesh = _meshSet.Meshes[0];
        }

        /// <summary>
        /// Called after Setup and after everything has been allocated.
        /// </summary>
        protected virtual void PostSetup() { }

        /// <summary>
        /// Allocate all native arrays and other related date.
        /// </summary>
        protected override void Allocate()
        {
            PreAllocate();

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

            IsSetup = true;

            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");
            PostSetup();
        }

        protected override void Render()
        {
            Graphics.RenderMeshInstanced(RenderParams,
                Mesh.mesh,
                Mesh.submeshIndex,
                _matrixData);
        }


    }
}