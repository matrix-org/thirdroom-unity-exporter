using UnityEngine;

public class CubemapUtils
{
    public static RenderTexture ConvertToEquirectangular(Cubemap cubemap) {
        var destRenderTexture = RenderTexture.GetTemporary(
            cubemap.width * 2, cubemap.height, 0, RenderTextureFormat.ARGB32);
        var material = new Material(Shader.Find("Hidden/CubemapToEquirectangular"));
        material.SetMatrix("_CubemapRotation", Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0)));
        Graphics.Blit(cubemap, destRenderTexture, material);
		return destRenderTexture;
    }
}
