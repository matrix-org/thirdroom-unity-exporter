#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Vector3 = GLTF.Math.Vector3;
using UnityGLTF.Extensions;

namespace ThirdRoom.Exporter
{
  public class ReflectionProbesExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static List<MX_ReflectionProbe> reflectionProbes = new List<MX_ReflectionProbe>();

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      ReflectionProbe unityReflectionProbe = transform.GetComponent<ReflectionProbe>();

      if (unityReflectionProbe == null || !unityReflectionProbe.enabled) {
        return;
      }

      var reflectionProbe = new MX_ReflectionProbe() {
        size = unityReflectionProbe.size.ToGltfVector3Raw(),
        reflectionProbeTexture = exporter.ExportTextureInfo(
          unityReflectionProbe.customBakedTexture == null ?
            unityReflectionProbe.bakedTexture : unityReflectionProbe.customBakedTexture,
          GLTFSceneExporter.TextureMapType.CubeMap
        )
      };

      var nodeReflectionProbe = new MX_ReflectionProbeRef() {
        reflectionProbe = new ReflectionProbeId() {
          Id = reflectionProbes.Count,
          Root = gltfRoot
        }
      };

      node.AddExtension(MX_ReflectionProbes.ExtensionName, nodeReflectionProbe);
      reflectionProbes.Add(reflectionProbe);
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      if (ReflectionProbe.defaultTexture != null) {
        var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];

        var reflectionProbe = new MX_ReflectionProbe() {
          reflectionProbeTexture = exporter.ExportTextureInfo(
            ReflectionProbe.defaultTexture,
            GLTFSceneExporter.TextureMapType.CubeMap
          ),
        };

        var sceneReflectionProbe = new MX_ReflectionProbeRef() {
          reflectionProbe = new ReflectionProbeId() {
            Id = reflectionProbes.Count,
            Root = gltfRoot
          },
        };

        scene.AddExtension(MX_ReflectionProbes.ExtensionName, sceneReflectionProbe);
        exporter.DeclareExtensionUsage(MX_ReflectionProbes.ExtensionName, false);
        reflectionProbes.Add(reflectionProbe);
      }

      if (reflectionProbes.Count > 0) {
        var extension = new MX_ReflectionProbes() {
          reflectionProbes = new List<MX_ReflectionProbe>(reflectionProbes)
        };
        gltfRoot.AddExtension(MX_ReflectionProbes.ExtensionName, extension);
        exporter.DeclareExtensionUsage(MX_ReflectionProbes.ExtensionName, false);
        reflectionProbes.Clear();
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class ReflectionProbeId : GLTFId<MX_ReflectionProbe> {
    public ReflectionProbeId()
		{
		}

		public ReflectionProbeId(ReflectionProbeId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override MX_ReflectionProbe Value
		{
			get
			{
				if (Root.Extensions.TryGetValue(MX_ReflectionProbes.ExtensionName, out IExtension iextension))
				{
					MX_ReflectionProbes extension = iextension as MX_ReflectionProbes;
					return extension.reflectionProbes[Id];
				}
				else
				{
					throw new Exception("MX_reflection_probes not found on root object");
				}
			}
		}
  }

  [Serializable]
  public class MX_ReflectionProbeRef : IExtension {
    public ReflectionProbeId reflectionProbe;

     public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(MX_ReflectionProbes.ExtensionName, jo);      
      jo.Add(new JProperty(nameof(reflectionProbe), reflectionProbe.Id));
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_ReflectionProbeRef() { reflectionProbe = reflectionProbe };
    }
  }

  [Serializable]
  public class MX_ReflectionProbe : GLTFChildOfRootProperty {

    public Vector3 size;

    public TextureInfo reflectionProbeTexture;

    public JObject Serialize() {
      var jo = new JObject();

      if (size != Vector3.Zero) {
        jo.Add(nameof(size), new JArray(size.X, size.Y, size.Z));
      }

      jo.Add(nameof(reflectionProbeTexture),
        new JObject(
          new JProperty(TextureInfo.INDEX, reflectionProbeTexture.Index.Id),
          new JProperty(TextureInfo.TEXCOORD, reflectionProbeTexture.TexCoord)
        )
      );

      return jo;
    }
  }

  [Serializable]
  public class MX_ReflectionProbes : IExtension
  {
    public const string ExtensionName = "MX_reflection_probes";

    public List<MX_ReflectionProbe> reflectionProbes;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      JArray arr = new JArray();

      foreach (var probe in reflectionProbes) {
        arr.Add(probe.Serialize());
      }

      jo.Add(new JProperty(nameof(reflectionProbes), arr));

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_ReflectionProbes() { reflectionProbes = reflectionProbes };
    }
  }
}

#endif