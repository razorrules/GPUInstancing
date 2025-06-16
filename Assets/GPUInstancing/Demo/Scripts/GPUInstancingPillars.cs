using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Laio.GPUInstancing.Samples
{
    public class GPUInstancingPillars : SingleInstanceManager
    {
        [Header("Construct settings")]
        public float gridOffset;
        public Vector3 rotation;

        [Header("Player Interaction")]
        public Transform player;
        public float minDist;
        public float maxDist;
        public float height;

        [Header("Settings")]
        public float scale = 8.0f;
        public float heightScale = 5.0f;
        public float timeScale = 2.0f;

        protected override void PostAllocation()
        {
            base.PostAllocation();
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
                    new float3(x * gridOffset, 0, -y * gridOffset),
                    Quaternion.Euler(rotation),
                    Vector3.one);

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

            updatePositionsJob.playerPos = new float2(player.position.x, player.position.z);
            updatePositionsJob.minDist = minDist;
            updatePositionsJob.maxDist = maxDist;
            updatePositionsJob.height = height;

            JobHandle updatePositionsHandle = updatePositionsJob.Schedule(_matrixData.Length, 16);
            updatePositionsHandle.Complete();

            if (stopTimer)
                FinishPreRender();
        }

        [BurstCompile]
        private struct UpdatePositionsJob : IJobParallelFor
        {
            public NativeArray<Matrix4x4> matrix;
            //Perlin settings
            [ReadOnly] public float timeFactor;
            [ReadOnly] public float divFactor;
            [ReadOnly] public float time;
            [ReadOnly] public float heightScale;

            //Interation settings
            [ReadOnly] public float2 playerPos;
            [ReadOnly] public float minDist;
            [ReadOnly] public float maxDist;
            [ReadOnly] public float height;

            public void Execute(int index)
            {
                Matrix4x4 temp = matrix[index];

                Vector4 pos = new Vector4(temp.GetPosition().x, 0, temp.GetPosition().z, 1);

                float dist = math.distance(
                    new float2(temp.GetPosition().x, temp.GetPosition().z), playerPos);

                float blend = 1 - GetBlend(dist, minDist, maxDist);

                pos.y = math.lerp(Mathf.PerlinNoise((temp.GetPosition().x + (time * timeFactor)) / divFactor,
                    (temp.GetPosition().z + (time * timeFactor)) / divFactor) * heightScale,
                    height,
                    blend);

                temp.SetColumn(3, pos);
                matrix[index] = temp;

            }

            private float GetBlend(float current, float min, float max)
            {
                if (current < min)
                    return 0.0f;
                if (current > max)
                    return 1.0f;
                current -= min;
                max -= min;
                return current / max;
            }
        }

    }

}