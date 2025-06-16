using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Laio.GPUInstancing.Samples
{

    public class GPUInstancingLOD : LODInstanceManager
    {
        [Header("Construct settings")]
        public float gridOffset;
        public Vector3 rotation;

        protected override void PostAllocation()
        {
            base.PostAllocation();
            GridLayout();
        }

        private void GridLayout()
        {
            //Next, we will set the grid of positions. This is temp
            int rowSize = (int)Mathf.Sqrt(AvailableInstances);

            for (int i = 0; i < AvailableInstances; i++)
            {
                int x = i % rowSize;
                int y = i / rowSize;

                _positions[i] = new float3(x * gridOffset, 0, -y * gridOffset);
                _rotations[i] = Quaternion.identity;
                _scale[i] = new float3(1, 1, 1);
            }
        }

    }

}