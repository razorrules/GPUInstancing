using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laio.GPUInstancing
{
    [CreateAssetMenu(fileName = "InstanceMeshSet", menuName = "Laio/Instance Mesh Set")]
    public class InstanceMeshSet : ScriptableObject
    {
        [SerializeField] private InstanceMesh[] _meshes;

        public InstanceMesh[] Meshes { get => _meshes; }
    }

}