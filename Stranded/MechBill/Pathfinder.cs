using System;
using System.Collections.Generic;
using Stranded.Util;
using UnityEngine;

namespace Stranded.MechBill {
  public class Pathfinder : VesselModule {
    private static readonly float[] _sqrts = { 1.0f, Mathf.Sqrt(2.0f), Mathf.Sqrt(3.0f) };

    private static readonly int _mode = Shader.PropertyToID("_Mode");
    private Bounds _bounds;

    private GameObject _debugOverlay;
    private ParticleSystem.Particle[] _debugParticles;
    private ParticleSystem _debugParticleSystem;
    private bool _failedPathfinding;
    private const float _gridElementPadding = 1.5f;

    //private Vector3 _gridExtents;
    //private readonly Vector3 _gridElementSize;
    //private readonly Vector3 _invGridElementSize;
    private float _gridElementSize;
    private bool _gridNeedsRebuild = true;

    private Vector3Int _gridSize;

    private bool[,,] _isObstructed;

    public float GridElementSize {
      get => _gridElementSize;
      set {
        _gridElementSize = value;
        _gridNeedsRebuild = true;
      }
    }

    public Vector3Int GridSize {
      get => _gridSize;
      set {
        _gridSize = value;
        _gridNeedsRebuild = true;
      }
    }

    // TODO: Subscribe to OnVesselStandardModification
    protected override void OnStart() {
      _bounds = vessel.CalculateCraftBounds();
      // Add padding to ensure we have room to move around the outside if we need to.
      _bounds.Expand(5f);
      //Bounds globalBounds = gameObject.GetColliderBounds();
      //_bounds = new Bounds(transform.InverseTransformPoint(globalBounds.center), transform.InverseTransformVector(globalBounds.size));
      Debug.Log("Bounds are " + _bounds + ", position is " + transform.position);
      // _gridExtents = vessel.vesselSize + Vector3.one * 2f;
      //_gridElementSize = _gridExtents / _gridSize;
      //_invGridElementSize = new Vector3(1f / _gridElementSize.x, 1f / _gridElementSize.y, 1f / _gridElementSize.z)
      //GridElementSize = (extents.magnitude + 5f) / _gridSize;
      GridElementSize = 0.2f;
      GridSize = Vector3Int.RoundToInt(_bounds.size / GridElementSize);
    }

    private int Sqr(int x) {
      return x * x;
    }

    private float Sqr(float x) {
      return x * x;
    }

   /* private void Update() {
      // Visualize the vessel axes and bounds axes
      GLUtils.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.blue, 0f, false);
      Debug.DrawLine(transform.position, transform.position + transform.up * 5f, Color.green, 0f, false);
      Debug.DrawLine(transform.position, transform.position + transform.right * 5f, Color.red, 0f, false);
    
      Vector3 center = transform.TransformPoint(_bounds.center);
      Vector3 extents = transform.TransformVector(_bounds.extents);
      Debug.DrawLine(center, center + extents, Color.yellow, 0f, false);
      Debug.DrawLine(center, center - extents, Color.magenta, 0f, false);
    }*/
   
   private GameObject _debugLines;
   private void CreateDebugLines() {
     if (_debugLines == null) {
       _debugLines = new GameObject("Debug Lines");
       _debugLines.transform.SetParent(transform, false);

       // We need 5 lines total
       for (int i = 0; i < 5; i++) {
         GameObject line = new ($"Line {i}");
         line.transform.SetParent(_debugLines.transform);
         LineRenderer renderer = line.AddComponent<LineRenderer>();
         renderer.useWorldSpace = false;
         renderer.startWidth = 0.1f;
         renderer.endWidth = 0.1f;
         renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
         renderer.SetPositions(new[] { Vector3.zero, Vector3.zero }); // Will update these later
       }
     }

     // Update line positions
     LineRenderer[] lines = _debugLines.GetComponentsInChildren<LineRenderer>();
    
     // Vessel axes - in local space since the lines are children of transform
     lines[0].startColor = lines[0].endColor = Color.blue;
     lines[0].SetPosition(0, Vector3.zero);
     lines[0].SetPosition(1, Vector3.forward * 5f);
    
     lines[1].startColor = lines[1].endColor = Color.green;
     lines[1].SetPosition(0, Vector3.zero);
     lines[1].SetPosition(1, Vector3.up * 5f);
    
     lines[2].startColor = lines[2].endColor = Color.red;
     lines[2].SetPosition(0, Vector3.zero);
     lines[2].SetPosition(1, Vector3.right * 5f);
    
     lines[3].startColor = lines[3].endColor = Color.yellow;
     lines[3].SetPosition(0, _bounds.center);
     lines[3].SetPosition(1, _bounds.max);
    
     lines[4].startColor = lines[4].endColor = Color.magenta;
     lines[4].SetPosition(0, _bounds.center);
     lines[4].SetPosition(1, _bounds.min);
   }

