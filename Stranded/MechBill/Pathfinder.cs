using System;
using System.Collections.Generic;
using Stranded.Util;
using UnityEngine;

namespace Stranded.MechBill {
  public class Pathfinder {
    private bool _gridNeedsRebuild = true;

    private float _gridElementSize = 0.5f;
    private int _gridSize = 150;
    private bool[,,] _isObstructed;

    public Transform Transform;

    public float GridElementSize {
      get => _gridElementSize;
      set {
        _gridElementSize = value;
        _gridNeedsRebuild = true;
      }
    }

    public int GridSize {
      get => _gridSize;
      set {
        _gridSize = value;
        _gridNeedsRebuild = true;
      }
    }

    private int Sqr(int x) => x * x;

    private float Sqr(float x) => x * x;

    public List<Vector3> FindPath(Vector3 start, Vector3 end, float radius) {
      if (_gridNeedsRebuild) {
        RebuildCollisionGrid();
      }

      Vector3Int startPoint = WorldToGrid(start);
      if (!InBounds(startPoint)) {
        throw new ArgumentOutOfRangeException(nameof(start), start,
            "Point is outside of grid. Grid coords " + startPoint + " valid values are 0.." + (_gridSize - 1));
      }

      Vector3Int endPoint = WorldToGrid(end);
      if (!InBounds(endPoint)) {
        throw new ArgumentOutOfRangeException(nameof(end), end,
            "Point is outside of grid. Grid coords " + endPoint + " valid values are 0.." + (_gridSize - 1));
      }

      int sqrRadius = Mathf.FloorToInt(Sqr(radius / _gridElementSize / Transform.lossyScale.x));

      var cameFrom = new Dictionary<ValueTuple<int, int, int>, ValueTuple<int, int, int>>();
      var visited = new HashSet<ValueTuple<int, int, int>> { (startPoint.x, startPoint.y, startPoint.z) };
      var openSet = new HeapDict<ValueTuple<int, int, int>, float>
          { { (startPoint.x, startPoint.y, startPoint.z), 0.0f } };

      float[] sqrts = { 1.0f, Mathf.Sqrt(2.0f), Mathf.Sqrt(3.0f) };

      while (openSet.Count > 0) {
        HeapDict<(int, int, int), float>.MutableKeyValuePair nextNode = openSet.Dequeue();
        ValueTuple<int, int, int> otherPoint;
        for (otherPoint.Item1 = nextNode.Key.Item1 - 1;
             otherPoint.Item1 <= nextNode.Key.Item1 + 1;
             ++otherPoint.Item1) {
          for (otherPoint.Item2 = nextNode.Key.Item2 - 1;
               otherPoint.Item2 <= nextNode.Key.Item2 + 1;
               ++otherPoint.Item2) {
            for (otherPoint.Item3 = nextNode.Key.Item3 - 1;
                 otherPoint.Item3 <= nextNode.Key.Item3 + 1;
                 ++otherPoint.Item3) {
              if (InBounds(otherPoint) && !_isObstructed[otherPoint.Item1, otherPoint.Item2, otherPoint.Item3] &&
                  otherPoint != nextNode.Key && !visited.Contains(otherPoint)) {
                cameFrom[otherPoint] = nextNode.Key;
                int intDistance = Math.Abs(otherPoint.Item1 - nextNode.Key.Item1) +
                                  Math.Abs(otherPoint.Item2 - nextNode.Key.Item2) +
                                  Math.Abs(otherPoint.Item3 - nextNode.Key.Item3);
                float distance = sqrts[intDistance - 1];
                int endDistance = Sqr(otherPoint.Item1 - nextNode.Key.Item1) +
                                  Sqr(otherPoint.Item2 - nextNode.Key.Item2) +
                                  Sqr(otherPoint.Item3 - nextNode.Key.Item3);
                if (endDistance <= sqrRadius) {
                  List<Vector3> result = new();
                  bool hasNext;
                  do {
                    result.Add(GridToWorld(new Vector3Int(otherPoint.Item1, otherPoint.Item2, otherPoint.Item3)));
                    hasNext = cameFrom.TryGetValue(otherPoint, out otherPoint);
                  } while (hasNext);

                  return result;
                }

                openSet[otherPoint] = nextNode.Value + distance;
                visited.Add(otherPoint);
              }
            }
          }
        }
      }

      return null;
    }

    protected void RebuildCollisionGrid() {
      _isObstructed = new bool[_gridSize, _gridSize, _gridSize];
      Vector3 halfExtents = _gridElementSize * Vector3.one / 2.0f;
      Vector3Int point = new Vector3Int();
      for (point.x = 0; point.x < _gridSize; ++point.x) {
        for (point.y = 0; point.y < _gridSize; ++point.y) {
          for (point.z = 0; point.z < _gridSize; ++point.z) {
            _isObstructed[point.x, point.y, point.z] = Physics.CheckBox(GridToWorld(point), halfExtents);
          }
        }
      }

      _gridNeedsRebuild = false;
    }

    public bool InBounds(Vector3Int point) {
      for (int i = 0; i < 3; ++i) {
        if (point[i] < 0 || point[i] >= _gridSize) {
          return false;
        }
      }

      return true;
    }

    public bool InBounds(ValueTuple<int, int, int> point) =>
        point.Item1 >= 0 && point.Item1 < _gridSize &&
        point.Item2 >= 0 && point.Item2 < _gridSize &&
        point.Item3 >= 0 && point.Item3 < _gridSize;

    public Vector3 GridToWorld(Vector3Int point) {
      return Transform.TransformPoint(_gridElementSize * (point - _gridSize * Vector3.one / 2.0f));
    }

    public Vector3Int WorldToGrid(Vector3 point) {
      return Vector3Int.RoundToInt(Transform.InverseTransformPoint(point) / _gridElementSize +
                                   _gridSize * Vector3.one / 2.0f);
    }
  }
}
