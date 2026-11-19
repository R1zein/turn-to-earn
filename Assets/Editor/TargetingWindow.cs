using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public class TargetingWindow : EditorWindow
{
    private const string Folder = "Assets/Data/Targeting";
    private const float ProfileWidth = 152f;
    private const float WeightWidth = 106f;

    private TargetType[] types;
    private TargetProfile[] profiles;
    private TargetProfile selected;
    private Vector2 scroll;

    [MenuItem("Window/Turn To Earn/Targeting")]
    private static void Open() => GetWindow<TargetingWindow>("Targeting");

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndo;
        Reload();
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndo;

    private void OnProjectChange() => Reload();

    private void OnUndo()
    {
        Reload();
        Repaint();
    }

    private void Reload()
    {
        types = LoadAssets<TargetType>();
        profiles = LoadAssets<TargetProfile>();
    }

    private T[] LoadAssets<T>() where T : ScriptableObject
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            return Array.Empty<T>();

        var assets = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Folder }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
                assets.Add(asset);
        }
        assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return assets.ToArray();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Add type", EditorStyles.toolbarButton))
                Create<TargetType>("New Type");
            if (GUILayout.Button("Add profile", EditorStyles.toolbarButton))
                selected = Create<TargetProfile>("New Profile");
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.LabelField("Enter a weight to target a type. Clear the cell to exclude it.", EditorStyles.wordWrappedLabel);

        using (var view = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.MinHeight(130f)))
        {
            scroll = view.scrollPosition;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel, GUILayout.Width(ProfileWidth));
                foreach (TargetType type in types)
                    DrawTypeName(type);
            }

            foreach (TargetProfile profile in profiles)
                DrawProfileRow(profile);
        }

        if (selected != null)
            DrawDetails(selected);
    }

    private void DrawTypeName(TargetType type)
    {
        var asset = new SerializedObject(type);
        SerializedProperty label = asset.FindProperty("displayName");
        string edited = EditorGUILayout.DelayedTextField(type.DisplayName, GUILayout.Width(WeightWidth));
        if (edited != type.DisplayName && !string.IsNullOrWhiteSpace(edited))
        {
            label.stringValue = edited.Trim();
            asset.ApplyModifiedProperties();
        }
    }

    private void DrawProfileRow(TargetProfile profile)
    {
        var asset = new SerializedObject(profile);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Toggle(selected == profile, profile.DisplayName, EditorStyles.miniButton, GUILayout.Width(ProfileWidth)))
                selected = profile;

            SerializedProperty weights = asset.FindProperty("typeWeights");
            foreach (TargetType type in types)
                DrawWeight(weights, type);
        }
        asset.ApplyModifiedProperties();
    }

    private void DrawWeight(SerializedProperty weights, TargetType type)
    {
        int index = -1;
        for (int i = 0; i < weights.arraySize; i++)
        {
            if (weights.GetArrayElementAtIndex(i).FindPropertyRelative("type").objectReferenceValue == type)
            {
                index = i;
                break;
            }
        }
        string value = index < 0 ? string.Empty : weights.GetArrayElementAtIndex(index)
            .FindPropertyRelative("weight").floatValue.ToString("G", CultureInfo.CurrentCulture);
        string edited = EditorGUILayout.DelayedTextField(value, GUILayout.Width(WeightWidth));
        if (edited == value)
            return;

        if (string.IsNullOrWhiteSpace(edited))
        {
            if (index >= 0)
                weights.DeleteArrayElementAtIndex(index);
            return;
        }
        if (!float.TryParse(edited, NumberStyles.Float, CultureInfo.CurrentCulture, out float number)
            || !float.IsFinite(number) || number < 0f)
            return;

        if (index < 0)
        {
            index = weights.arraySize;
            weights.InsertArrayElementAtIndex(index);
        }
        SerializedProperty row = weights.GetArrayElementAtIndex(index);
        row.FindPropertyRelative("type").objectReferenceValue = type;
        row.FindPropertyRelative("weight").floatValue = number;
    }

    private void DrawDetails(TargetProfile profile)
    {
        var asset = new SerializedObject(profile);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Selected profile", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(asset.FindProperty("displayName"));
            EditorGUILayout.PropertyField(asset.FindProperty("targetFractions"));
            EditorGUILayout.PropertyField(asset.FindProperty("distanceCurve"));
            EditorGUILayout.PropertyField(asset.FindProperty("crowdCurve"));
            EditorGUILayout.PropertyField(asset.FindProperty("holdBonus"));
            EditorGUILayout.PropertyField(asset.FindProperty("maxDistance"));
            EditorGUILayout.PropertyField(asset.FindProperty("needsPath"));
        }
        asset.ApplyModifiedProperties();
    }

    private T Create<T>(string label) where T : ScriptableObject
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Data", "Targeting");

        T asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + label + ".asset"));
        Reload();
        return asset;
    }
}
