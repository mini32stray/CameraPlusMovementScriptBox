using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.Core
{
	public sealed class SimpleMultiMap<TKey, TValue> where TKey : notnull
	{
		private readonly ConcurrentDictionary<TKey, List<TValue>> _map = new();

		 /* 
		  * Note: This implementation is not thread-safe for concurrent Add() to the same key.
		  */
		public void Add(TKey key, TValue value)
		{
			if (!_map.TryGetValue(key, out List<TValue>? list))
			{
				list = new List<TValue>();
				if (!_map.TryAdd(key, list))
				{
					list = _map[key];
				}
			}
			list.Add(value);
		}

		public IReadOnlyList<TValue> GetValues(TKey key)
		{
			if (_map.TryGetValue(key, out List<TValue>? list))
			{
				return list;
			}
			return Array.Empty<TValue>(); ;
		}

		public IEnumerable<TKey> Keys => _map.Keys;

		public IEnumerable<TValue> Values => _map.Values.SelectMany(x => x);

		public bool ContainsKey(TKey key) => _map.ContainsKey(key);

		public int Count => _map.Count;

		public void Clear() => _map.Clear();
	}
}