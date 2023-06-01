using System;
using System.Collections.Generic;

namespace Stranded.Util {
  public class PriorityQueue<TKey, TValue> where TValue : IComparable<TValue> {
    public class MutableKeyValuePair {
      public readonly TKey Key;
      public TValue Value;

      public MutableKeyValuePair(TKey key, TValue value) {
        Key = key;
        Value = value;
      }
    }

    private readonly List<MutableKeyValuePair> _heap = new();

    private void SwapValues(int v1, int v2) {
      (_heap[v1], _heap[v2]) = (_heap[v2], _heap[v1]);
    }

    private void UpHeapify(int index, TValue value) {
      while (index > 0) {
        int parentIndex = (index - 1) >> 1;
        if (value.CompareTo(_heap[parentIndex].Value) >= 0) {
          return;
        }

        SwapValues(index, parentIndex);
        index = parentIndex;
      }
    }

    public bool IsEmpty() => _heap.Count == 0;

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

        SwapValues(index, child);
        index = child;
      }
    }

    private void DownHeapify(int index) {
      DownHeapify(index, _heap[index].Value);
    }

    public void Add(TKey key, TValue value) {
      int index = _heap.Count;
      _heap.Add(new MutableKeyValuePair(key, value));
      UpHeapify(index, value);
    }

    public MutableKeyValuePair Dequeue() {
      MutableKeyValuePair nextItem = _heap[0];
      _heap[0] = _heap[_heap.Count - 1];
      _heap.RemoveAt(_heap.Count - 1);
      if (_heap.Count > 0) {
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
      DownHeapify(0, value);
      return nextItem;
    }
  }
}
