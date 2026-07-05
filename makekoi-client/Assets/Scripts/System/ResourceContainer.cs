using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class GamePhaseContentResource
{
    public GamePhaseContentType ContentType;
    public GameObject ContentPrefab;
}
public class ResourceContainer : MonoSingletoneBase<ResourceContainer>
{
    private const string ItemIconResourceDirectory = "2D/UI/item";
    [SerializeField] private List<GamePhaseContentResource> _gamePhaseContentResources;
    private class ItemImageCache
    {
        public string Key;
        public Sprite ResourceImageCache;
    }

    private List<ItemImageCache> _itemImageCacheList = new List<ItemImageCache>();
    private Dictionary<string, Sprite> _spriteCacheByPath = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Initialize(this);
    }

    public Sprite GetItemIconSprite(string spritePath)
    {
        var cache = _itemImageCacheList.Find(x => x.Key == spritePath);
        if (cache != null)
        {
            return cache.ResourceImageCache;
        }

        var resourcePath = GetItemIconResourcePath(spritePath);
        var image = Resources.Load<Sprite>(resourcePath);
        if (image != null)
        {
            _itemImageCacheList.Add(new ItemImageCache
            {
                Key = spritePath,
                ResourceImageCache = image
            });
            return image;
        }

        Debug.LogError($"ResourceContainer: {resourcePath} not found");
        return null;
    }

    public Sprite GetSpriteByPath(string resourcePath)
    {
        if (_spriteCacheByPath.TryGetValue(resourcePath, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            _spriteCacheByPath[resourcePath] = sprite;
            return sprite;
        }

        Debug.LogError($"ResourceContainer: {resourcePath} not found");
        return null;
    }

    private static string GetItemIconResourcePath(string itemSpriteFileName)
    {
        return $"{ItemIconResourceDirectory}/{GetResourceFileName(itemSpriteFileName)}";
    }

    private static string GetResourceFileName(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return resourcePath;
        }

        var normalizedPath = resourcePath.Replace('\\', '/');
        var slashIndex = normalizedPath.LastIndexOf('/');
        return slashIndex >= 0 ? normalizedPath.Substring(slashIndex + 1) : normalizedPath;
    }
    public GameObject GetGamePhaseContentPrefab(GamePhaseContentType contentType)
    {
        var resource = _gamePhaseContentResources.Find(x => x.ContentType == contentType);
        if (resource != null)
        {
            return resource.ContentPrefab;
        }

        Debug.LogError($"ResourceContainer: GamePhaseContentPrefab for {contentType} not found");
        return null;
    }
}
