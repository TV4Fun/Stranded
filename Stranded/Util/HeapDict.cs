using System;
using System.Collections.Generic;

namespace Stranded.Util {
  public class HeapDict<TKey, TValue> : Dictionary<TKey, TValue> where TValue : IComparable<TValue> {
    public class MutableKeyValuePair {
      public readonly TKey Key;
      public TValue Value;

      public MutableKeyValuePair(TKey key, TValue value) {
        Key = key;
        Value = value;
      }
    }

    private readonly List<MutableKeyValuePair> _heap = new();
    private readonly Dictionary<TKey, int> _indices = new();

    void UpdateIndex(int index) {
      _indices[_heap[index].Key] = index;
    }

    private void SwapIndices(int v1, int v2) {
      (_heap[v1], _heap[v2]) = (_heap[v2], _heap[v1]);
      UpdateIndex(v1);
      UpdateIndex(v2);
    }

    private void UpHeapify(int index, TValue value) {
      while (index > 0) {
        int parentIndex = (index - 1) >> 1;
        if (value.CompareTo(_heap[parentIndex].Value) >= 0) {
          return;
        }

        SwapIndices(index, parentIndex);
        index = parentIndex;
      }
    }

    private void DownHeapify(int index, TValue value) {
      while (true) {
        int child1 = (index << 1) + 1;
        int child2 = (index << 1) + 2;
        if (child1 >= _heap.Count) {
          return;
        }

        int child;
        if (child2 >= _heap.Count) {
          child = child1;
        } else {
          child = _heap[child1].Value.CompareTo(_heap[child2].Value) <= 0 ? child1 : child2;
        }

        if (value.CompareTo(_heap[child].Value) <= 0) {
          return;
        }

        SwapIndices(index, child);
        index = child;
      }
    }

    private void DownHeapify(int index) {
      DownHeapify(index, _heap[index].Value);
    }

    public new TValue this[TKey key] {
      get => base[key];

      set {
        bool exists = TryGetValue(key, out TValue oldValue);
        if (exists) {
          int comparison = value.CompareTo(oldValue);
          if (comparison == 0) {
            return;
          }

          base[key] = value;
          int index = _indices[key];
          _heap[index].Value = value;
          if (comparison < 0) {
            UpHeapify(index, value);
          } else {
            DownHeapify(index, value);
          }
        } else {
          Add(key, value);
        }
      }
    }

    public new void Add(TKey key, TValue value) {
      base.Add(key, value);
      int index = _heap.Count;
      _heap.Add(new MutableKeyValuePair(key, value));
      _indices.Add(key, index);
      UpHeapify(index, value);
    }

    public new void Clear() {
      base.Clear();
      _heap.Clear();
      _indices.Clear();
    }

    public new bool Remove(TKey key) {
      bool result = TryGetValue(key, out TValue oldValue);
      if (!result) {
        return false;
      }
      base.Remove(key);
      int index = _indices[key];
      _indices.Remove(key);
      _heap[index] = _heap[_heap.Count - 1];
      _heap.RemoveAt(_heap.Count - 1);
      if (_heap.Count > index) {
        UpdateIndex(index);
        TValue newValue = _heap[index].Value;
        if (newValue.CompareTo(oldValue) > 0) {
          DownHeapify(index, newValue);
        }
      }

      return true;
    }

    public MutableKeyValuePair Dequeue() {
      MutableKeyValuePair nextItem = _heap[0];
      _heap[0] = _heap[_heap.Count - 1];
      _heap.RemoveAt(_heap.Count - 1);
      _indices.Remove(nextItem.Key);
      base.Remove(nextItem.Key);
      if (_heap.Count > 0) {
        UpdateIndex(0);
        DownHeapify(0);
      }

      return nextItem;
    }

    public TKey EnqueueDequeue(TKey key, TValue value) {
      if (value.CompareTo(_heap[0].Value) < 0) {
        return key;
      }

      return DequeueEnqueue(key, value);
    }

    public TKey DequeueEnqueue(TKey key, TValue value) {
      TKey nextItem = _heap[0].Key;
      _heap[0] = new MutableKeyValuePair(key, value);
      UpdateIndex(0);
      _indices.Remove(nextItem);
      base.Remove(nextItem);
      DownHeapify(0, value);
      return nextItem;
    }
  }
}
