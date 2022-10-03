#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

public class SpawnPointExtensionConfig : ScriptableObject
{

  [InitializeOnLoadMethod]
  static void InitExt()
  {
    GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
  }

  private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
  {
    if (transform.gameObject.GetComponent<SpawnPointBehaviour>() != null) {
      node.AddExtension(MX_SpawnPoint.ExtensionName, new MX_SpawnPoint());
      exporter.DeclareExtensionUsage(MX_SpawnPoint.ExtensionName, false);
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_SpawnPoint : IExtension
  {
    public const string ExtensionName = "MX_spawn_point";

    public JProperty Serialize()
    {
      JProperty jProperty = new JProperty(ExtensionName, new JObject());
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_SpawnPoint();
    }
  }
}

#endif