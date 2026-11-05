using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PrepareProtocolEscapeAssets
{
    private const string Root = "Assets/ArtTest/Protocol_Escape_Assets";

    public static void Prepare()
    {
        var paths = new List<string>();
        foreach (var file in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories))
            paths.Add(file.Replace('\\', '/'));

        foreach (var path in paths)
        {
            if (path.Contains("/previews/")) continue;
            if (path.EndsWith("engineer_spritesheet_3x4.png") || path.EndsWith("robot_spritesheet_3x4.png"))
                ConfigureGrid(path, 32, 40, 3, 4);
            else if (path.EndsWith("lava_tileset_6x2.png"))
                ConfigureGrid(path, 32, 32, 6, 2);
            else
                ConfigureSingle(path);
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        Debug.Log("Protocol Escape assets prepared as 32 PPU point-filtered sprites. Preview images remain ordinary textures.");
    }

    private static TextureImporter GetImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing TextureImporter for " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        return importer;
    }

    private static void ConfigureSingle(string path)
    {
        var importer = GetImporter(path);
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }

    private static void ConfigureGrid(string path, int cellWidth, int cellHeight, int columns, int rows)
    {
        var importer = GetImporter(path);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        var metadata = new List<SpriteMetaData>();
        for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
        {
            metadata.Add(new SpriteMetaData
            {
                name = $"{Path.GetFileNameWithoutExtension(path)}_{column}_{row}",
                rect = new Rect(column * cellWidth, (rows - 1 - row) * cellHeight, cellWidth, cellHeight),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }
        importer.spritesheet = metadata.ToArray();
        importer.SaveAndReimport();
    }
}
