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

      float sqrRadius = Mathf.FloorToInt(radius * radius / (_gridElementSize * _gridElementSize));

      var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
      var visited = new HashSet<Vector3Int> { startPoint };
      var openSet = new HeapDict<Vector3Int, float> { { startPoint, 0.0f } };

      while (openSet.Count > 0) {
        HeapDict<Vector3Int, float>.MutableKeyValuePair nextNode = openSet.Dequeue();
        Vector3Int delta = new();
        for (delta.x = -1; delta.x <= 1; ++delta.x) {
          for (delta.y = -1; delta.y <= 1; ++delta.y) {
            for (delta.z = -1; delta.z <= 1; ++delta.z) {
              Vector3Int otherPoint = nextNode.Key + delta;
              if (InBounds(otherPoint) && !_isObstructed[otherPoint.x, otherPoint.y, otherPoint.z] &&
                  !visited.Contains(otherPoint)) {
                cameFrom[otherPoint] = nextNode.Key;
                if ((otherPoint - endPoint).sqrMagnitude <= sqrRadius) {
                  List<Vector3> result = new();
                  bool hasNext;
                  do {
                    result.Add(GridToWorld(otherPoint));
                    hasNext = cameFrom.TryGetValue(otherPoint, out otherPoint);
                  } while (hasNext);

                  return result;
                }

                openSet[otherPoint] = nextNode.Value + delta.magnitude;
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

    public Vector3 GridToWorld(Vector3Int point) {
      return Transform.TransformPoint(_gridElementSize * (point - _gridSize * Vector3.one / 2.0f));
    }

    public Vector3Int WorldToGrid(Vector3 point) {
      return Vector3Int.RoundToInt(Transform.InverseTransformPoint(point) / _gridElementSize +
                                   _gridSize * Vector3.one / 2.0f);
    }
  }
}
