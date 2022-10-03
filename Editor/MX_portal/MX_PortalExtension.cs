#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

public class MXPortalExtensionConfig : ScriptableObject
{

  [InitializeOnLoadMethod]
  static void InitExt()
  {
    GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
  }

  private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
  {
    var portal = transform.gameObject.GetComponent<MXPortalBehaviour>();

    if (portal != null) {
      node.AddExtension(MX_Portal.ExtensionName, new MX_Portal() { uri = portal.uri });
      exporter.DeclareExtensionUsage(MX_Portal.ExtensionName, false);
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_Portal : IExtension
  {
    public const string ExtensionName = "MX_portal";

    public string uri;

    public JProperty Serialize()
    {
      JObject jo = new JObject();

      JProperty jProperty = new JProperty(ExtensionName, jo);

      jo.Add(nameof(uri), uri);

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_Portal() { uri = uri };
    }
  }
}

#endif