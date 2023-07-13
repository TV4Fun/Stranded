using System;
using System.Collections.Generic;
using Stranded.Util;
using UnityEngine;

namespace Stranded.MechBill {
  public class Pathfinder {
    private bool _gridNeedsRebuild = true;

    private int _gridSize = 80;

    //private Vector3 _gridExtents;
    //private readonly Vector3 _gridElementSize;
    //private readonly Vector3 _invGridElementSize;
    private float _gridElementSize;

    private bool[,,] _isObstructed;

    private GameObject _debugOverlay;

    public Transform Transform;
    private Vessel _vessel;
    private static readonly int _mode = Shader.PropertyToID("_Mode");
    private ParticleSystem.Particle[] _particles;
    private ParticleSystem _particleSystem;

    // TODO: Subscribe to OnVesselStandardModification

    public Pathfinder(Vessel vessel) {
      _vessel = vessel;
      Vector3 extents = vessel.vesselSize;
      // _gridExtents = vessel.vesselSize + Vector3.one * 2f;
      //_gridElementSize = _gridExtents / _gridSize;
      //_invGridElementSize = new Vector3(1f / _gridElementSize.x, 1f / _gridElementSize.y, 1f / _gridElementSize.z)
      GridElementSize = (extents.magnitude + 2f) / _gridSize;
    }

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

    private void CreateDebugOverlay() {
      if (_debugOverlay == null) {
        _debugOverlay = new GameObject("Pathfinder Debug Overlay");
        _debugOverlay.transform.SetParent(Transform);
        _particleSystem = _debugOverlay.AddComponent<ParticleSystem>();
      } else {
        _particleSystem = _debugOverlay.GetComponent<ParticleSystem>();
      }

      ParticleSystem.MainModule main = _particleSystem.main;
      main.startLifetime = float.PositiveInfinity;
      //main.startSize = _gridElementSize.magnitude / 4f;
      main.startSize = _gridElementSize / 2f;
      main.maxParticles = _gridSize * _gridSize * _gridSize;
      ParticleSystem.EmissionModule emission = _particleSystem.emission;
      emission.enabled = false;

      //Vector3 extents = _gridElementSize * Vector3.one;
      Vector3Int point = new();

      _particles = new ParticleSystem.Particle[_gridSize * _gridSize * _gridSize];
      int index = 0;
      for (point.x = 0; point.x < _gridSize; ++point.x) {
        for (point.y = 0; point.y < _gridSize; ++point.y) {
          for (point.z = 0; point.z < _gridSize; ++point.z) {
            _particles[index++] = new ParticleSystem.Particle {
                position = GridToWorld(point),
                startColor = _isObstructed[point.x, point.y, point.z]
                    ? new Color(1.0f, 0.0f, 0.0f, 0.2f)
                    : new Color(0.0f, 1.0f, 0.0f, 0.1f),
                startSize = _gridElementSize / 2f,
                startLifetime = float.PositiveInfinity
            };
          }
        }
      }

      _particleSystem.SetParticles(_particles, _particles.Length);
      _debugOverlay.SetLayerRecursive(Globals.GhostLayer);
      ParticleSystemRenderer renderer = _debugOverlay.GetComponent<ParticleSystemRenderer>();
      renderer.material =
          (Resources.Load("Effects/fx_smokeTrail_light", typeof(ParticleSystemRenderer)) as ParticleSystemRenderer)
          .material;
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

      int endX = endPoint.x;
      int endY = endPoint.y;
      int endZ = endPoint.z;

      int sqrRadius = Mathf.FloorToInt(Sqr(radius / _gridElementSize / Transform.lossyScale.x));

      var cameFrom = new Dictionary<ValueTuple<int, int, int>, ValueTuple<int, int, int>>();
      //var visited = new HashSet<ValueTuple<int, int, int>> { (startPoint.x, startPoint.y, startPoint.z) };
      bool[,,] visited = new bool[_gridSize, _gridSize, _gridSize];
      visited[startPoint.x, startPoint.y, startPoint.z] = true;
      var openSet = new PriorityQueue<ValueTuple<int, int, int>, float>();
      openSet.Add((startPoint.x, startPoint.y, startPoint.z), 0.0f);

      float[] sqrts = { 1.0f, Mathf.Sqrt(2.0f), Mathf.Sqrt(3.0f) };

      while (!openSet.IsEmpty()) {
        PriorityQueue<(int, int, int), float>.MutableKeyValuePair nextNode = openSet.Dequeue();
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
                  !visited[otherPoint.Item1, otherPoint.Item2, otherPoint.Item3]) {
                cameFrom[otherPoint] = nextNode.Key;
                int intDistance = Math.Abs(otherPoint.Item1 - nextNode.Key.Item1) +
                                  Math.Abs(otherPoint.Item2 - nextNode.Key.Item2) +
                                  Math.Abs(otherPoint.Item3 - nextNode.Key.Item3);
                float distance = sqrts[intDistance - 1];
                int endDistance = Sqr(otherPoint.Item1 - endX) +
                                  Sqr(otherPoint.Item2 - endY) +
                                  Sqr(otherPoint.Item3 - endZ);
                if (endDistance <= sqrRadius) {
                  List<Vector3> result = new();
                  bool hasNext;
                  do {
                    Vector3Int point = new Vector3Int(otherPoint.Item1, otherPoint.Item2, otherPoint.Item3);
                    if (Globals.ShowDebugOverlay) {
                      _particles[FlattenGrid(point)].startColor = Color.white;
                      _particleSystem.SetParticles(_particles, _particles.Length);
                    }

                    result.Add(GridToWorld(point));
                    hasNext = cameFrom.TryGetValue(otherPoint, out otherPoint);
                  } while (hasNext);

                  return result;
                }

                openSet.Add(otherPoint, nextNode.Value + distance);
                visited[otherPoint.Item1, otherPoint.Item2, otherPoint.Item3] = true;
              }
            }
          }
        }
      }

      return null;
    }

    protected void RebuildCollisionGrid() {
      _isObstructed = new bool[_gridSize, _gridSize, _gridSize];
      //Vector3 halfExtents = _gridElementSize / 2.0f;
      Vector3 halfExtents = _gridElementSize * Vector3.one / 2.0f;
      Vector3Int point = new();
      for (point.x = 0; point.x < _gridSize; ++point.x) {
        for (point.y = 0; point.y < _gridSize; ++point.y) {
          for (point.z = 0; point.z < _gridSize; ++point.z) {
            _isObstructed[point.x, point.y, point.z] = Physics.CheckBox(GridToWorld(point), halfExtents);
          }
        }
      }

      if (Globals.ShowDebugOverlay) {
        CreateDebugOverlay();
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
      //return Transform.TransformPoint(Vector3.Scale(_gridElementSize, point - _gridSize * Vector3.one / 2.0f));
      return Transform.TransformPoint(_gridElementSize * (point - _gridSize * Vector3.one / 2.0f));
    }

    public int FlattenGrid(Vector3Int point) {
      return Sqr(_gridSize) * point.x + _gridSize * point.y + point.z;
    }

    public Vector3Int WorldToGrid(Vector3 point) {
      //return Vector3Int.RoundToInt(Vector3.Scale(Transform.InverseTransformPoint(point), _invGridElementSize) +
      return Vector3Int.RoundToInt(Transform.InverseTransformPoint(point) / _gridElementSize +
                                   _gridSize * Vector3.one / 2.0f);
    }
  }
}
