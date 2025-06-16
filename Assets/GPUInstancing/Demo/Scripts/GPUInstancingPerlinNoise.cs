using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Laio.GPUInstancing.Samples
{

    /// <summary>
    /// Lays out meshes in a grid and applies perlin noise to it
    /// </summary>
    public class GPUInstancingPerlinNoise : SingleInstanceManager
    {
        public const float TIME_OFFSET = 10000.0f;

        [Header("Perlin Noise")]
        public float scale = 8.0f;
        public float heightScale = 5.0f;
        public float timeScale = 2.0f;

        [Header("Mesh")]
        public float meshYScale;

        private void OnValidate()
        {
            if (meshYScale < 0)
                meshYScale = 0;
        }

        protected override void PostAllocation()
        {
            base.PostAllocation();
            GridLayout();
            Debug.Log("Post alloc");
        }

        /// <summary>
        /// Setup the matrix data so all meshes are in a grid
        /// </summary>
        private void GridLayout()
        {
            int rowSize = (int)Mathf.Sqrt(AvailableInstances);

            for (int i = 0; i < AvailableInstances; i++)
            {
                int x = i % rowSize;
                int y = i / rowSize;

                _matrixData[i] = Matrix4x4.TRS(
                    new float3(x, 0, -y),
                    Quaternion.identity,
                    Vector3.one);
            }
        }

        protected override void PreRender(bool finishPreRender = true)
        {
            base.PreRender(false);

            //Schedule the update position matrix
            UpdatePositionsJob updatePositionsJob = new UpdatePositionsJob();
            updatePositionsJob.matrix = _matrixData;
            updatePositionsJob.time = Time.time + TIME_OFFSET;
            updatePositionsJob.divFactor = scale;
            updatePositionsJob.timeFactor = timeScale;
            updatePositionsJob.heightScale = heightScale;
            updatePositionsJob.yScale = meshYScale;

            JobHandle updatePositionsHandle = updatePositionsJob.Schedule(_matrixData.Length, 16);
            updatePositionsHandle.Complete();

            Debug.Log("Prerender");
            if (finishPreRender)
                FinishPreRender();
        }

        protected override void Render()
        {
            base.Render();
            Debug.Log("Render");
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