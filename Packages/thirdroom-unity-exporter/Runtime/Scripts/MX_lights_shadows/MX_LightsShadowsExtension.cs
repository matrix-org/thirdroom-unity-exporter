#if UNITY_EDITOR

using System;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF;

namespace ThirdRoom.Exporter
{
  public class ShadowsExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      Light light = transform.GetComponent<Light>();
      bool castShadow = light != null &&
        light.shadows != LightShadows.None &&
        light.lightmapBakeType != LightmapBakeType.Baked;

      MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();

      bool receiveShadow = false;

      if (meshRenderer != null) {
        receiveShadow = meshRenderer.receiveShadows;
        castShadow = castShadow || meshRenderer.shadowCastingMode != ShadowCastingMode.Off;
      }

      if (castShadow || receiveShadow) {
        node.AddExtension(MX_LightsShadows.ExtensionName, new MX_LightsShadows() {
          castShadow = castShadow,
          receiveShadow = receiveShadow
        });
      }
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      exporter.DeclareExtensionUsage(MX_LightsShadows.ExtensionName, false);
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_LightsShadows : IExtension
  {
    public const string ExtensionName = "MX_lights_shadows";

    public bool castShadow;

    public bool receiveShadow;

    public JProperty Serialize()
    {
      var jo = new JObject();

      if (castShadow) {
        jo.Add(new JProperty(nameof(castShadow), castShadow));
      }

      if (receiveShadow) {
        jo.Add(new JProperty(nameof(receiveShadow), receiveShadow));
      }

      return new JProperty(ExtensionName, jo);
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_LightsShadows();
    }
  }
}

#endif