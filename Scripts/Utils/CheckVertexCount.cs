using UnityEngine;

public class CheckVertexCount : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            int vertexCount = mf.sharedMesh.vertexCount;
            Debug.Log("Vertex Count: " + vertexCount);
        }

        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        if (smr != null && smr.sharedMesh != null)
        {
            int vertexCount = smr.sharedMesh.vertexCount;
            Debug.Log("Skinned mesh vertex count: " + vertexCount);
        }
    }
}
