#if UNITY_EDITOR

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

public struct ComponentPropDefinition {
  public Type propType;
  public object defaultValue;
}

namespace ThirdRoom.Exporter {

  public class ThirdRoomComponentExporterConfig : ScriptableObject {

    [InitializeOnLoadMethod]
    static void InitExt() {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static HashSet<Type> definedComponents = new HashSet<Type>();

    private static void OnAfterNodeExport(
      GLTFSceneExporter exporter,
      GLTFRoot gltfRoot,
      Transform transform,
      Node node
    ) {
      var monoBehaviours = new List<MonoBehaviour>();
      transform.gameObject.GetComponents(monoBehaviours);
      var thirdroomComponents = monoBehaviours.Where(behaviour => behaviour is ThirdroomComponent);

      if (thirdroomComponents.Count() > 0) {
        var components = new Dictionary<string, Dictionary<string, object>>();

        foreach (var component in thirdroomComponents) {
          definedComponents.Add(component.GetType());

          var props = component.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => IsFieldVisibleInInspector(prop));

          var componentProps = new Dictionary<string, object>();

          foreach (var prop in props) {
            var value = prop.GetValue(component);

            if (value is Transform) {
              if (value.Equals(null)) {
                componentProps.Add(prop.Name, null);
              } else {
                componentProps.Add(prop.Name, exporter.ExportNode(((Transform) value).gameObject));
              }
            } else {
              componentProps.Add(prop.Name, value);
            }
          }

          components.Add(component.GetType().Name, componentProps);
        }

        node.AddExtension(MXComponentDefinitions.ExtensionName, new MXNodeComponents() { components = components });
      }

      var additionalComponents = transform.gameObject.GetComponent<ThirdroomAdditionalComponents>();

      if (additionalComponents != null) {
        foreach (var component in additionalComponents.components) {
          definedComponents.Add(component.Type);
        }
      }
    }

    private static Dictionary<string, string> PropTypeMap = new Dictionary<string, string>() {
      { "Boolean", "bool"},
      { "Int32", "i32"},
      { "Single", "f32" },
      { "Vector2", "vec2" },
      { "Vector3", "vec3" },
      { "Vector4", "vec4" },
      { "Transform", "ref" },
    };

    private static Dictionary<string, string> PropStorageTypeMap = new Dictionary<string, string>() {
      { "Boolean", "i32"},
      { "Int32", "i32"},
      { "Single", "f32" },
      { "Vector2", "f32" },
      { "Vector3", "f32" },
      { "Vector4", "f32" },
      { "Transform", "u32" },
    };

    private static Dictionary<string, int> PropSizeMap = new Dictionary<string, int>() {
      { "Boolean", 1},
      { "Int32", 1},
      { "Single", 1 },
      { "Vector2", 2 },
      { "Vector3", 3 },
      { "Vector4", 4 },
      { "Transform", 1 },
    };

    private static Dictionary<string, string> RefTypeMap = new Dictionary<string, string>() {
      { "Transform", "node" },
    };

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {
      if (definedComponents.Count > 0) {
        var definitions = new MXComponentDefinitions();

        foreach (var componentType in definedComponents) {
          var definition = new MXComponentDefinition();

          var props = componentType
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => IsFieldVisibleInInspector(prop));

          var defaultObj = Activator.CreateInstance(componentType);

          definition.name = componentType.Name;

          foreach (var prop in props) {
            var propDef = new MXComponentPropertyDefinition();

            propDef.name = prop.Name;

            var propType = prop.FieldType;
            string type = PropTypeMap[propType.Name];

            if (type == null) {
              Debug.LogError($"Unsupported type {propType.Name} for component {componentType.Name}");
              continue;
            }

            propDef.type = type;
            propDef.storageType = PropStorageTypeMap[propType.Name];
            propDef.size = PropSizeMap[propType.Name];

            if (type == "ref") {
              propDef.refType = RefTypeMap[propType.Name];

              if (propDef.refType == "node") {
                propDef.defaultValue = prop.GetValue(defaultObj) as NodeId;
              }
            } else {
              propDef.defaultValue = prop.GetValue(defaultObj);
            }

            definition.props.Add(propDef);
          }

          definitions.definitions.Add(definition);
        }

        gltfRoot.AddExtension(MXComponentDefinitions.ExtensionName, definitions);
        exporter.DeclareExtensionUsage(MXComponentDefinitions.ExtensionName, false);

        definedComponents.Clear();
      }
    }

