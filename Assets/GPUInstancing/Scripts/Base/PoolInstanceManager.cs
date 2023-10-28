using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using System.Collections.Generic;

namespace GPUInstancing
{

    public struct PoolInstanceData
    {
        public int index;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool doRender;

        public static int GetSize()
        {
            return sizeof(int) + // Index
                (sizeof(float) * 3) + //Position
                (sizeof(float) * 4) + //Rotation
                (sizeof(float) * 3) + //Scale
                sizeof(bool); // doRender
        }
    }

    /// <summary>
    /// This class allows you to use the GPU instancing as a pooling system. Provides helpful methods
    /// for adding and removing points. Removing points takes indexes, so you will need to implement 
    /// your own tracking as to which points you want to remove.
    /// </summary>
    public class PoolInstanceManager : InstanceManagerBase
    {
        [Header("Settings")]
        [SerializeField] private InstanceMeshSet _meshSet;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<Matrix4x4> _matrixData;

        /// <summary> Data related to all matrix's for all positions and LODS</summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<PoolInstanceData> _data;

        /// <summary> Length of the array for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<int> _matrixLength;

        /// <summary> Length of the array for a given LOD </summary>
        [NativeDisableParallelForRestriction]
        protected NativeArray<bool> _doRender;

        private RenderParams RenderParams;
        private bool _doPrerender;


        //========== Properties
        public InstanceMesh Mesh { get; protected set; }


        /// <summary>
        /// Deallocate all of the native arrays.
        /// </summary>
        protected override void Deallocate()
        {
            _data.Dispose();
            _matrixData.Dispose();
            _matrixLength.Dispose();
            _doRender.Dispose();
        }

        public override void Setup(int instances)
        {
            base.Setup(instances);

            Mesh = _meshSet.Meshes[0];

            RenderParams = new RenderParams(Mesh.material);
            RenderParams.layer = Mesh.layer;
            RenderParams.shadowCastingMode = Mesh.shadowCastingMode;
            RenderParams.receiveShadows = Mesh.receiveShadows;
            RenderParams.camera = _camera;
            //Flag that it is setup and allocate
            IsSetup = true;
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

            _data = new NativeArray<PoolInstanceData>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += PoolInstanceData.GetSize() * AvailableInstances;

            //Initial indexes
            for (int i = 0; i < _data.Length; i++)
                _data[i] = new PoolInstanceData() { index = i };

            //Allocate matrix data
            _matrixData = new NativeArray<Matrix4x4>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += (matrixSize * AvailableInstances);

            //Allocate matrix length
            _matrixLength = new NativeArray<int>(1, Allocator.Persistent);
            AllocatedKB += sizeof(int);

            AllocatedKB /= 1024;
            Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");

            IsSetup = true;
        }

        protected override void PreRender(bool stopTimer = true)
        {
            base.PreRender(false);

            if (_doPrerender)
            {
                UpdateMatrixData();
            }

            if (stopTimer)
                FinishPreRender();
        }

        private void UpdateMatrixData()
        {
            UpdateMatrixJob updateMatrix = new UpdateMatrixJob()
            {
                matrixLengths = _matrixLength,
                matrixData = _matrixData,
                data = _data,
            };

            JobHandle updateMatrixHandle = updateMatrix.Schedule();
            updateMatrixHandle.Complete();
        }

        public void CopyData(out NativeArray<PoolInstanceData> data)
        {
            data = _data;
        }

        /// <summary>
        /// Update points from a native array. Does not dispose of native array being passed in
        /// </summary>
        /// <param name="toUpdate"></param>
        public void UpdatePoints(NativeArray<PoolInstanceData> toUpdate)
        {
            UpdatePointsJob updatePoints = new UpdatePointsJob()
            {
                toUpdate = toUpdate,
                data = _data,
            };

            JobHandle updatePointsHandle = updatePoints.Schedule();
            updatePointsHandle.Complete();
            _doPrerender = true;
        }

        /// <summary>
        /// Update points based on array
        /// </summary>
        /// <param name="toUpdate"></param>
        public void UpdatePoints(PoolInstanceData[] toUpdate)
        {
            NativeArray<PoolInstanceData> toUpdateNA = new NativeArray<PoolInstanceData>(toUpdate, Allocator.TempJob);

            UpdatePoints(toUpdateNA);
            toUpdateNA.Dispose();
        }

