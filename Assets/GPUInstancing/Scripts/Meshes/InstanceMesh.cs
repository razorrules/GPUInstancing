using UnityEngine;
using UnityEngine.Rendering;

namespace Laio.GPUInstancing
{

    /// <summary>
    /// Individual mesh for GPU instancing.
    /// </summary>
    [System.Serializable]
    public class InstanceMesh
    {
        public Mesh mesh;
        public Material material;
        public int submeshIndex = 1;
        public int layer;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;

    }

}