    private void CreateDebugOverlay() {
      if (_debugOverlay == null) {
        _debugOverlay = new GameObject("Pathfinder Debug Overlay");
        _debugOverlay.transform.SetParent(transform, false);
        _debugParticleSystem = _debugOverlay.AddComponent<ParticleSystem>();
      } else {
        _debugParticleSystem = _debugOverlay.GetComponent<ParticleSystem>();
        return;
      }

      ParticleSystem.MainModule main = _debugParticleSystem.main;
      main.startLifetime = float.PositiveInfinity;
      //main.startSize = _gridElementSize.magnitude / 4f;
      main.startSize = _gridElementSize / 2f;
      main.maxParticles = _gridSize.x * _gridSize.y * _gridSize.z;
      ParticleSystem.EmissionModule emission = _debugParticleSystem.emission;
      emission.enabled = false;

      //Vector3 extents = _gridElementSize * Vector3.one;
      Vector3Int point = new();

      _debugParticles = new ParticleSystem.Particle[_gridSize.x * _gridSize.y * _gridSize.z];
      int index = 0;
      for (point.x = 0; point.x < _gridSize.x; ++point.x) {
        for (point.y = 0; point.y < _gridSize.y; ++point.y) {
          for (point.z = 0; point.z < _gridSize.z; ++point.z) {
            _debugParticles[index++] = new ParticleSystem.Particle {
              position = GridToLocal(point),
              startColor = _isObstructed[point.x, point.y, point.z]
                ? new Color(1.0f, 0.0f, 0.0f, 0.2f)
                : new Color(0.0f, 1.0f, 0.0f, 0.1f),
              startSize = _gridElementSize / 2f,
              startLifetime = float.PositiveInfinity
            };
          }
        }
      }

      _debugParticleSystem.SetParticles(_debugParticles, _debugParticles.Length);
      _debugOverlay.SetLayerRecursive(Globals.GhostLayer);
      ParticleSystemRenderer renderer = _debugOverlay.GetComponent<ParticleSystemRenderer>();
      renderer.material =
        (Resources.Load("Effects/fx_smokeTrail_light", typeof(ParticleSystemRenderer)) as ParticleSystemRenderer)
        .material;
      CreateDebugLines();
    }

