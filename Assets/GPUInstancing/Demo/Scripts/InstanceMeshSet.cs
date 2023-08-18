using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancing
{
    [CreateAssetMenu(fileName = "InstanceSet", menuName = "Laio/Instance Set")]
    public class InstanceMeshSet : ScriptableObject
    {
        [SerializeField] private InstanceMesh[] _meshes;

        public InstanceMesh[] Meshes { get => _meshes; }
    }

}