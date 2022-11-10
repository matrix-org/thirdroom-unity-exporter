#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

namespace ThirdRoom.Exporter
{
  public class OMILinkExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      var link = transform.gameObject.GetComponent<OMILinkBehaviour>();

      if (link != null) {
        node.AddExtension(OMI_Link.ExtensionName, new OMI_Link() { uri = link.uri });
        exporter.DeclareExtensionUsage(OMI_Link.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class OMI_Link : IExtension
  {
    public const string ExtensionName = "OMI_link";

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
      return new OMI_Link() { uri = uri };
    }
  }
}

#endif