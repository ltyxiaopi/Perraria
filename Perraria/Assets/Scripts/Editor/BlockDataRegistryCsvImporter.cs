using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class BlockDataRegistryCsvImporter
{
    private const string CsvAssetPath = "Assets/Data/BlockDataRegistry.csv";
    private const string RegistryAssetPath = "Assets/Data/BlockDataRegistry.asset";

    [MenuItem("Perraria/Tools/Import Block Data CSV...")]
    public static void ImportWithPicker()
    {
        string filePath = EditorUtility.OpenFilePanel("Import Block Data CSV", Application.dataPath, "csv");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        ImportFromFile(filePath);
    }

    [MenuItem("Perraria/Tools/Import Block Data CSV Into Selected Registry...")]
    public static void ImportIntoSelectedRegistryWithPicker()
    {
        BlockDataRegistry registry = Selection.activeObject as BlockDataRegistry;
        if (registry == null)
        {
            Debug.LogError("Select a BlockDataRegistry asset in the Project window first.");
            return;
        }

        string filePath = EditorUtility.OpenFilePanel("Import Block Data CSV", Application.dataPath, "csv");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        ImportFromFile(filePath, registry);
    }

    [MenuItem("Perraria/Tools/Import Default Block Data CSV")]
    public static void ImportDefault()
    {
        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvAssetPath);
        if (csvAsset == null)
        {
            Debug.LogError($"CSV asset not found at '{CsvAssetPath}'.");
            return;
        }

        ImportFromText(csvAsset.text, CsvAssetPath, GetOrCreateDefaultRegistry());
    }

    public static void ImportFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found at '{filePath}'.");
            return;
        }

        string csvText = System.IO.File.ReadAllText(filePath);
        ImportFromText(csvText, filePath, GetOrCreateDefaultRegistry());
    }

    public static void ImportFromFile(string filePath, BlockDataRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (registry == null)
        {
            Debug.LogError("Target BlockDataRegistry is null.");
            return;
        }

        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found at '{filePath}'.");
            return;
        }

        string csvText = System.IO.File.ReadAllText(filePath);
        ImportFromText(csvText, filePath, registry);
    }

    [MenuItem("Perraria/Tools/Import Block Data CSV...", true)]
    [MenuItem("Perraria/Tools/Import Block Data CSV Into Selected Registry...", true)]
    [MenuItem("Perraria/Tools/Import Default Block Data CSV", true)]
    public static bool ValidateImport()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem("Perraria/Tools/Create Block Data Registry Asset")]
    public static void CreateRegistryAsset()
    {
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Create Block Data Registry",
            "BlockDataRegistry",
            "asset",
            "Choose where to create the BlockDataRegistry asset.");

        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        BlockDataRegistry registry = ScriptableObject.CreateInstance<BlockDataRegistry>();
        AssetDatabase.CreateAsset(registry, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = registry;

        Debug.Log($"Created BlockDataRegistry asset at '{assetPath}'.");
    }

    private static void ImportFromText(string csvText, string sourceLabel, BlockDataRegistry registry)
    {
        if (!TryParseRows(csvText, out List<BlockDataRegistry.BlockData> rows, out string error))
        {
            Debug.LogError(error);
            return;
        }

        SerializedObject serializedObject = new SerializedObject(registry);
        SerializedProperty blocks = serializedObject.FindProperty("_blocks");
        blocks.arraySize = rows.Count;

        for (int i = 0; i < rows.Count; i++)
        {
            SerializedProperty entry = blocks.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("Type").enumValueIndex = (int)rows[i].Type;
            entry.FindPropertyRelative("Hardness").floatValue = rows[i].Hardness;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string targetPath = AssetDatabase.GetAssetPath(registry);
        Debug.Log($"Imported {rows.Count} block data rows from '{sourceLabel}' into '{targetPath}'.");
    }

    private static BlockDataRegistry GetOrCreateDefaultRegistry()
    {
        BlockDataRegistry registry = AssetDatabase.LoadAssetAtPath<BlockDataRegistry>(RegistryAssetPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<BlockDataRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryAssetPath);
        }

        return registry;
    }

    private static bool TryParseRows(
        string csvText,
        out List<BlockDataRegistry.BlockData> rows,
        out string error)
    {
        rows = new List<BlockDataRegistry.BlockData>();
        error = null;

        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (rows.Count == 0 && line.StartsWith("Type,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] columns = line.Split(',');
            if (columns.Length != 2)
            {
                error = $"Invalid CSV format on line {i + 1}: '{line}'. Expected 'Type,Hardness'.";
                return false;
            }

            if (!Enum.TryParse(columns[0].Trim(), true, out BlockType blockType))
            {
                error = $"Unknown BlockType '{columns[0].Trim()}' on line {i + 1}.";
                return false;
            }

            if (!float.TryParse(columns[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float hardness))
            {
                error = $"Invalid hardness '{columns[1].Trim()}' on line {i + 1}.";
                return false;
            }

            rows.Add(new BlockDataRegistry.BlockData
            {
                Type = blockType,
                Hardness = hardness
            });
        }

        if (rows.Count == 0)
        {
            error = $"No block data rows were found in '{CsvAssetPath}'.";
            return false;
        }

        return true;
    }
}
