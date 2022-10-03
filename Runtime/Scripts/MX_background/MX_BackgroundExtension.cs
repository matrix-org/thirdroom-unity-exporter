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

  public class BackgroundExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      if (RenderSettings.skybox != null) {
        var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];

        

        var go = new GameObject("SkyboxCamera");
        var camera = go.AddComponent<Camera>();
        camera.cullingMask = 0;
        var skybox = new Cubemap(1024, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, 0);
        camera.RenderToCubemap(skybox);

        var background = new MX_Background() {
          backgroundTexture = exporter.ExportTextureInfo(
            skybox,
            GLTFSceneExporter.TextureMapType.CubeMap
          ),
        };

        DestroyImmediate(go);

        scene.AddExtension(MX_Background.ExtensionName, background);
        exporter.DeclareExtensionUsage(MX_Background.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_Background : IExtension
  {
    public const string ExtensionName = "MX_background";

    public TextureInfo backgroundTexture;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

       jo.Add(nameof(backgroundTexture),
        new JObject(
          new JProperty(TextureInfo.INDEX, backgroundTexture.Index.Id),
          new JProperty(TextureInfo.TEXCOORD, backgroundTexture.TexCoord)
        )
      );

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_Background() { backgroundTexture = backgroundTexture };
    }
  }
}

#endif