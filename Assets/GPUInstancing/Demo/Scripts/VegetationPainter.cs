using GPUInstancing;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class VegetationPainter : MonoBehaviour
{

    public Vector3 treeSize;
    public float treeSizeVariation;

    public NativeArray<PoolInstanceData> data;

    public PoolInstanceManager manager;

    private void Start()
    {
        Copy();
    }


    private void Copy()
    {
        manager.CopyData(out data);
    }

    public void RemovePoints(Vector3 origin, float brushSize)
    {
        List<int> changes = new List<int>();

        for (int i = 0; i < data.Length; i++)
        {
            if (!data[i].doRender)
                continue;
            if (Vector3.Distance(data[i].position, origin) < brushSize)
                changes.Add(data[i].index);
        }

        manager.RemovePoints(changes.ToArray());
        manager.CopyData(out data);
    }

    public void AddPoints(List<Vector3> point)
    {
        PoolInstanceData[] changes = new PoolInstanceData[point.Count];
        for (int i = 0; i < changes.Length; i++)
        {
            changes[i] = new PoolInstanceData
            {
                doRender = true,
                position = point[i],
                rotation = Quaternion.Euler(-90, UnityEngine.Random.Range(0, 360), 0),
                scale = treeSize + (Vector3.one * UnityEngine.Random.Range(0, treeSizeVariation)),
                index = -1
            };
        }
        manager.AddPoints(changes);
        manager.CopyData(out data);
    }


}
