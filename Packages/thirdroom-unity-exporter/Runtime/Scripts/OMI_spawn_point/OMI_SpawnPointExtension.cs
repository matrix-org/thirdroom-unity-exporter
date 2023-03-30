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
  public class SpawnPointExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      var spawnPoint = transform.gameObject.GetComponent<SpawnPointBehaviour>();
      if (spawnPoint != null) {
        node.AddExtension(OMI_SpawnPoint.ExtensionName, new OMI_SpawnPoint() { title = spawnPoint.title, team = spawnPoint.team, group = spawnPoint.group });
        exporter.DeclareExtensionUsage(OMI_SpawnPoint.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class OMI_SpawnPoint : IExtension
  {
    public const string ExtensionName = "OMI_spawn_point";

    public string title;
    public string team;
    public string group;

    public JProperty Serialize()
    {
      JObject jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);
      jo.Add(nameof(title), title);
      jo.Add(nameof(team), team);
      jo.Add(nameof(group), group);

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new OMI_SpawnPoint() { title = title, team = team, group = group };
    }
  }
}

#endif