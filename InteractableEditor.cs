using UnityEditor;
using UnityEngine;
using static Interactable;

[CustomEditor(typeof(Interactable))]
public class InteractableEditor : Editor
{
    private SerializedProperty colliderTypeProp;
    private SerializedProperty interactableTypeProp;
    private SerializedProperty puzzlePiecesProp;
    private string[] colliderOptions = { "Box Collider", "Sphere Collider", "Capsule Collider", "Mesh Collider" };

    private void OnEnable()
    {
        colliderTypeProp = serializedObject.FindProperty("colliderType");
        interactableTypeProp = serializedObject.FindProperty("interactableType");
        puzzlePiecesProp = serializedObject.FindProperty("puzzlePieces");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Interactable interactable = (Interactable)target;

        EditorGUILayout.PropertyField(interactableTypeProp);

        Interactable.EInteractableType interactableType = (Interactable.EInteractableType)interactableTypeProp.enumValueIndex;

        if (interactableType == Interactable.EInteractableType.Door)
        {
            EditorGUILayout.PropertyField(puzzlePiecesProp);
        }

        int colliderType = colliderTypeProp.intValue;
        int newColliderType = EditorGUILayout.Popup("Attach a Collider", colliderType - 1, colliderOptions) + 1;

        if (newColliderType != colliderType)
        {
            Undo.RecordObject(target, "Change Collider Type");
            colliderTypeProp.intValue = newColliderType;
            serializedObject.ApplyModifiedProperties();

            AttachCollider(interactable, newColliderType);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AttachCollider(Interactable interactable, int colliderType)
    {
        Collider existingCollider = interactable.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Debug.Log($"Removing existing collider: {existingCollider.GetType().Name}");
            DestroyImmediate(existingCollider);
        }

        Collider collider = null;

        switch (colliderType)
        {
            case 1: // Box Collider
                collider = interactable.gameObject.AddComponent<BoxCollider>();
                break;
            case 2: // Sphere Collider
                collider = interactable.gameObject.AddComponent<SphereCollider>();
                break;
            case 3: // Capsule Collider
                collider = interactable.gameObject.AddComponent<CapsuleCollider>();
                break;
            case 4: // Mesh Collider
                collider = interactable.gameObject.AddComponent<MeshCollider>();
                break;
        }

        if (collider != null)
        {
            Debug.Log($"Collider attached: {collider.GetType().Name}");
        }
    }
}
