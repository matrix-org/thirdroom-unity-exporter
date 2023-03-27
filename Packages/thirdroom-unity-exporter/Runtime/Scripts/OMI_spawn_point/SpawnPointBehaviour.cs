using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdRoom.Exporter
{
  public class SpawnPointBehaviour : MonoBehaviour {
	public string title;
    public string team;
    public string group;

    private void OnDrawGizmos() {
          #if UNITY_EDITOR
            var radius = 0.5f;
            var height = 1.0f;

            var p1 = transform.position;
            var p2 = transform.position + new Vector3(0, height, 0);

            using (new UnityEditor.Handles.DrawingScope(Color.green, Gizmos.matrix))
            {
                Quaternion p1Rotation = Quaternion.LookRotation(p1 - p2);
                Quaternion p2Rotation = Quaternion.LookRotation(p2 - p1);
                // Check if capsule direction is collinear to Vector.up
                float c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
                if (c == 1f || c == -1f)
                {
                    // Fix rotation
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // First side
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left,  p1Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up,  p1Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // Second side
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left,  p2Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up,  p2Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // Lines
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);

                var p3 = p1 + (transform.forward * 0.5f);

                UnityEditor.Handles.DrawLine(p1, p3);
        
                Vector3 right = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0,180 + 20, 0) * new Vector3(0,0,1);
                Vector3 left = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0,180-20,0) * new Vector3(0,0,1);
                UnityEditor.Handles.DrawLine(p3, p3 + right * 0.25f);
                UnityEditor.Handles.DrawLine(p3, p3 + left * 0.25f);

            }
          #endif
    }
  }
}
