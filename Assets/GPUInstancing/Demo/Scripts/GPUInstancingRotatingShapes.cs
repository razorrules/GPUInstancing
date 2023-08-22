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
        public float radiusSpeedBoost = 0;

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

        protected override void Allocate()
        {
            base.Allocate();

            _angle = new NativeArray<float>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += sizeof(float) * AvailableInstances;

            _radius = new NativeArray<float>(AvailableInstances, Allocator.Persistent);
            AllocatedKB += sizeof(float) * AvailableInstances;

            Layout();
            Debug.Log("Allocated");
        }

        /// <summary>
        /// Layout the objects in a random pattern. 
        /// This is done by given random radius and random angle.
        /// </summary>
        private void Layout()
        {
            for (int i = 0; i < AvailableInstances; i++)
            {

                System.Random r = new System.Random();

                //Default matrix data
                _scale[i] = new float3(1, 1, 1);
                _rotations[i] = Quaternion.identity;
                _matrixData[i] = Matrix4x4.TRS(
                    float3.zero,
                    Quaternion.identity,
                    Vector3.one);

                //Set random angle and radius
                _angle[i] = r.Next(0, 360);
                _radius[i] = (float)r.NextDouble() * spawnRadius;

            }
        }

        protected override void PreRender(bool stopTimer = true)
        {
            //Make all of the points circle the center
            CircleCenter circleCenter = new CircleCenter()
            {
                positions = _positions,
                rotation = _rotations,
                scale = _scale,
                angle = _angle,
                radius = _radius,
                rotateSpeed = rotateSpeed,
                radiusSpeedBoost = radiusSpeedBoost,
                deltaTime = Time.deltaTime,
            };

            JobHandle circleCenterHandle = circleCenter.Schedule(_positions.Length, 1);
            circleCenterHandle.Complete();

            //Change the mesh dependent on position
            MeshSelection lodCheck = new MeshSelection()
            {
                positions = _positions,
                meshGroup = _meshGroup,
            };

            JobHandle lodCheckHandle = lodCheck.Schedule(_positions.Length, 1);
            lodCheckHandle.Complete();

            //We want to call base pre-render after we finish moving around objects, as that will
            //re-order the matrix data and ensure it is up to date with current changes
            base.PreRender(false);

            if (stopTimer)
                FinishPreRender();
        }

        /// <summary>
        /// Job that rotates positions around a given point
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
            [ReadOnly] public float radiusSpeedBoost;

            [BurstCompile]
            public void Execute(int index)
            {
                angle[index] += (rotateSpeed + (radius[index] * radiusSpeedBoost)) * deltaTime;

                positions[index] = new float3(
                    radius[index] * math.cos(angle[index] * math.PI / 180f),
                    0,
                    radius[index] * math.sin(angle[index] * math.PI / 180f));
            }
        }

        /// <summary>
        /// Checks the position of each instanced mesh and sets the mesh group depending on quadrant in world space
        /// </summary>
        [BurstCompile]
        protected struct MeshSelection : IJobParallelFor
        {
            public NativeArray<float3> positions;
            public NativeArray<byte> meshGroup;

            [BurstCompile]
            public void Execute(int index)
            {
                //Set the base LOD to 0, in case we fail to find the correct one.
                bool positiveX = positions[index].x > 0;
                bool positiveZ = positions[index].z > 0;

                if (positiveX)
                {
                    if (positiveZ)
                        meshGroup[index] = 0;
                    else
                        meshGroup[index] = 1;
                }
                else
                {
                    if (positiveZ)
                        meshGroup[index] = 2;
                    else
                        meshGroup[index] = 3;
                }

            }
        }

    }

}