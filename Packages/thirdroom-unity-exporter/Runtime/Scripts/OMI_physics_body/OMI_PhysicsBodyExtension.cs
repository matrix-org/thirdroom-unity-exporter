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

  public class PhysicsBodyExtensionConfig : ScriptableObject
  {

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
    }

     private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) {
      Collider collider = transform.GetComponent<Collider>();
      Rigidbody rigidBody = transform.GetComponent<Rigidbody>();
      Rigidbody parentRigidBody = transform.parent != null ? transform.parent.GetComponent<Rigidbody>() : null;

      if (collider == null) {
        return;
      }

      if (rigidBody == null && parentRigidBody == null) {
        node.AddExtension(OMI_PhysicsBody.ExtensionName, new OMI_PhysicsBody() {
          type = "static"
        });

        exporter.DeclareExtensionUsage(OMI_PhysicsBody.ExtensionName, false);
      } else if (rigidBody != null) {
        string type;

        if (rigidBody.isKinematic) {
          type = "kinematic";
        } else {
          type = "rigid";
        }

        node.AddExtension(OMI_PhysicsBody.ExtensionName, new OMI_PhysicsBody() {
          type = type,
          mass = rigidBody.mass,
          linearVelocity = rigidBody.velocity.ToGltfVector3Raw(),
          angularVelocity = rigidBody.angularVelocity.ToGltfVector3Raw(),
          // TODO
          inertiaTensor = new float[] {
            0, 0, 0,
            0, 0, 0,
            0, 0, 0
          }
        });

        exporter.DeclareExtensionUsage(OMI_PhysicsBody.ExtensionName, false);
      }
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class OMI_PhysicsBody : IExtension
  {
    public const string ExtensionName = "OMI_physics_body";

    public string type;

    public float mass;

    public Vector3 linearVelocity;

    public Vector3 angularVelocity;

    public float[] inertiaTensor;

    public JProperty Serialize()
    {
      var jo = new JObject();

      jo.Add(new JProperty(nameof(type), type));

      if (mass != 1.0f) {
        jo.Add(new JProperty(nameof(mass), mass));
      }

      if (linearVelocity != null && linearVelocity != Vector3.Zero) {
        jo.Add(new JProperty(
          nameof(linearVelocity),
          new JArray(linearVelocity.X, linearVelocity.Y, linearVelocity.Z)
        ));
      }

      if (angularVelocity != null && angularVelocity != Vector3.Zero) {
        jo.Add(new JProperty(
          nameof(angularVelocity),
          new JArray(angularVelocity.X, angularVelocity.Y, angularVelocity.Z)
        ));
      }

      if (inertiaTensor != null && inertiaTensor.Any(x => x != 0.0f)) {
        jo.Add(new JProperty(nameof(inertiaTensor), new JArray(inertiaTensor)));
      }

      return new JProperty(ExtensionName, jo);
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new OMI_PhysicsBody();
    }
  }
}

#endif