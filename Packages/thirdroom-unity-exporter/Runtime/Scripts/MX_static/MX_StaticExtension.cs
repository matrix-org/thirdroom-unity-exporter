#if UNITY_EDITOR

using System;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

namespace ThirdRoom.Exporter
{
  public class StaticExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      if (transform.gameObject.isStatic) {
        node.AddExtension(MX_Static.ExtensionName, new MX_Static());
      }
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      exporter.DeclareExtensionUsage(MX_Static.ExtensionName, false);
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_Static : IExtension
  {
    public const string ExtensionName = "MX_static";

    public JProperty Serialize()
    {
      JProperty jProperty = new JProperty(ExtensionName, new JObject());
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_Static();
    }
  }
}

#endif