using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class TypeReference {
  [SerializeField]
  private string assemblyQualifiedName;

  public Type Type {
    get => string.IsNullOrEmpty(assemblyQualifiedName) ? null : Type.GetType(assemblyQualifiedName);
    set => assemblyQualifiedName = value?.AssemblyQualifiedName;
  }

  public TypeReference(Type type) {
    Type = type;
  }
}

[CustomPropertyDrawer(typeof(TypeReference))]
public class TypeReferenceDrawer : PropertyDrawer
{
    private Type[] derivedTypes;
    private string[] typeNames;
    private int selectedIndex;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty assemblyQualifiedName = property.FindPropertyRelative("assemblyQualifiedName");
        Type currentType = string.IsNullOrEmpty(assemblyQualifiedName.stringValue) ? null : Type.GetType(assemblyQualifiedName.stringValue);

        if (derivedTypes == null)
        {
            Assembly currentAssembly = Assembly.GetAssembly(typeof(ThirdroomComponent));
            derivedTypes = currentAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ThirdroomComponent))).ToArray();
            typeNames = derivedTypes.Select(t => t.Name).ToArray();
            selectedIndex = Array.IndexOf(derivedTypes, currentType);
        }

        EditorGUI.BeginChangeCheck();

        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, typeNames);

        if (EditorGUI.EndChangeCheck())
        {
            assemblyQualifiedName.stringValue = selectedIndex >= 0 ? derivedTypes[selectedIndex].AssemblyQualifiedName : string.Empty;
        }
    }
}

public class ThirdroomAdditionalComponents : MonoBehaviour {
  [SerializeField]
  public List<TypeReference> components;
}
