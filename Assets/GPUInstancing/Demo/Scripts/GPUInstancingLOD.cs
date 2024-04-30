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

        protected override void Allocate(bool markSetup)
        {
            base.Allocate(true);
            GridLayout();
        }

        private void GridLayout()
        {
            //Next, we will set the grid of positions.
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

                _positions[i] = new float3(x * gridOffset, 0, -y * gridOffset);
                _rotations[i] = Quaternion.identity;
                _scale[i] = new float3(1, 1, 1);

                y++;
            }
        }
    }

}