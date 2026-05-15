using UnityEngine;

[ExecuteInEditMode]
public class PostProcessHeightFog : MonoBehaviour
{
    new Camera camera;
    public Material material;
    public float fogHeight = 0;
    public float fogDensity = 1;

    private void Start()
    {
        camera = GetComponent<Camera>();
        camera.depthTextureMode = camera.depthTextureMode | DepthTextureMode.Depth;
        SetFogProperties();
    }

    private void OnValidate()
    {
        SetFogProperties();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Matrix4x4 v = camera.worldToCameraMatrix;
        Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

        Matrix4x4 invVP = Matrix4x4.Inverse(p * v);
        material.SetMatrix("_Matrix_Inv_VP", invVP);
        Graphics.Blit(source, destination, material);
    }

    public void SetFogProperties()
    {
        material.SetFloat("_FogDensity", fogDensity);
        material.SetFloat("_FogHeight", fogHeight);
    }
}