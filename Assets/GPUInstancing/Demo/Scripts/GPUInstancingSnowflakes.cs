using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace Laio.GPUInstancing.Samples
{

    /// <summary>
    /// Manages spawning instancing snow and causing it fall to the ground based on a wind value.
    /// Upon reaching the ground, the snow will be reused and moved back to sky level and given
    /// a random rotation.
    /// </summary>
    public class GPUInstancingSnowflakes : DynamicInstanceManager
    {

        // Flag we can use to pause movement and see the scene as is
        public bool pauseMovement;

        [Space(10), Tooltip("Movement applied to the snow")]
        //Movement of the snow
        public Vector3 movement;

        [Header("Positioning and scaling")]
        public int startY;
        public int areaSize;
        public Vector3 startScale;

        //Manage a seed for math.random
        private uint baseSeed;

        protected override void Update()
        {
            base.Update();

            if (!IsSetup)
                return;

            baseSeed++;

            if (baseSeed >= uint.MaxValue - (AvailableInstances * 10))
                baseSeed = 1;
        }

        protected override void PostAllocation()
        {
            base.PostAllocation();
            baseSeed = 1;
            Layout();
        }

        /// <summary>
        /// Layout the initial snow based on area size. Set default scale, and random rotation
        /// </summary>
        private void Layout()
        {
            Random r = new Random();

            for (int i = 0; i < AvailableInstances; i++)
            {
                SetData(i,
                /*Position*/new float3(r.Next(-areaSize, areaSize), r.Next(0, startY), r.Next(-areaSize, areaSize)),
                /*Rotation*/Quaternion.Euler(new Vector3(r.Next(0, 360), r.Next(0, 360), r.Next(0, 360))),
                /*  Scale */startScale);
            }
        }

        /// <summary>
        /// Manage movement for the snow inside of prerender
        /// </summary>
        /// <param name="stopTimer"></param>
        protected override void PreRender(bool stopTimer = true)
        {
            //If movement is paused, ignore job
            if (!pauseMovement)
            {
                Debug.Log("Prerender");
                //Make the snow fall
                SnowFlakeFall snowFlakeFall = new SnowFlakeFall()
                {
                    positions = _positions,
                    windSpeed = new float3(movement.x, movement.y, movement.z),
                    deltaTime = Time.deltaTime,
                };

                JobHandle snowflakeHandle = snowFlakeFall.Schedule(_positions.Length, 1);
                snowflakeHandle.Complete();

                //See of the snow needs to be reset, as it went through the ground
                ResetCheck resetCheck = new ResetCheck()
                {
                    positions = _positions,
                    rotations = _rotations,
                    startY = startY,
                    areaSize = areaSize,
                    baseSeed = baseSeed,
                };

                JobHandle resetCheckHandle = resetCheck.Schedule(_positions.Length, 1);
                resetCheckHandle.Complete();

                base.PreRender(false);

            }

            //Ensure that we stop the pre-render timer so we can track performance
            if (stopTimer)
                FinishPreRender();
        }

        /// <summary>
        /// Job that handles resetting the snow
        /// </summary>
        protected struct ResetCheck : IJobParallelFor
        {

            public NativeArray<float3> positions;
            public NativeArray<Quaternion> rotations;

            [ReadOnly] public float areaSize;
            [ReadOnly] public float startY;
            [ReadOnly] public uint baseSeed;
            public void Execute(int index)
            {
                Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(baseSeed + (index * 10)));
                if (positions[index].y < 0)
                {
                    positions[index] = new float3(random.NextFloat(-areaSize, areaSize), random.NextFloat(0, startY), random.NextFloat(-areaSize, areaSize));
                    rotations[index] = Quaternion.Euler(new Vector3(random.NextFloat(0, 360), random.NextFloat(0, 360), random.NextFloat(0, 360)));
                }
            }
        }

        /// <summary>
        /// Job that handles the snow falling
        /// </summary>
        protected struct SnowFlakeFall : IJobParallelFor
        {

            public NativeArray<float3> positions;

            [ReadOnly] public float deltaTime;
            [ReadOnly] public float3 windSpeed;
            [ReadOnly] public float startY;
            public void Execute(int index)
            {
                positions[index] += windSpeed * deltaTime;
            }
        }

    }

}