        /// <summary>
        /// Remove points from pool based on index
        /// </summary>
        /// <param name="toRemove"></param>
        public void RemovePoints(int[] toRemove)
        {
            if (toRemove.Length == 0)
                return;
            NativeArray<int> toRemoveNA = new NativeArray<int>(toRemove, Allocator.TempJob);

            RemovePointsJob removePoints = new RemovePointsJob()
            {
                toRemove = toRemoveNA,
                data = _data,
            };

            JobHandle removePointsHandle = removePoints.Schedule();
            removePointsHandle.Complete();
            toRemoveNA.Dispose();
            _doPrerender = true;
        }

        /// <summary>
        /// Add new points to the pool
        /// </summary>
        /// <param name="toAdd"></param>
        public void AddPoints(PoolInstanceData[] toAdd)
        {
            NativeArray<PoolInstanceData> toRemoveNA = new NativeArray<PoolInstanceData>(toAdd, Allocator.TempJob);

            AddPointsJob addPoints = new AddPointsJob()
            {
                toAdd = toRemoveNA,
                data = _data,
            };

            JobHandle addPointsHandle = addPoints.Schedule();
            addPointsHandle.Complete();
            toRemoveNA.Dispose();
            _doPrerender = true;
        }

        public void UpdateData(ref NativeArray<PoolInstanceData> data)
        {
            NativeArray<PoolInstanceData>.Copy(data, _data);
            _doPrerender = true;
        }

        protected override void Render()
        {
            if (_matrixLength[0] == 0)
                return;

            Graphics.RenderMeshInstanced(RenderParams,
                Mesh.mesh,
                Mesh.submeshIndex,
                _matrixData.GetSubArray(0, _matrixLength[0])
                );
        }

        //============================================ Jobs

        [BurstCompile]
        protected struct UpdateMatrixJob : IJob
        {
            public NativeArray<int> matrixLengths;
            public NativeArray<Matrix4x4> matrixData;

            [ReadOnly] public NativeArray<PoolInstanceData> data;

            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < matrixLengths.Length; i++)
                    matrixLengths[i] = 0;

                for (int i = 0; i < matrixData.Length; i++)
                {
                    //Check if we actually want to use this
                    if (!data[i].doRender)
                        continue;

                    //Update positions and the length
                    matrixData[matrixLengths[0]] = Matrix4x4.TRS(data[i].position, data[i].rotation, data[i].scale);
                    matrixLengths[0]++;
                }
            }
        }

        [BurstCompile]
        protected struct UpdatePointsJob : IJob
        {
            [ReadOnly] public NativeArray<PoolInstanceData> toUpdate;
            public NativeArray<PoolInstanceData> data;
            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < toUpdate.Length; i++)
                {
                    //Lets check to make sure we are in bounds
                    if (toUpdate[i].index < 0 || toUpdate[i].index >= data.Length)
                        continue;

                    data[toUpdate[i].index] = toUpdate[i];
                }
            }
        }

        [BurstCompile]
        protected struct AddPointsJob : IJob
        {
            [ReadOnly] public NativeArray<PoolInstanceData> toAdd;
            public NativeArray<PoolInstanceData> data;
            [BurstCompile]
            public void Execute()
            {
                int added = 0;
                PoolInstanceData tmp;

                for (int i = 0; i < data.Length; i++)
                {
                    //Look for a position that is not being used
                    if (data[i].doRender)
                        continue;

                    tmp = toAdd[added];
                    tmp.index = i;
                    data[i] = tmp;
                    added++;
                    if (added >= toAdd.Length)
                        return;
                }

                Debug.Log("Failed to add points to job, ran out of allocated spaces.");
            }
        }
        [BurstCompile]
        protected struct RemovePointsJob : IJob
        {
            [ReadOnly] public NativeArray<int> toRemove;
            public NativeArray<PoolInstanceData> data;
            [BurstCompile]
            public void Execute()
            {
                PoolInstanceData tmp;
                for (int i = 0; i < toRemove.Length; i++)
                {
                    //Lets check to make sure we are in bounds
                    if (toRemove[i] < 0 || toRemove[i] >= data.Length)
                        continue;

                    tmp = data[toRemove[i]];
                    tmp.doRender = false;
                    data[toRemove[i]] = tmp;
                }
            }
        }
    }

}