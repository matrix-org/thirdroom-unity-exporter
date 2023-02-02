#if UNITY_EDITOR

using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ThirdRoom.Exporter
{

  public class TextureRGBMExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
    }

  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class MX_TextureRGBM : IExtension
  {
    public const string ExtensionName = "MX_texture_rgbm";

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new MX_TextureRGBM();
    }
  }
}

#endif