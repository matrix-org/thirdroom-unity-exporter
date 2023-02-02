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
using UnityEngine.Rendering.Universal;

namespace ThirdRoom.Exporter
{
  public class ReflectionProbesExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.BeforeSceneExport += OnBeforeSceneExport;
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    public static void OnBeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {
      if (QualitySettings.activeColorSpace != ColorSpace.Linear) {
        throw new Exception("Please set your project's color space to linear.");
      }

      var renderPipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

      if (!renderPipeline.supportsHDR) {
        throw new Exception("Please ensure that HDR is enabled in your render pipeline");
      }
    }

    private static GLTFSceneExporter.TextureExportSettings textureExportSettings = new GLTFSceneExporter.TextureExportSettings {
      conversion = GLTFSceneExporter.TextureExportSettings.Conversion.None,
      linear = true,
      alphaMode = GLTFSceneExporter.TextureExportSettings.AlphaMode.Always,
      isValid = true,
    };

    private static List<MX_ReflectionProbe> reflectionProbes = new List<MX_ReflectionProbe>();

    private static MX_ReflectionProbe CreateReflectionProbe(GLTFSceneExporter exporter, GLTFRoot gltfRoot, string name, Cubemap cubemap, float hdrRange, GLTF.Math.Vector3? size) {
      if (cubemap == null) {
        Debug.LogWarningFormat("Missing cubemap for node {0}", name);
        return null;
      }

      var texture = CubemapUtils.ConvertToEquirectangular(cubemap as Cubemap);

      bool useRGBM = true;

      if (Mathf.Approximately(hdrRange, 1.0f)) {
        useRGBM = false;
      } else if (!Mathf.Approximately(hdrRange, 34.49324f)) {
        Debug.LogWarningFormat("Unexpected HDR settings for reflection probe on \"{0}\". Ensure that the HDR Cubemap Encoding setting is set to normal.", name);
        return null;
      }
      
      var reflectionProbe = new MX_ReflectionProbe() {
        reflectionProbeTexture = exporter.ExportTextureInfo(
          texture,
          GLTFSceneExporter.TextureMapType.Linear,
          textureExportSettings
        )
      };

      if (size.HasValue) {
        reflectionProbe.size = size.Value;
      }

      if (useRGBM) {

        var reflectionProbeTextureInfo = reflectionProbe.reflectionProbeTexture;
        var reflectionProbeTexture = gltfRoot.Textures[reflectionProbeTextureInfo.Index.Id];
        
        if (
          reflectionProbeTexture.Extensions == null ||
          !reflectionProbeTexture.Extensions.ContainsKey(MX_TextureRGBM.ExtensionName)
        ) {
          reflectionProbeTexture.AddExtension(MX_TextureRGBM.ExtensionName, new MX_TextureRGBM());
          exporter.DeclareExtensionUsage(MX_TextureRGBM.ExtensionName, false);
        }
      }

      return reflectionProbe;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      ReflectionProbe unityReflectionProbe = transform.GetComponent<ReflectionProbe>();

      if (unityReflectionProbe == null || !unityReflectionProbe.enabled) {
        return;
      }

      var renderPipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

      if (!unityReflectionProbe.hdr) {
        Debug.LogWarningFormat("Please enable the HDR setting on reflection probe \"{0}\".", transform.name);
        return;
      }

      var cubemap = unityReflectionProbe.customBakedTexture == null ?
            unityReflectionProbe.bakedTexture : unityReflectionProbe.customBakedTexture;

      var size = unityReflectionProbe.size.ToGltfVector3Raw();
      var hdrRange = unityReflectionProbe.textureHDRDecodeValues[0];

      var reflectionProbe = CreateReflectionProbe(
        exporter, gltfRoot, transform.name, cubemap as Cubemap, hdrRange, size);

      if (reflectionProbe == null) {
        return;
      }

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

        var cubemap = ReflectionProbe.defaultTexture as Cubemap;
        var hdrRange = ReflectionProbe.defaultTextureHDRDecodeValues[0];

        var reflectionProbe = CreateReflectionProbe(
          exporter, gltfRoot, "Scene", cubemap, hdrRange, null);

        if (reflectionProbe == null) {
          return;
        }

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