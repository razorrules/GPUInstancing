using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GPUInstancing.Samples
{

    public class GPUInstancingRotatingShapes : MultiInstanceManager
    {

        public int spawnRadius;
        public float rotateSpeed;

        [NativeDisableParallelForRestriction]
        protected NativeArray<float> _angle;

        [NativeDisableParallelForRestriction]
        protected NativeArray<float> _radius;

        protected override void Deallocate()
        {
            base.Deallocate();
            _angle.Dispose();
            _radius.Dispose();
        }

        public override void Allocate(int instancesCount)
        {
            base.Allocate(instancesCount);

            _angle = new NativeArray<float>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += sizeof(float) * AvailableInstances;

            _radius = new NativeArray<float>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += sizeof(float) * AvailableInstances;

            Layout();
        }

        private void Layout()
        {

            for (int i = 0; i < AvailableInstances; i++)
            {

                System.Random r = new System.Random();

                _scale[i] = new float3(1, 1, 1);
                _rotations[i] = Quaternion.identity;
                _matrixData[i] = Matrix4x4.TRS(
                    float3.zero,
                    Quaternion.identity,
                    Vector3.one);

                _angle[i] = r.Next(0, 360);
                _radius[i] = (float)r.NextDouble() * spawnRadius;

            }
        }

        protected override void PreRender(bool stopTimer = true)
        {
            base.PreRender(false);

            //Calculate the LOD groups and what different points should use
            CircleCenter circleCenter = new CircleCenter()
            {
                positions = _positions,
                rotation = _rotations,
                scale = _scale,
                angle = _angle,
                radius = _radius,
                rotateSpeed = rotateSpeed,
                deltaTime = Time.deltaTime,
            };

            JobHandle circleCenterHandle = circleCenter.Schedule(_positions.Length, 1);
            circleCenterHandle.Complete();

            //Calculate the LOD groups and what different points should use
            MeshSelection lodCheck = new MeshSelection()
            {
                positions = _positions,
                lodGroup = _lodGroup,
            };

            JobHandle lodCheckHandle = lodCheck.Schedule(_positions.Length, 1);
            lodCheckHandle.Complete();


            if (stopTimer)
                FinishPreRender();
        }

        protected override void Setup()
        {
            base.Setup();

        }


        /// <summary>
        /// Checks the distance between camera and current position to 
        /// see if it should be an LOD. 
        /// </summary>
        [BurstCompile]
        protected struct CircleCenter : IJobParallelFor
        {
            public NativeArray<float> angle;
            public NativeArray<float> radius;
            public NativeArray<float3> positions;
            public NativeArray<Quaternion> rotation;
            public NativeArray<float3> scale;
            [ReadOnly] public float rotateSpeed;
            [ReadOnly] public float deltaTime;

            [BurstCompile]
            public void Execute(int index)
            {
                angle[index] += rotateSpeed * deltaTime;

                positions[index] = new float3(
                    radius[index] * math.cos(angle[index] * math.PI / 180f),
                    0,
                    radius[index] * math.sin(angle[index] * math.PI / 180f));

            }

        }

        /// <summary>
        /// Checks the distance between camera and current position to 
        /// see if it should be an LOD. 
        /// </summary>
        [BurstCompile]
        protected struct MeshSelection : IJobParallelFor
        {
            public NativeArray<float3> positions;
            public NativeArray<byte> lodGroup;

            [BurstCompile]
            public void Execute(int index)
            {
                //Set the base LOD to 0, in case we fail to find the correct one.
                bool positiveX = positions[index].x > 0;
                bool positiveZ = positions[index].z > 0;

                if (positiveX)
                {
                    if (positiveZ)
                        lodGroup[index] = 0;
                    else
                        lodGroup[index] = 1;
                }
                else
                {
                    if (positiveZ)
                        lodGroup[index] = 2;
                    else
                        lodGroup[index] = 3;
                }

            }
        }

    }

}