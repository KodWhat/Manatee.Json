﻿using System;
using System.Threading;

namespace Manatee.Json.Internal
{
	internal class ObjectCache<T> where T : class
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly int _cacheSize = Environment.ProcessorCount * 2;

		private readonly T[] _items = new T[_cacheSize];
		private T _item = default!;

		private readonly Func<T> _builder;

		public ObjectCache(Func<T> builder)
		{
			_builder = builder;
		}

		public T Acquire()
		{
			var instance = _item;
			if (instance == null || instance != Interlocked.CompareExchange(ref _item, null!, instance))
				instance = _AcquireSlow();

			return instance;
		}

		public void Release(T obj)
		{
			if (_item == null)
				// Intentionally not using interlocked here. 
				// In a worst case scenario two objects may be stored into same slot.
				// It is very unlikely to happen and will only mean that one of the objects will get collected.
				_item = obj;
			else
				_ReleaseSlow(obj);
		}

		private T _AcquireSlow()
		{
			for (int i = 0; i < _items.Length; ++i)
			{
				// Note that the initial read is optimistically not synchronized. That is intentional. 
				// We will interlock only when we have a candidate. in a worst case we may miss some
				// recently returned objects. Not a big deal.
				var instance = _items[i];
				if (instance != null &&
				    instance == Interlocked.CompareExchange(ref _items[i], null!, instance))
					return instance;
			}

			return _builder();
		}

		private void _ReleaseSlow(T obj)
		{
			for (int i = 0; i < _items.Length; i++)
			{
				if (_items[i] == null)
				{
					// Intentionally not using interlocked here. 
					// In a worst case scenario two objects may be stored into same slot.
					// It is very unlikely to happen and will only mean that one of the objects will get collected.
					_items[i] = obj;
					break;
				}
			}
		}
	}
}
