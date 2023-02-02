#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Vector2 = GLTF.Math.Vector2;

namespace ThirdRoom.Exporter
{

  public class LightmapExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.BeforeSceneExport += OnBeforeSceneExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
    }

    public static void OnBeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {
      if (LightmapSettings.lightmapsMode != LightmapsMode.NonDirectional) {
        throw new Exception("Please set your lightmap directional mode to Non Directional");
      }

      if (Lightmapping.lightingSettings.mixedBakeMode != MixedLightingMode.IndirectOnly) {
        throw new Exception("Please set your lighting mode to Baked Indirect.");
      }
    }

    private static bool beforeSceneHookCalled = false;

    private static GLTFSceneExporter.TextureExportSettings textureExportSettings = new GLTFSceneExporter.TextureExportSettings {
      conversion = GLTFSceneExporter.TextureExportSettings.Conversion.None,
      linear = true,
      alphaMode = GLTFSceneExporter.TextureExportSettings.AlphaMode.Always,
      isValid = true,
    };

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      if (!beforeSceneHookCalled)
      {
        var lights = UnityEngine.Object.FindObjectsOfType<Light>();

        foreach (var light in lights)
        {
          if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
          {
            light.enabled = false;
            bakedLights.Add(light);
          }
        }

        beforeSceneHookCalled = true;
      }

      var meshRenderer = transform.gameObject.GetComponent<MeshRenderer>();

      if (meshRenderer == null || meshRenderer.lightmapIndex == -1 || meshRenderer.lightmapIndex >= 65533)
      {
        return;
      }

      var lightmap = LightmapSettings.lightmaps[meshRenderer.lightmapIndex];

      if (lightmap == null || lightmap.lightmapColor == null)
      {
        return;
      }

      var lightMapTexture = exporter.ExportTextureInfo(
        lightmap.lightmapColor,
        GLTFSceneExporter.TextureMapType.Linear,
        textureExportSettings
      );

      var texture = gltfRoot.Textures[lightMapTexture.Index.Id];

      if (texture.Extensions == null || !texture.Extensions.ContainsKey(MX_TextureRGBM.ExtensionName)) {
        texture.AddExtension(MX_TextureRGBM.ExtensionName, new MX_TextureRGBM());
        exporter.DeclareExtensionUsage(MX_TextureRGBM.ExtensionName, false);
      }

      var scale = new Vector2(
          meshRenderer.lightmapScaleOffset.x,
          meshRenderer.lightmapScaleOffset.y
      );

      var offset = new Vector2(
          meshRenderer.lightmapScaleOffset.z,
          1 - meshRenderer.lightmapScaleOffset.y - meshRenderer.lightmapScaleOffset.w
      );

      var extension = new MX_lightmap()
      {
        lightMapTexture = lightMapTexture,
        scale = scale,
        offset = offset,
      };

      node.AddExtension(MX_lightmap.ExtensionName, extension);
      exporter.DeclareExtensionUsage(MX_lightmap.ExtensionName, false);
    }

    private static List<Light> bakedLights = new List<Light>();

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      foreach (var light in bakedLights)
      {
        light.enabled = true;
      }

      bakedLights.Clear();

      beforeSceneHookCalled = false;
    }
  }
}

namespace GLTF.Schema
{

  // TODO: Should we have both a node and material extension property? That way the objects just specify offsets?
  [Serializable]
  public class MX_lightmap : IExtension
  {
    public const string ExtensionName = "MX_lightmap";

    // The index of the lightmap texture to use for this object
    public TextureInfo lightMapTexture;

    // The node's UV scale in the lightmap
    // Defaults to 1,1
    // Note if the object has the EXT_gpu_instancing extension, the _LIGHTMAP_SCALE accessor takes precedent
    public Vector2 scale;

    // The node's UV offset in the lightmap
    // Note if the object has the EXT_gpu_instancing extension, the _LIGHTMAP_OFFSET accessor takes precedent
    // Defaults to 0,0
    public Vector2 offset;

    // Hardcoded to 1.0f to maintain partial compatibility with MOZ_lightmap
    public float intensity = 1.0f;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      jo.Add(nameof(scale), new JArray(scale.X, scale.Y));
      jo.Add(nameof(offset), new JArray(offset.X, offset.Y));
      jo.Add(nameof(lightMapTexture),
        new JObject(
          new JProperty(TextureInfo.INDEX, lightMapTexture.Index.Id),
          new JProperty(TextureInfo.TEXCOORD, lightMapTexture.TexCoord)
        )
      );
      jo.Add(nameof(intensity), intensity);

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_lightmap() {
        lightMapTexture = lightMapTexture,
        offset = offset,
        scale = scale,
        intensity = intensity
      };
    }
  }
}

#endif