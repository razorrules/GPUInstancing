using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancing
{

    [System.Serializable]
    public class InstanceMesh
    {
        public float renderDistance;
        public Mesh mesh;
        public Material material;
        public int submeshIndex = 1;
        public int layer;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;

    }

}