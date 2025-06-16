using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;



namespace Laio.GPUInstancing
{

    /// <summary>
    /// This manager handles spawning instanced meshes with the ability
    /// to add LOD's to them.
    /// </summary>
    public class LODInstanceManager : MultiInstanceManager
    {

        [Header("Settings")]
        [SerializeField] protected float[] _renderDistance;

        /// <summary> Render distance for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<float> _renderDistanceArray;

        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected override void Deallocate()
        {
            base.Deallocate();
            _renderDistanceArray.Dispose();
        }

        /// <summary>
        /// Update render distance constantly, that way you are not forced to restart. 
        /// While also ensuring that the length of the render distance array is the 
        /// correct size.
        /// </summary>
        private void OnValidate()
        {
            //Ensure array size is correct
            if (_renderDistance != null && _meshSet != null)
                if (_renderDistance.Length != _meshSet.Meshes.Length - 1)
                    _renderDistance = new float[_meshSet.Meshes.Length - 1];

            //Update render distance real time.
            if (_renderDistanceArray == null || Meshes == null)
                return;
            for (int i = 0; i < MeshesCount - 1; i++)
                _renderDistanceArray[i] = _renderDistance[i];
        }

        protected override void Allocate(bool finishAllocation = true)
        {
            if (Meshes == null || Meshes.Length == 0)
            {
                Debug.Log("InstanceSpawningManager cannot allocate and setup without meshes.");
                return;
            }
            base.Allocate(false);

            //Setup the render distance into an array so we can pass it to jobs
            _renderDistanceArray = new NativeArray<float>(MeshesCount, Allocator.Persistent);

            for (int i = 0; i < MeshesCount - 1; i++)
                _renderDistanceArray[i] = _renderDistance[i];

            AllocatedKB += sizeof(float) * MeshesCount;

            if (finishAllocation)
                FinishAllocation();
        }

        protected override void PreRender(bool finishPreRender = true)
        {
            base.PreRender(false);

            //Calculate the LOD groups and what different points should use
            CalculateLODGroups lodCheck = new CalculateLODGroups()
            {
                origin = _camera.transform.position,
                renderDistance = _renderDistanceArray,
                positions = _positions,
                lodGroup = _meshGroup,
                MaxLOD = (byte)(MeshesCount - 1),
            };

            JobHandle lodCheckHandle = lodCheck.Schedule(_positions.Length, 1);
            lodCheckHandle.Complete();

            if (finishPreRender)
                FinishPreRender();
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
            [ReadOnly] public NativeArray<float> renderDistance;
            [ReadOnly] public byte MaxLOD;

            [BurstCompile]
            public void Execute(int index)
            {
                //Set the base LOD to 0, in case we fail to find the correct one.
                lodGroup[index] = MaxLOD;
                float dist = math.distance(origin, positions[index]);

                //Loop through all of the render distances to find the lowest
                for (byte i = 0; i < renderDistance.Length; i++)
                {
                    //Compare the render distance, and use I as the lod group. 
                    //Any values below or at 0 will default to always.
                    if (dist < renderDistance[i])
                    {
                        lodGroup[index] = i;
                        return;
                    }
                }

            }
        }

    }
}