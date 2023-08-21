using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GPUInstancing.Samples
{

    /// <summary>
    /// Lays out meshes in a grid and applies perlin noise to it
    /// </summary>
    public class GPUInstancingPerlinNoise : InstanceManager
    {
        [Header("Settings")]
        public float scale = 8.0f;
        public float heightScale = 5.0f;
        public float timeScale = 2.0f;
        public float yScale;

        private void OnValidate()
        {
            if (yScale < 0)
                yScale = 0;
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

                _matrixData[i] = Matrix4x4.TRS(
                    new float3(x, 0, -y),
                    Quaternion.identity,
                    Vector3.one);

                y++;
            }
        }

        protected override void PreRender(bool stopTimer = true)
        {
            base.PreRender(false);

            //Schedule the update position matrix
            UpdatePositionsJob updatePositionsJob = new UpdatePositionsJob();
            updatePositionsJob.matrix = _matrixData;
            updatePositionsJob.time = Time.time;
            updatePositionsJob.divFactor = scale;
            updatePositionsJob.timeFactor = timeScale;
            updatePositionsJob.heightScale = heightScale;
            updatePositionsJob.yScale = yScale;

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
            public float yScale;

            public void Execute(int index)
            {
                //Store reference to matrix
                Matrix4x4 temp = matrix[index];

                //Set the Y position
                temp.m13 = Mathf.PerlinNoise(
                    (temp.GetPosition().x + (time * timeFactor)) / divFactor, // X
                    (temp.GetPosition().z + (time * timeFactor)) / divFactor) // Y
                    * heightScale; // Height multiplier

                //Set the Y scale, if 0 then default to 1
                if (yScale != 0)
                    temp.m11 = yScale; // Y
                else
                    temp.m11 = 1;

                //Update matrix
                matrix[index] = temp;

            }

        }

    }

}