using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace _25DogMusic.Desktop;

/// <summary>按数量上限缓存已解码的封面，淘汰最久未使用项，避免长时间浏览后内存无上限增长。</summary>
sealed class LruImageCache(int capacity)
{
    readonly record struct Entry(string Key, Bitmap Image);

    readonly Dictionary<string, LinkedListNode<Entry>> _map = new();
    readonly LinkedList<Entry> _list = new();

    public Bitmap? Get(string key)
    {
        var node = _map.GetValueOrDefault(key);
        if (node is null) return null;

        // 命中后移到队首，保持 LRU 顺序
        _list.Remove(node);
        _list.AddFirst(node);
        return node.Value.Image;
    }

    public void Set(string key, Bitmap image)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            _list.Remove(existing);
            _map.Remove(key);
        }

        var node = _list.AddFirst(new Entry(key, image));
        _map[key] = node;

        while (_list.Count > capacity && _list.Last is { } last)
        {
            _map.Remove(last.Value.Key);
            _list.RemoveLast();
        }
    }

    public void Clear()
    {
        _map.Clear();
        _list.Clear();
    }
}
