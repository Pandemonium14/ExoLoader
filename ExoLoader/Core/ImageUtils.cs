using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ExoLoader
{
    public static class ImageUtils
    {
        public static Texture2D LoadTexture(string filePath, bool silent = false)
        {
            if (!File.Exists(filePath))
            {
                if (!silent)
                {
                    ModInstance.log($"Texture file not found: {filePath}");
                    ModLoadingStatus.LogError($"Texture file not found: {filePath}");
                }

                return null;
            }

            try
            {
                var texture = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes(filePath);
                ImageConversion.LoadImage(texture, bytes);
                texture.Apply();
                texture.name = Path.GetFileNameWithoutExtension(filePath);
                return texture;
            }
            catch (Exception e)
            {
                if (!silent)
                {
                    ModInstance.log($"Failed to load texture from: {filePath}");
                    ModInstance.log($"Error: {e}");
                    ModLoadingStatus.LogError($"Failed to load texture from {filePath}: {e.Message}");
                }
                return null;
            }
        }

        public static Texture2D[] LoadTexturesFromFolder(string folderPath, bool silentErrors = false)
        {
            if (!Directory.Exists(folderPath))
            {
                if (!silentErrors)
                {
                    ModInstance.log($"Folder not found: {folderPath}");
                    ModLoadingStatus.LogError($"Folder not found: {folderPath}");
                }
                return [];
            }

            List<Texture2D> textures = new List<Texture2D>();
            string[] files = Directory.GetFiles(folderPath, "*.png");
            foreach (string file in files)
            {
                try
                {
                    Texture2D texture = LoadTexture(file, silentErrors);
                    if (texture != null)
                    {
                        textures.Add(texture);
                    }
                    else if (!silentErrors)
                    {
                        ModInstance.log($"Failed to load texture from file: {file}");
                        ModLoadingStatus.LogError($"Failed to load texture from file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    if (!silentErrors)
                    {
                        ModInstance.log($"Exception loading texture {file}: {ex.Message}");
                        ModLoadingStatus.LogError($"Exception loading texture {file}: {ex.Message}");
                    }
                }
            }
            return [.. textures];
        }

        public static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit = 100f, Vector2? pivot = null, string spriteName = null)
        {
            if (texture == null)
                return null;

            var actualPivot = pivot ?? new Vector2(0.5f, 0f);
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                actualPivot,
                pixelsPerUnit
            );

            if (!string.IsNullOrEmpty(spriteName))
                sprite.name = spriteName;

            return sprite;
        }

        private static float CalculatePixelsPerUnit(int textureHeight, int targetHeightInUnits)
        {
            return (float)textureHeight / targetHeightInUnits;
        }

        public static Sprite LoadSprite(string filePath, SpriteLoadConfig config = null)
        {
            var texture = LoadTexture(filePath, config.SilentErrors);
            if (texture == null)
                return null;

            // Determine sprite name
            string spriteName;
            if (!string.IsNullOrEmpty(config.SpriteName))
            {
                spriteName = config.SpriteName;
            }
            else if (!string.IsNullOrEmpty(config.NamePrefix))
            {
                var baseName = Path.GetFileNameWithoutExtension(filePath);
                spriteName = $"{config.NamePrefix}{baseName}";
            }
            else
            {
                spriteName = Path.GetFileNameWithoutExtension(filePath);
            }

            // Calculate pixels per unit
            float pixelsPerUnit;
            if (config.TargetHeightInUnits.HasValue)
            {
                pixelsPerUnit = CalculatePixelsPerUnit(texture.height, config.TargetHeightInUnits.Value);
            }
            else
            {
                pixelsPerUnit = config.PixelsPerUnit;
            }

            return CreateSprite(texture, pixelsPerUnit, config.Pivot, spriteName);
        }

        public static Sprite[] LoadSpritesFromFolder(string folderPath, SpriteLoadConfig config = null)
        {
            if (config == null)
                config = new SpriteLoadConfig();

            List<Sprite> sprites = [];
            string[] files = Directory.GetFiles(folderPath, "*.png");

            foreach (string file in files)
            {
                try
                {
                    var sprite = LoadSprite(file, config);
                    if (sprite != null)
                    {
                        sprites.Add(sprite);
                    }
                    else if (!config.SilentErrors)
                    {
                        ModInstance.log($"Failed to load sprite from file: {file}");
                        ModLoadingStatus.LogError($"Failed to load sprite from file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    if (!config.SilentErrors)
                    {
                        ModInstance.log($"Exception loading sprite {file}: {ex.Message}");
                        ModLoadingStatus.LogError($"Exception loading sprite {file}: {ex.Message}");
                    }
                }
            }

            return [.. sprites];
        }
    }

    public class SpriteLoadConfig
    {
        public string SpriteName { get; set; }
        public string NamePrefix { get; set; }
        public int? TargetHeightInUnits { get; set; }
        public float PixelsPerUnit { get; set; } = 100f;
        public Vector2? Pivot { get; set; }
        public bool SilentErrors { get; set; } = false;
    }
}