    private static bool IsFieldVisibleInInspector(FieldInfo field) {
      if (field.IsDefined(typeof(HideInInspector), false)) return false;

      if (field.IsDefined(typeof(SerializeField), false)) return true;

      // Exclude fields from the base MonoBehaviour class.
      if (typeof(MonoBehaviour).GetField(field.Name) != null) return false;

      return field.IsPublic && !field.IsDefined(typeof(ObsoleteAttribute), false);
    }
  }
}

namespace GLTF.Schema {

  [Serializable]
  public class MXNodeComponents : IExtension {
    public Dictionary<string, Dictionary<string, object>> components;

    public JProperty Serialize() {
      JObject jo = new JObject();

      foreach (var entry in components) {
        JObject component = new JObject();

        foreach (var prop in entry.Value) {
          var propValue = prop.Value;

          if (propValue is string) {
            component.Add(prop.Key, (string) propValue);
          } else if (propValue is float) {
            component.Add(prop.Key, (float) propValue);
          } else if (propValue is int) {
            component.Add(prop.Key, (int) propValue);
          } else if (propValue is Vector2) {
            var vec2 = (Vector2) propValue;
            component.Add(prop.Key, new JArray(vec2.x, vec2.y));
          } else if (propValue is Vector3) {
            var vec3 = (Vector3) propValue;
            component.Add(prop.Key, new JArray(vec3.x, vec3.y, vec3.z));
          } else if (propValue is Vector4) {
            var vec4 = (Vector4) propValue;
            component.Add(prop.Key, new JArray(vec4.x, vec4.y, vec4.z, vec4.w));
          } else if (propValue is NodeId) {
            component.Add(prop.Key, ((NodeId) propValue).Id);
          } else if (propValue is bool) {
            component.Add(prop.Key, (bool) propValue);
          } else if (propValue == null) {
            component.Add(prop.Key, null);
          } else {
            Debug.LogWarning("Unsupported ThirdroomComponent prop type: " + propValue.GetType());
          }
        }

        jo.Add(entry.Key, component);
      }

      return new JProperty(MXComponentDefinitions.ExtensionName, jo);
    }

    public IExtension Clone(GLTFRoot root) {
      return new MXNodeComponents() { components = components };
    }
  }

  [Serializable]
  public class MXComponentPropertyDefinition : GLTFChildOfRootProperty {

    public string name;
    public string type;
    public string refType;
    public string storageType;
    public int size;
    public object defaultValue;

    public JObject Serialize() {
      JObject def = new JObject();

      def.Add(nameof(name), name);
      def.Add(nameof(type), type);

      if (refType != null) {
        def.Add(nameof(refType), refType);
      }

      def.Add(nameof(storageType), storageType);
      def.Add(nameof(size), size);

      if (defaultValue is float) {
        def.Add("default", (float) defaultValue);
      } else if (defaultValue is int) {
        def.Add("default", (int) defaultValue);
      } else if (defaultValue is Vector2) {
        var vec2 = (Vector2) defaultValue;
        def.Add("default", new JArray(vec2.x, vec2.y));
      } else if (defaultValue is Vector3) {
        var vec3 = (Vector3) defaultValue;
        def.Add("default", new JArray(vec3.x, vec3.y, vec3.z));
      } else if (defaultValue is Vector4) {
        var vec4 = (Vector4) defaultValue;
        def.Add("default", new JArray(vec4.x, vec4.y, vec4.z, vec4.w));
      } else if (defaultValue is NodeId) {
        def.Add("default", ((NodeId) defaultValue).Id);
      } else if (defaultValue is bool) {
        def.Add("default", (bool) defaultValue);
      }

      return def;
    }
  }

  [Serializable]
  public class MXComponentDefinition : GLTFChildOfRootProperty {

    public string name;

    public List<MXComponentPropertyDefinition> props = new List<MXComponentPropertyDefinition>();

    public JObject Serialize() {
      var jo = new JObject();

      jo.Add(nameof(name), name);

      if (props.Count > 0) {
        var propsArray = new JArray();

        foreach (var prop in props) {
          propsArray.Add(prop.Serialize());
        }

        jo.Add(nameof(props), propsArray);
      }

      return jo;
    }
  }

  [Serializable]
  public class MXComponentDefinitions : IExtension {
    public const string ExtensionName = "MX_components";

    public List<MXComponentDefinition> definitions = new List<MXComponentDefinition>();

    public JProperty Serialize() {
      JObject jo = new JObject();

      JArray arr = new JArray();

      foreach(var definition in definitions) {
        arr.Add(definition.Serialize());
      }

      jo.Add(nameof(definitions), arr);

      return new JProperty(ExtensionName, jo);
    }

    public IExtension Clone(GLTFRoot root) {
      return new MXComponentDefinitions() { definitions = definitions };
    }
  }
}

#endif