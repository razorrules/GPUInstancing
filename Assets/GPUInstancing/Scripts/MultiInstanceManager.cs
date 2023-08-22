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
    public class MultiInstanceManager : InstanceManagerBase
    {
        [Header("Settings")]
        [SerializeField] private InstanceMeshSet _meshSet;

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
        protected NativeArray<int> _matrixLength;

        private RenderParams[] RenderParams;

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
            _matrixLength.Dispose();
        }

        public override void Setup(int instances)
        {
            base.Setup(instances);

            Meshes = _meshSet.Meshes;

            RenderParams = new RenderParams[MeshesCount];

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

        protected override void Allocate()
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
            _matrixLength = new NativeArray<int>(MeshesCount, Allocator.Persistent);
            AllocatedKB += sizeof(int) * MeshesCount;

            AllocatedKB /= 1024;
            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");

            IsSetup = true;
        }

        protected override void PreRender(bool stopTimer = true)
        {
            base.PreRender(stopTimer);

            UpdateMatrixJob updateMatrix = new UpdateMatrixJob()
            {
                lodGroups = _meshGroup,
                matrixLengths = _matrixLength,
                matrixData = _matrixData,
                positions = _positions,
                rotations = _rotations,
                scales = _scale,
            };

            JobHandle updateMatrixHandle = updateMatrix.Schedule();
            updateMatrixHandle.Complete();

            if (stopTimer)
                FinishPreRender();
        }

        protected override void Render()
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
        protected struct UpdateMatrixJob : IJob
        {
            public NativeArray<byte> lodGroups;

            public NativeArray<int> matrixLengths;
            public NativeArray<Matrix4x4> matrixData;

            public NativeArray<float3> positions;
            public NativeArray<Quaternion> rotations;
            public NativeArray<float3> scales;

            public Matrix4x4 tmp;
            public Vector4 pos;

            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < matrixLengths.Length; i++)
                    matrixLengths[i] = 0;

                for (int i = 0; i < positions.Length; i++)
                {
                    matrixData[positions.Length * lodGroups[i] + matrixLengths[lodGroups[i]]] = Matrix4x4.TRS(positions[i], rotations[i], scales[i]);

                    matrixLengths[lodGroups[i]]++;
                }
            }
        }
    }
}