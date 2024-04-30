using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Laio.GPUInstancing.Samples.VegetationPainter
{

    /// <summary>
    /// Handles passing points to the instance manager, along with detecting which points need to be removed.
    /// </summary>
    public class VegetationPainter : MonoBehaviour
    {
        public PoolInstanceManager manager;
        [Space()]
        public Vector3 defaultSize;
        public float sizeVariation;

        private NativeArray<PoolInstanceData> _data;

        private void Start()
        {
            Copy();
        }

        /// <summary>
        /// Copy the data from the manager into our native array
        /// </summary>
        private void Copy()
        {
            manager.CopyData(out _data);
        }

        /// <summary>
        /// Remove all points at a given point based on radius
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        public void RemovePoints(Vector3 origin, float radius)
        {
            //Create a list, and loop through all points to find which ones need to be removed.
            //Here, we only use ints for indexes instead of actually using PoolInstanceData
            List<int> changes = new List<int>();

            for (int i = 0; i < _data.Length; i++)
            {
                //If it is not rendered, skip the distance calc
                if (!_data[i].doRender)
                    continue;
                if (Vector3.Distance(_data[i].position, origin) < radius)
                    changes.Add(_data[i].index);
            }

            //Remove points and copy array back over to ensure we are up to date
            manager.RemovePoints(changes.ToArray());
            Copy();
        }

        /// <summary>
        /// Add all points to the instance spawner
        /// </summary>
        /// <param name="point"></param>
        public void AddPoints(List<Vector3> point)
        {
            if (point.Count == 0)
                return;

            //Setup our array to pass
            PoolInstanceData[] changes = new PoolInstanceData[point.Count];
            for (int i = 0; i < changes.Length; i++)
            {
                //Set basic data and the position
                changes[i] = new PoolInstanceData
                {
                    doRender = true,
                    position = point[i],
                    rotation = Quaternion.Euler(-90, UnityEngine.Random.Range(0, 360), 0),
                    scale = defaultSize + (Vector3.one * UnityEngine.Random.Range(0, sizeVariation)),
                    index = -1
                };
            }

            //Add points and copy array back over to ensure we are up to date
            manager.AddPoints(changes);
            Copy();
        }


    }
}