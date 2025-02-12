using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  /// <summary>
  ///   Manages automated Kerbal EVA (Extra-Vehicular Activity) behavior.
  /// </summary>
  /// <remarks>
  ///   This class extends the base KerbalEVA functionality with AI capabilities including:
  ///   - Automated movement and navigation
  ///   - Construction tasks
  ///   - Pathfinding
  ///   - State management
  ///   The class uses a callback system to handle various events like:
  ///   - Task assignment/completion
  ///   - Movement control
  ///   - Target reaching
  ///   - Construction operations
  /// </remarks>
  public class MechBill : KerbalEVA {
    public delegate void ControlCallback(MechBill eva);

    private static readonly FieldInfo _constructionTargetField =
      typeof(KerbalEVA).GetField("constructionTarget", BindingFlags.NonPublic | BindingFlags.Instance);

    [SerializeField] private Vector3 HomeAirlock;
    [SerializeField] private bool GoingHome;

    // Debug visualization of control states after automation applied.
    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control Linear", isPersistant = false)]
    public Vector3 CtrlLinear;

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control PYR", isPersistant = false)]
    public Vector3 CtrlPyr;

    public float ApproachSpeedLimit = 1.0f;
    public float TgtApproachDistance = 1.0f;
    private Task _assignedTask;

    private GameObject _debugSphere;

    private Part _homePart;
    private Pathfinder _pathfinder;
    private List<Vector3> _pathToTarget;
    public Callback OnBuildCompleted = delegate { };

    // Callbacks for custom automation.
    public FlightInputCallback OnFlyByWire = st => { };
    public Callback OnTargetAssigned = delegate { };
    public Callback OnTargetReached = delegate { };

    /// <summary>
    ///   Called when a new task is assigned to the Kerbal
    /// </summary>
    public Callback OnTaskAssigned = delegate { };

    public Callback OnTaskCompleted = delegate { };
    public ControlCallback OnWalkByWire = eva => { };

    public Part HomePart {
      get => _homePart;
      set {
        _homePart = value;
        HomeAirlock = _homePart.transform.InverseTransformPoint(transform.position);
      }
    }

    // ReSharper disable once InconsistentNaming
    private Part constructionTarget {
      get => (Part)_constructionTargetField.GetValue(this);
      set => _constructionTargetField.SetValue(this, value);
    }

    public bool HasAITarget { get; private set; }

    public Task AssignedTask {
      get => _assignedTask;
      set {
        // vessel.targetObject = value;
        _assignedTask = value;
        OnTaskAssigned();
      }
    }

    public ITargetable Target {
      get => vessel.targetObject;
      set {
        // ReSharper disable once PossibleUnintendedReferenceComparison
        if (value != vessel.targetObject) {
          if (value != null) {
            _pathfinder = value.GetVessel().GetComponent<Pathfinder>();
            HasAITarget = true;
          } else {
            _pathfinder = null;
            HasAITarget = false;
          }

          GoingHome = false;
          _pathToTarget = null;
          vessel.targetObject = value;
          OnTargetAssigned();
        }
      }
    }

    private void Start() {
      OnBuildCompleted = ResetFsm;
    }

    protected override void HandleMovementInput() {
      if (_assignedTask != null) {
        _assignedTask.FixedUpdate();
      }

      if (HasAITarget || GoingHome) {
        // Ignore player input if this Kerbal is AI controlled
        DoAIMovement();
        return;
      }

      base.HandleMovementInput();
      OnWalkByWire(this);
      Quaternion localToWorld = transform.rotation;
      Quaternion worldToLocal = localToWorld.Inverse();

      FlightCtrlState ctrlState = new();
      Vector3 xyz = worldToLocal * packTgtRPos;
      ctrlState.X = xyz.x;
      ctrlState.Y = xyz.y;
      ctrlState.Z = xyz.z;

      Vector3 pyr = worldToLocal * cmdRot;
      ctrlState.pitch = pyr.x;
      ctrlState.yaw = pyr.y;
      ctrlState.roll = pyr.z;

      OnFlyByWire(ctrlState);

      xyz = ctrlState.GetXYZ();
      pyr = new Vector3(ctrlState.pitch, ctrlState.yaw,
        ctrlState.roll); // For some reason, GetPYR() switches around y and z.

      packTgtRPos = localToWorld * xyz;

      if (pyr != Vector3.zero) {
        manualAxisControl = true;
        cmdRot = localToWorld * pyr;
      } else {
        manualAxisControl = false;
      }

      // Update debug visualization
      CtrlLinear = xyz;
      CtrlPyr = pyr;
    }

    public void SetPackWaypoint(Vector3 tgtPos) {
      packTgtRPos = (tgtPos - transform.position).normalized;
    }

    private bool GetNextPathPoint(out Vector3 nextPoint) {
      nextPoint = Vector3.zero;
      if (_pathToTarget == null) {
        return false;
      }

      while (_pathToTarget.Count > 0) {
        nextPoint = _pathToTarget[_pathToTarget.Count - 1];
        if ((nextPoint - transform.position).magnitude > _pathfinder.GridElementSize) {
          return true;
        }

        _pathToTarget.RemoveAt(_pathToTarget.Count - 1);
      }

      return false;
    }

    private bool MoveToTarget() {
      bool reachedTarget = false;
      if (Globals.ShowDebugOverlay && _debugSphere == null) {
        _debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _debugSphere.GetComponent<Collider>().enabled = false;
        Destroy(_debugSphere.GetComponent<Rigidbody>());
        _debugSphere.transform.SetParent(transform);
        _debugSphere.transform.localScale = 0.1f * Vector3.one;
      }

      // Find a path to the target if we don't already have one.
      // FIXME: Need to navigate to point outside of large containers.
      _pathToTarget ??= _pathfinder.FindPath(transform.position,
        GoingHome ? HomePart.transform.TransformPoint(HomeAirlock) : vessel.targetObject.GetTransform().position,
        TgtApproachDistance);

      bool approachingTarget = !GetNextPathPoint(out Vector3 nextPoint);
      if (approachingTarget) {
        nextPoint = GoingHome
          ? HomePart.transform.TransformPoint(HomeAirlock)
          : vessel.targetObject.GetTransform().position; // FIXME: Make this less bespoke
      }

      if (Globals.ShowDebugOverlay && _debugSphere != null) {
        _debugSphere.transform.position = nextPoint;
      }

      Vector3 tgtRelativePosition = nextPoint - transform.position;
      tgtFwd = tgtRelativePosition.normalized;

      Vector3 goalVelocity;
      if (approachingTarget && (nextPoint - transform.position).magnitude <= TgtApproachDistance) {
        goalVelocity = Vector3.zero;
        reachedTarget = true;
      } else {
        goalVelocity = tgtFwd * ApproachSpeedLimit;
      }

      /*Debug.Log("Target Position: " + vessel.targetObject.GetTransform().position + "; Vessel Position: " +
                transform.position + "; Relative Position: " + tgtRelativePosition);
      Debug.Log("Target Velocity: " + vessel.targetObject.GetObtVelocity() + "; Vessel Velocity: " +
                part.orbit.GetVel() + "; Relative Velocity: " + tgtRelativeVelocity + "; Goal Velocity: " +
                goalVelocity);*/
      Vector3 tgtRelativeVelocity = part.orbit.GetVel() -
                                    (Vector3)(GoingHome
                                      ? HomePart.vessel.obt_velocity
                                      : vessel.targetObject.GetObtVelocity()); // FIXME
      Vector3 velError = goalVelocity - tgtRelativeVelocity;
      if (velError.sqrMagnitude > 1.0f) {
        velError.Normalize();
      }

      packTgtRPos = velError;
      return reachedTarget;
    }

    private void DoAIMovement() {
      if (OnALadder) {
        fsm.RunEvent(On_ladderLetGo);
      }

      if (!JetpackDeployed) {
        ToggleJetpack(true);
      }

      if (MoveToTarget()) {
        if (GoingHome) {
          // TODO: Better separate this logic from move to target logic.
          // TODO: Check conditions to board and handle cases where we can't board.
          BoardPart(HomePart);
          // FIXME: Need to get crew display in the corner to update properly.
        } else {
          vessel.targetObject = null;
          HasAITarget = false;
          OnTargetReached();
        }
      }
    }

    public override void OnAwake() {
      // Replace the stock KerbalEVA class with our subclass.
      ModuleAttributes.classID = "KerbalEVA".GetHashCode();
      base.OnAwake();
    }

    /// <summary>
    ///   Navigates the Kerbal back to their home part
    /// </summary>
    /// <remarks>
    ///   This method will clear current targets, set the going home state,
    ///   and initialize pathfinding to the home airlock
    /// </remarks>
    public void GoHome() {
      // TODO: Check if there are any more available tasks and only go home if there is nothing more to do.
      // FIXME: Make this less repetitive with Target.set.
      Target = null;
      GoingHome = true;
      _pathfinder = _homePart.vessel.GetComponent<Pathfinder>();
      _pathToTarget = null;
      TgtApproachDistance = 1.0f;
      // TODO: Have Kerbal board ship on reaching airlock.
    }

    /*   public void StartWelding(KFSMState st) {
         fsm.RunEvent(On_weldStart);
       }*/

    /*   protected override void SetupFSM() {
         base.SetupFSM();
         //st_enteringConstruction.OnLeave += StartWelding; //new KFSMEventCallback(fsm, On_weldStart);
       }*/

    private void ExitConstructionMode() {
      InConstructionMode = false;
      if (!OnALadder) {
        //fsm.RunEvent(On_constructionModeExit);
        InputLockManager.RemoveControlLock("WeldLock_" + vessel.id);
      }
    }

    private void ResetFsm() {
      On_constructionModeTrigger_fl_Complete.GoToStateOnEvent = st_idle_fl;
      On_weldComplete.OnEvent -= ExitConstructionMode;
      On_weldComplete.GoToStateOnEvent = st_idle_gr;
      st_exitingConstruction.OnLeave -= delegate { OnBuildCompleted(); };
    }

    public new void Weld(Part target) {
      // Enter construction mode, then weld.
      InConstructionMode = true;
      constructionTarget = target;
      On_constructionModeTrigger_fl_Complete.GoToStateOnEvent = st_weldAcquireHeading;
      On_weldComplete.OnEvent += ExitConstructionMode;
      On_weldComplete.GoToStateOnEvent = st_exitingConstruction;
      st_exitingConstruction.OnLeave += delegate { OnBuildCompleted(); };
      fsm.RunEvent(On_constructionModeEnter);
    }
  }
}