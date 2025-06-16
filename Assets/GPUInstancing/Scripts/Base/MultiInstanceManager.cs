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
    /// This class allows for multiple different meshes to be used in one script. This is not as
    /// performant as writing different managers, but makes it easier to work with considered centralized 
    /// data. The main drawback to this, is if we have 1000 max instances, with 4 different meshes, then we 
    /// need to account for all combinations. (Combinations meaning 1000 cubes rendered | 500 cubes, 500 spheres).
    /// To make this easier, we just set the matrix data array to instances * meshes. Meaning we allocate four
    /// times as much.
    /// </summary>
    public class MultiInstanceManager : InstanceManagerBase
    {
        [SerializeField] protected InstanceMeshSet _meshSet;

        //List of all positions on the grid
        [NativeDisableParallelForRestriction]
        protected NativeArray<float3> _positions;
        [NativeDisableParallelForRestriction]
        protected NativeArray<Quaternion> _rotations;
        [NativeDisableParallelForRestriction]
        protected NativeArray<float3> _scale;

        /// <summary> Byte for the LOD group a given position belongs too. </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<byte> _meshGroup;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        /// <summary> Length of the array for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<int> _meshGroupLength;

        protected RenderParams[] RenderParams;

        //========== Properties
        public int MeshesCount { get => Meshes.Length; }
        public InstanceMesh[] Meshes { get; protected set; }

        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected override void Deallocate()
        {
            _positions.Dispose();
            _rotations.Dispose();
            _scale.Dispose();
            _meshGroup.Dispose();
            _matrixData.Dispose();
            _meshGroupLength.Dispose();
        }

        public override void Setup(int instances)
        {
            base.Setup(instances);

            Meshes = _meshSet.Meshes;

            //Setup the render params array
            RenderParams = new RenderParams[MeshesCount];

            //Setup the render params to the meshes data
            for (int i = 0; i < RenderParams.Length; i++)
            {
                RenderParams[i] = new RenderParams(Meshes[i].material);
                RenderParams[i].layer = Meshes[i].layer;
                RenderParams[i].shadowCastingMode = Meshes[i].shadowCastingMode;
                RenderParams[i].receiveShadows = Meshes[i].receiveShadows;
                RenderParams[i].camera = _camera;
            }

            Allocate();
        }

        protected override void Allocate(bool finishAllocation = true)
        {
            //Lets allocate all of the arrays, we will also track how much we allocated
            //Float 3 does not have a predefined size, but it contains 3 floats
            //Matrix4x4 does not have a predefined size, but it contains 16 floats
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

            //Allocate LOD groups
            _meshGroup = new NativeArray<byte>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (sizeof(byte) * AvailableInstances);

            //Allocate matrix data
            _matrixData = new NativeArray<Matrix4x4>(AvailableInstances * MeshesCount, Allocator.Persistent);
            AllocatedKB += (matrixSize * AvailableInstances) * MeshesCount;

            //Allocate matrix length
            _meshGroupLength = new NativeArray<int>(MeshesCount, Allocator.Persistent);
            AllocatedKB += sizeof(int) * MeshesCount;

            if (finishAllocation)
                FinishAllocation();
        }

        protected override void PreRender(bool finishPreRender = true)
        {
            base.PreRender(finishPreRender);

            UpdateMatrixJob updateMatrix = new UpdateMatrixJob()
            {
                meshGroup = _meshGroup,
                meshGroupsLength = _meshGroupLength,
                matrixData = _matrixData,
                positions = _positions,
                rotations = _rotations,
                scales = _scale,
            };

            JobHandle updateMatrixHandle = updateMatrix.Schedule();
            updateMatrixHandle.Complete();

            if (finishPreRender)
                FinishPreRender();
        }

        protected override void Render()
        {
            for (int i = 0; i < MeshesCount; i++)
            {
                if (_meshGroupLength[i] == 0)
                    continue;

                Graphics.RenderMeshInstanced(RenderParams[i],
                    Meshes[i].mesh,
                    Meshes[i].submeshIndex,
                    _matrixData.GetSubArray(i * AvailableInstances, _meshGroupLength[i])
                    );
            }
        }

        [BurstCompile]
        protected struct UpdateMatrixJob : IJob
        {
            public NativeArray<int> meshGroupsLength;
            public NativeArray<Matrix4x4> matrixData;

            [ReadOnly] public NativeArray<byte> meshGroup;

            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<Quaternion> rotations;
            [ReadOnly] public NativeArray<float3> scales;

            [BurstCompile]
            public void Execute()
            {
                //Reset all lengths to 0
                for (int i = 0; i < meshGroupsLength.Length; i++)
                    meshGroupsLength[i] = 0;

                //Loop through all positions
                for (int i = 0; i < positions.Length; i++)
                {
                    //Update position data. Multiply the total positions by meshGroup to get the correct offset, then use the meshGroupsLength to set the next element in the sequence.
                    matrixData[positions.Length * meshGroup[i] + meshGroupsLength[meshGroup[i]]] = Matrix4x4.TRS(positions[i], rotations[i], scales[i]);

                    //Increment the length of meshGroups
                    meshGroupsLength[meshGroup[i]]++;
                }
            }
        }
    }
}