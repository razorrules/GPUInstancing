using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class InstanceMesh
{
    public float renderDistance;
    public Mesh mesh;
    public int submeshIndex = 1;
    public Vector3 scale = Vector3.one;

    //Values for render params
    [Header("Render Params")]
    public int layer;
    public Material material;
    public ShadowCastingMode shadowCastingMode;
    public bool receiveShadows;


}