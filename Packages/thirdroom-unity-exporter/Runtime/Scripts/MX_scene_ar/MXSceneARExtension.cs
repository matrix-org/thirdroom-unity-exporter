#if UNITY_EDITOR

using System;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

namespace ThirdRoom.Exporter
{

  public class SceneARExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      var sceneAR = FindObjectOfType<ARSceneBehaviour>();

      if (sceneAR != null) {
        var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];
        scene.AddExtension(MX_SceneAR.ExtensionName, new MX_SceneAR());
        exporter.DeclareExtensionUsage(MX_SceneAR.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_SceneAR : IExtension
  {
    public const string ExtensionName = "MX_scene_ar";

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_SceneAR();
    }
  }
}

#endif