    public List<Vector3> FindPath(Vector3 start, Vector3 end, float radius) {
      if (_failedPathfinding) return null;
      // TODO: Optimize this better to reduce pathfinding lag.
      if (_gridNeedsRebuild) {
        RebuildCollisionGrid();
      }

      Vector3Int startPoint = WorldToGrid(start);
      if (!InBounds(startPoint)) {
        throw new ArgumentOutOfRangeException(nameof(start), start,
          "Point is outside of grid. Grid coords " + startPoint + " valid values are 0.." +
          (_gridSize - Vector3Int.one));
      }

      Vector3Int endPoint = WorldToGrid(end);
      if (!InBounds(endPoint)) {
        throw new ArgumentOutOfRangeException(nameof(end), end,
          "Point is outside of grid. Grid coords " + endPoint + " valid values are 0.." + (_gridSize - Vector3Int.one));
      }

      int endX = endPoint.x;
      int endY = endPoint.y;
      int endZ = endPoint.z;

      int sqrRadius = Mathf.FloorToInt(Sqr(radius / _gridElementSize / transform.lossyScale.x));
      
      int paddingRadius = Mathf.CeilToInt(_gridElementPadding / _gridElementSize);
      Vector3Int point = Vector3Int.zero;
      // Find the nearest unobstructed point to start
      float nearestDistance = float.MaxValue;
      Vector3Int startingPoint = startPoint;
      for (point.x = startPoint.x - paddingRadius; point.x <= startPoint.x + paddingRadius; ++point.x) {
        for (point.y = startPoint.y - paddingRadius; point.y <= startPoint.y + paddingRadius; ++point.y) {
          for (point.z = startPoint.z - paddingRadius; point.z <= startPoint.z + paddingRadius; ++point.z) {
            if (InBounds(point) && !_isObstructed[point.x, point.y, point.z]) {
              float distance = (point - startPoint).sqrMagnitude; // Using sqrMagnitude to avoid sqrt
              if (distance < nearestDistance) {
                nearestDistance = distance;
                startingPoint = point;
              }
            }
          }
        }
      }
      
      // If no unobstructed point was found
      if (nearestDistance == float.MaxValue) {
        Debug.LogError("Cannot find unobstructed point within " + paddingRadius + " units of from " + start + " (grid " + startPoint + ").");
        _failedPathfinding = true;
        return null;
      }
      
      Dictionary<(int, int, int), (int, int, int)> cameFrom = new();
      //var visited = new HashSet<ValueTuple<int, int, int>> { (startPoint.x, startPoint.y, startPoint.z) };
      bool[,,] visited = new bool[_gridSize.x, _gridSize.y, _gridSize.z];
      visited[startingPoint.x, startingPoint.y, startingPoint.z] = true;
      PriorityQueue<(int, int, int), float> openSet = new();
      openSet.Add((startingPoint.x, startingPoint.y, startingPoint.z), 0.0f);

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
                float distance = _sqrts[intDistance - 1];
                int endDistance = Sqr(otherPoint.Item1 - endX) +
                                  Sqr(otherPoint.Item2 - endY) +
                                  Sqr(otherPoint.Item3 - endZ);
                if (endDistance <= sqrRadius) {
                  List<Vector3> result = new();
                  bool hasNext;
                  do {
                    point = new Vector3Int(otherPoint.Item1, otherPoint.Item2, otherPoint.Item3);
                    if (Globals.ShowDebugOverlay) {
                      // FIXME: Need to reset particle colors after path is finished with.
                      _debugParticles[FlattenGrid(point)].startColor = Color.white;
                      _debugParticleSystem.SetParticles(_debugParticles, _debugParticles.Length);
                    }

                    result.Add(GridToWorld(point));
                    hasNext = cameFrom.TryGetValue(otherPoint, out otherPoint);
                  } while (hasNext);

                  return result;
                }

                openSet.Add(otherPoint, nextNode.Value + distance + Mathf.Sqrt(endDistance));
                visited[otherPoint.Item1, otherPoint.Item2, otherPoint.Item3] = true;
              }
            }
          }
        }
      }

      // Pathfinding failed. We should probably do something about that.
      Debug.LogError("Cannot find path from " + start + " (grid " + startPoint + ") to " + end + " (grid " + endPoint + ").");
      Globals.ShowDebugOverlay = true;
      CreateDebugOverlay();
      _failedPathfinding = true;
      return null;
    }

    protected void RebuildCollisionGrid() {
      _isObstructed = new bool[_gridSize.x, _gridSize.y, _gridSize.z];
      //Vector3 halfExtents = _gridElementSize / 2.0f;
      // Intentionally make hitbox larger to make sure we don't fly too close to anything. 
      Vector3 halfExtents = (_gridElementSize + _gridElementPadding) * Vector3.one / 2.0f;
      Vector3Int point = new();
      for (point.x = 0; point.x < _gridSize.x; ++point.x) {
        for (point.y = 0; point.y < _gridSize.y; ++point.y) {
          for (point.z = 0; point.z < _gridSize.z; ++point.z) {
            Vector3 worldPos = GridToWorld(point);
            bool isBlocked = Physics.CheckBox(worldPos, halfExtents, transform.rotation, Part.layerMask);
            /*if (isBlocked) {
              // Add debug to see what's being hit
              Collider[] hits = Physics.OverlapBox(worldPos, halfExtents);
              Debug.Log($"Grid point {point} (world pos {worldPos}) is blocked by: {string.Join(", ", hits.Select(h => $"{h.gameObject.name} at {h.transform.position}"))}");
            }*/
            _isObstructed[point.x, point.y, point.z] = isBlocked;
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
        if (point[i] < 0 || point[i] >= _gridSize[i]) {
          return false;
        }
      }

      return true;
    }

    public bool InBounds(ValueTuple<int, int, int> point) {
      return point.Item1 >= 0 && point.Item1 < _gridSize.x &&
             point.Item2 >= 0 && point.Item2 < _gridSize.y &&
             point.Item3 >= 0 && point.Item3 < _gridSize.z;
    }

    public Vector3 GridToLocal(Vector3Int point) {
      return _gridElementSize * (Vector3)point + _bounds.min;
    }

    public Vector3Int LocalToGrid(Vector3 point) {
      return Vector3Int.RoundToInt((point - _bounds.min) / _gridElementSize);
    }

    public Vector3 GridToWorld(Vector3Int point) {
      //return transform.TransformPoint(Vector3.Scale(_gridElementSize, point - _gridSize * Vector3.one / 2.0f));
      //return transform.TransformPoint(_gridElementSize * (Vector3)(point - _gridSize / 2) + transform.InverseTransformPoint(_bounds.center));
      return transform.TransformPoint(GridToLocal(point));
    }

    public int FlattenGrid(Vector3Int point) {
      return _gridSize.z * (_gridSize.y * point.x + point.y) + point.z;
    }

    public Vector3Int WorldToGrid(Vector3 point) {
      //return Vector3Int.RoundToInt(Vector3.Scale(transform.InverseTransformPoint(point), _invGridElementSize) +
      //return Vector3Int.RoundToInt((transform.InverseTransformPoint(point) - transform.InverseTransformPoint(_bounds.center)) / _gridElementSize) +
      //       _gridSize / 2;
      return LocalToGrid(transform.InverseTransformPoint(point));
    }
  }
}