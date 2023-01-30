#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Vector3 = GLTF.Math.Vector3;
using UnityGLTF.Extensions;

namespace ThirdRoom.Exporter
{

  public class ColliderExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
    }

    private static List<GLTFCollider> colliders = new List<GLTFCollider>();

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      Collider unityCollider = transform.GetComponent<Collider>();

      if (unityCollider == null || !unityCollider.enabled) {
        return;
      }

      var nodeCollider = new OMI_colliderNode() {
          collider = new ColliderId() {
            Id = colliders.Count,
            Root = gltfRoot
          }
        };

      GLTFCollider collider;
      UnityEngine.Vector3 center = UnityEngine.Vector3.zero;

      if (unityCollider.GetType() == typeof(BoxCollider)) {
        var boxCollider = unityCollider as BoxCollider;
        collider = new GLTFCollider() {
          type = ColliderType.box,
          extents = (boxCollider.size / 2).ToGltfVector3Raw(),
        };
        center = boxCollider.center;
      } else if (unityCollider.GetType() == typeof(SphereCollider)) {
        var sphereCollider = unityCollider as SphereCollider;
        collider = new GLTFCollider() {
          type = ColliderType.sphere,
          radius = sphereCollider.radius,
        };
        center = sphereCollider.center;
      } else if (unityCollider.GetType() == typeof(CapsuleCollider)) {
        var capsuleCollider = unityCollider as CapsuleCollider;
        collider = new GLTFCollider() {
          type = ColliderType.capsule,
          radius = capsuleCollider.radius,
          height = capsuleCollider.height,
        };
        center = capsuleCollider.center;
      } else if (unityCollider.GetType() == typeof(MeshCollider)) {
        var meshCollider = unityCollider as MeshCollider;

        var renderer = transform.GetComponent<MeshRenderer>();
        var meshFilter = transform.GetComponent<MeshFilter>();

        var uniquePrimitives = new List<GLTFSceneExporter.UniquePrimitive>();

        var primitive = new GLTFSceneExporter.UniquePrimitive();

        if (meshFilter.sharedMesh == meshCollider.sharedMesh) {
          primitive.Mesh = meshFilter.sharedMesh;
          primitive.Materials = renderer.sharedMaterials;
        } else {
          primitive.Mesh = meshCollider.sharedMesh;
          primitive.Materials = renderer.sharedMaterials;
        }

        var meshId = exporter.ExportMesh(meshCollider.name, uniquePrimitives);

        collider = new GLTFCollider() {
          type = ColliderType.mesh,
          mesh = meshId,
        };
      } else {
        Debug.LogFormat("Unsupported collider type {0} on {1}", unityCollider.GetType(), transform.name);
        return;
      }

      if (center == UnityEngine.Vector3.zero) {
        node.AddExtension(OMI_collider.ExtensionName, nodeCollider);
      } else {
        var child = new Node();
        
        child.Name = transform.name + "-collider";
        child.Translation = center.ToGltfVector3Convert();

        var childId = new NodeId {
          Id = gltfRoot.Nodes.Count,
          Root = gltfRoot,
        };
        gltfRoot.Nodes.Add(child);

        if (node.Children == null) {
          node.Children = new List<NodeId>();
        }

        node.Children.Add(childId);

        child.AddExtension(OMI_collider.ExtensionName, nodeCollider);
      }

      colliders.Add(collider);
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      if (colliders.Count > 0) {
        var extension = new OMI_collider() { colliders = new List<GLTFCollider>(colliders) };
        gltfRoot.AddExtension(OMI_collider.ExtensionName, extension);
        exporter.DeclareExtensionUsage(OMI_collider.ExtensionName, false);
        colliders.Clear();
      }
    }
  }
}

namespace GLTF.Schema
{
  public enum ColliderType
	{
    box,
    sphere,
    capsule,
		mesh,
	}

  [Serializable]
  public class ColliderId : GLTFId<GLTFCollider> {
    public ColliderId()
		{
		}

		public ColliderId(ColliderId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFCollider Value
		{
			get
			{
				if (Root.Extensions.TryGetValue(OMI_collider.ExtensionName, out IExtension iextension))
				{
					OMI_collider extension = iextension as OMI_collider;
					return extension.colliders[Id];
				}
				else
				{
					throw new Exception("OMI_collider not found on root object");
				}
			}
		}
  }

  [Serializable]
  public class OMI_colliderNode : IExtension {
    public ColliderId collider;

     public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(OMI_collider.ExtensionName, jo);      
      jo.Add(new JProperty(nameof(collider), collider.Id));
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new OMI_colliderNode() { collider = collider };
    }
  }

  [Serializable]
  public class GLTFCollider : GLTFChildOfRootProperty {
    public ColliderType type;
    public Vector3 extents;
    public float radius;
    public float height;
    public MeshId mesh;

    public JObject Serialize() {
      var jo = new JObject();
      
      jo.Add(new JProperty(nameof(type), type.ToString()));

      if (extents != null && extents != Vector3.Zero) {
        jo.Add(new JProperty(nameof(extents), new JArray(extents.X, extents.Y, extents.Z)));
      }

      if (radius != 0.0f) {
        jo.Add(new JProperty(nameof(radius), radius));
      }

      if (height != 0.0f) {
        jo.Add(new JProperty(nameof(height), height));
      }

      if (mesh != null) {
        jo.Add(new JProperty(nameof(mesh), mesh.Id));
      }
      

      return jo;
    }
  }

  [Serializable]
  public class OMI_collider : IExtension
  {
    public const string ExtensionName = "OMI_collider";

    public List<GLTFCollider> colliders;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      JArray arr = new JArray();

      foreach (var collider in colliders) {
        arr.Add(collider.Serialize());
      }

      jo.Add(new JProperty(nameof(colliders), arr));

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new OMI_collider() { colliders = colliders };
    }
  }
  
}

#endif