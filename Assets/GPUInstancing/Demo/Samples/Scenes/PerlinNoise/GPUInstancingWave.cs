using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GPUInstancing.Samples
{

    public class GPUInstancingWave : InstanceSpawningManager
    {
        [Header("Settings")]
        public float scale = 8.0f;
        public float heightScale = 5.0f;
        public float timeScale = 2.0f;

        protected override void Deallocate()
        {
            base.Deallocate();
        }

        protected override void Setup()
        {
            base.Setup();
            int sqr = (int)Mathf.Sqrt(AvailableInstances);
        }

        public override void Allocate(int instancesCount)
        {
            base.Allocate(instancesCount);
            GridLayout();
        }

        private void GridLayout()
        {
            //Next, we will set the grid of positions. This is temp
            int x = 0;
            int y = 0;

            int rowSize = (int)Mathf.Sqrt(AvailableInstances);

            for (int i = 0; i < AvailableInstances; i++)
            {
                if (y >= rowSize)
                {
                    x++;
                    y = 0;
                }

                _doRender[i] = false;
                _positions[i] = new float3(x, 0, -y);

                for (int lod = 0; lod < MeshesCount; lod++)
                {
                    _matrixData[(lod * AvailableInstances) + i] = Matrix4x4.TRS(_positions[i], Quaternion.identity, Meshes[lod].scale);
                }

                y++;
            }
        }

        protected override void PreRender(bool stopTimer = true)
        {
            base.PreRender(false);

            UpdatePositionsJob updatePositionsJob = new UpdatePositionsJob();
            updatePositionsJob.matrix = _matrixData;
            updatePositionsJob.time = Time.time;
            updatePositionsJob.divFactor = scale;
            updatePositionsJob.timeFactor = timeScale;
            updatePositionsJob.heightScale = heightScale;

            JobHandle updatePositionsHandle = updatePositionsJob.Schedule(_matrixData.Length, 16);
            updatePositionsHandle.Complete();

            if (stopTimer)
                FinishPreRender();
        }

        [BurstCompile]
        private struct UpdatePositionsJob : IJobParallelFor
        {
            public NativeArray<Matrix4x4> matrix;
            public float timeFactor;
            public float divFactor;
            public float time;
            public float heightScale;

            public void Execute(int index)
            {
                Matrix4x4 temp = matrix[index];

                Vector4 pos = new Vector4(temp.GetPosition().x, 0, temp.GetPosition().z, 1);

                pos.y = Mathf.PerlinNoise((temp.GetPosition().x + (time * timeFactor)) / divFactor,
                    (temp.GetPosition().z + (time * timeFactor)) / divFactor) * heightScale;

                temp.SetColumn(3, pos);
                matrix[index] = temp;

            }
        }

    }

}