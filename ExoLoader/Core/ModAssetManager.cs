using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace ExoLoader
{
    public static class ModAssetManager
    {
        // Main storage dictionaries organized by content type
        private static readonly Dictionary<AssetContentType, Dictionary<string, IModAsset>> _assetStore = 
            new Dictionary<AssetContentType, Dictionary<string, IModAsset>>();

        // Quick lookup for sprites specifically (most common use case)
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // Asset counts per type for progress tracking
        private static readonly Dictionary<AssetContentType, int> _assetCounts = 
            new Dictionary<AssetContentType, int>();

        private static readonly List<string> _existingStorySprites = [];

        #region Public API

        public static void StoreSprite(AssetContentType contentType, string key, Sprite sprite)
        {
            var asset = new SpriteAsset(sprite);
            StoreAsset(contentType, key, asset);
            _spriteCache[GetFullKey(contentType, key)] = sprite;

            if (contentType == AssetContentType.CharacterStory)
            {
                string[] parts = key.Split('_');
                // remove any numbers if present from parts[0]
                string charaId = parts[0].RemoveEnding("1").RemoveEnding("2").RemoveEnding("3");
                if (!_existingStorySprites.Contains(charaId))
                {
                    _existingStorySprites.Add(charaId);
                }

                if (parts[1] != null)
                {
                    string storySpriteKey = $"{charaId}_{parts[1]}";
                    if (!_spriteCache.ContainsKey(storySpriteKey))
                    {
                        _spriteCache[storySpriteKey] = sprite;
                    }
                }
            }
            else if (contentType == AssetContentType.StorySprite)
            {
                _existingStorySprites.Add(key);
            }
        }

        public static void StoreSpriteAnimation(AssetContentType contentType, string key, Sprite[] sprites)
        {
            var asset = new SpriteAnimationAsset(sprites);
            StoreAsset(contentType, key, asset);
        }

        public static void StoreSkeletonData(AssetContentType contentType, string key, Texture2D[] textures,
            TextAsset atlasText, TextAsset skeletonJson, bool isAnimated)
        {
            var asset = new SkeletonAsset(textures, atlasText, skeletonJson, isAnimated);
            StoreAsset(contentType, key, asset);
        }

        public static void StoreGameObject(AssetContentType contentType, string key, GameObject gameObject)
        {
            var asset = new GameObjectAsset(gameObject);
            StoreAsset(contentType, key, asset);
        }

        public static Sprite GetSprite(AssetContentType contentType, string key)
        {
            string fullKey = GetFullKey(contentType, key);
            if (_spriteCache.TryGetValue(fullKey, out Sprite sprite))
                return sprite;

            var asset = GetAsset(contentType, key);
            return (asset as SpriteAsset)?.Sprite;
        }

        public static Sprite GetStorySprite(string key)
        {
            // It's either CharacterStory or StorySprite
            if (_spriteCache.TryGetValue(key, out Sprite sprite))
                return sprite;

            // If not found in cache, check if it's a character story sprite
            var asset = GetAsset(AssetContentType.CharacterStory, key);
            if (asset == null)
            {
                asset = GetAsset(AssetContentType.StorySprite, key);
            }
            return (asset as SpriteAsset)?.Sprite;
        }

        public static Sprite[] GetSpriteAnimation(AssetContentType contentType, string key)
        {
            var asset = GetAsset(contentType, key);
            return (asset as SpriteAnimationAsset)?.Sprites;
        }

        public static SkeletonAsset GetSkeletonData(AssetContentType contentType, string key)
        {
            var asset = GetAsset(contentType, key);
            ModInstance.log($"Getting skeleton data for {key} of type {contentType}: {asset}");
            return asset as SkeletonAsset;
        }

        public static GameObject GetGameObject(AssetContentType contentType, string key)
        {
            var asset = GetAsset(contentType, key);
            return (asset as GameObjectAsset)?.GameObject;
        }

        public static bool HasAsset(AssetContentType contentType, string key)
        {
            return _assetStore.ContainsKey(contentType) && 
                   _assetStore[contentType].ContainsKey(key);
        }

        public static bool StorySpriteExists(string spriteName)
        {
            return _existingStorySprites.Contains(spriteName);
        }

        public static IEnumerable<string> GetAssetKeys(AssetContentType contentType)
        {
            if (_assetStore.ContainsKey(contentType))
                return _assetStore[contentType].Keys;
            return [];
        }

        public static int GetAssetCount(AssetContentType contentType)
        {
            return _assetCounts.ContainsKey(contentType) ? _assetCounts[contentType] : 0;
        }

        public static int GetTotalAssetCount()
        {
            int total = 0;
            foreach (var count in _assetCounts.Values)
                total += count;
            return total;
        }

        public static void ClearAllAssets()
        {
            // Cleanup Unity objects to prevent memory leaks
            foreach (var typeDict in _assetStore.Values)
            {
                foreach (var asset in typeDict.Values)
                {
                    asset.Dispose();
                }
            }

            _assetStore.Clear();
            _spriteCache.Clear();
            _assetCounts.Clear();
        }

        public static void ClearAssets(AssetContentType contentType)
        {
            if (_assetStore.ContainsKey(contentType))
            {
                foreach (var asset in _assetStore[contentType].Values)
                {
                    asset.Dispose();
                }
                _assetStore[contentType].Clear();
            }

            // Clear from sprite cache
            var keysToRemove = new List<string>();
            foreach (var key in _spriteCache.Keys)
            {
                if (key.StartsWith(contentType.ToString() + "_"))
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                _spriteCache.Remove(key);
            }

            _assetCounts[contentType] = 0;
        }

        #endregion

        #region Private Methods

        private static void StoreAsset(AssetContentType contentType, string key, IModAsset asset)
        {
            if (!_assetStore.ContainsKey(contentType))
                _assetStore[contentType] = new Dictionary<string, IModAsset>();

            _assetStore[contentType][key] = asset;

            // Update count
            if (!_assetCounts.ContainsKey(contentType))
                _assetCounts[contentType] = 0;
            _assetCounts[contentType]++;
        }

        private static IModAsset GetAsset(AssetContentType contentType, string key)
        {
            if (_assetStore.ContainsKey(contentType) && 
                _assetStore[contentType].ContainsKey(key))
            {
                return _assetStore[contentType][key];
            }
            return null;
        }

        private static string GetFullKey(AssetContentType contentType, string key)
        {
            return $"{contentType}_{key}";
        }

        #endregion
    }

    #region Asset Types and Interfaces

    public enum AssetContentType
    {
        Achievement,
        Background,
        BackgroundThumbnail,
        Card,
        CharacterMainMenu,
        CharacterModel,
        CharacterPortrait,
        CharacterSpriteModel,
        CharacterStory,
        StorySprite
    }

    public interface IModAsset : IDisposable
    {
        AssetType Type { get; }
        bool IsLoaded { get; }
    }

    public enum AssetType
    {
        Sprite,
        SkeletonData,
        GameObject,
        Texture2D
    }

    public class SpriteAsset : IModAsset
    {
        public Sprite Sprite { get; private set; }
        public AssetType Type => AssetType.Sprite;
        public bool IsLoaded => Sprite != null;

        public SpriteAsset(Sprite sprite)
        {
            Sprite = sprite;
        }

        public void Dispose()
        {
            if (Sprite != null)
            {
                UnityEngine.Object.DestroyImmediate(Sprite);
                Sprite = null;
            }
        }
    }

    public class SpriteAnimationAsset : IModAsset
    {
        public Sprite[] Sprites { get; private set; }
        public AssetType Type => AssetType.Sprite;
        public bool IsLoaded => Sprites != null && Sprites.Length > 0;

        public SpriteAnimationAsset(Sprite[] sprites)
        {
            Sprites = sprites;
        }

        public void Dispose()
        {
            if (Sprites != null)
            {
                foreach (var sprite in Sprites)
                {
                    if (sprite != null)
                        UnityEngine.Object.DestroyImmediate(sprite);
                }
                Sprites = null;
            }
        }
    }

    public class SkeletonAsset : IModAsset
    {
        public Texture2D[] Textures { get; private set; }
        public TextAsset AtlasText { get; private set; }
        public TextAsset SkeletonJson { get; private set; }
        public bool isAnimated;
        public SkeletonDataAsset SkeletonData { get; private set; }
        public AssetType Type => AssetType.SkeletonData;
        public bool IsLoaded => false;

        public SkeletonAsset(Texture2D[] textures, TextAsset atlas, TextAsset skeletonJson, bool isAnimated)
        {
            Textures = textures;
            AtlasText = atlas;
            SkeletonJson = skeletonJson;
            this.isAnimated = isAnimated;
        }

        public void Dispose()
        {
            if (Textures != null)
            {
                foreach (var tex in Textures)
                {
                    if (tex != null)
                        UnityEngine.Object.DestroyImmediate(tex);
                }
                Textures = null;
            }
            if (AtlasText != null)
            {
                UnityEngine.Object.DestroyImmediate(AtlasText);
                AtlasText = null;
            }
            if (SkeletonJson != null)
            {
                UnityEngine.Object.DestroyImmediate(SkeletonJson);
                SkeletonJson = null;
            }
        }
    }

    public class GameObjectAsset : IModAsset
    {
        public GameObject GameObject { get; private set; }
        public AssetType Type => AssetType.GameObject;
        public bool IsLoaded => GameObject != null;

        public GameObjectAsset(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public void Dispose()
        {
            if (GameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject);
                GameObject = null;
            }
        }
    }

    #endregion
}
