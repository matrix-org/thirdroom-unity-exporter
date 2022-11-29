#if UNITY_EDITOR

using System;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdRoom.Exporter
{

  public class PostprocessingExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      
      var volumes = FindObjectsOfType<Volume>();

      Volume globalVolume = null;

      foreach (var volume in volumes) {
        if (volume.isGlobal) {
          globalVolume = volume;
        }
      }

      if (globalVolume != null && globalVolume.isActiveAndEnabled) {
        var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];
        var postprocessing = new MX_Postprocessing();

        var profile = globalVolume.profile;
        
        Bloom bloom;

        if (profile.TryGet<Bloom>(out bloom)) {
          var bloomEffect = new MX_BloomEffect();
          bloomEffect.strength = bloom.intensity.value / 10.0f;
          postprocessing.bloom = bloomEffect;
        }

        scene.AddExtension(MX_Postprocessing.ExtensionName, postprocessing);
        exporter.DeclareExtensionUsage(MX_Postprocessing.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{

  [Serializable]
  public class MX_BloomEffect : GLTFChildOfRootProperty {

    public float strength;

    public virtual JObject Serialize() {
      var jo = new JObject();

      jo.Add(nameof(strength), strength);

      return jo;
    }
  }

  // TODO: Should we have both a node and material extension property? That way the objects just specify offsets?
  [Serializable]
  public class MX_Postprocessing : IExtension
  {
    public const string ExtensionName = "MX_postprocessing";

    public MX_BloomEffect bloom;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      if (bloom != null) {
        jo.Add(nameof(bloom),  bloom.Serialize());
      }

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_Postprocessing() {
        bloom = bloom
      };
    }
  }
}

#endif