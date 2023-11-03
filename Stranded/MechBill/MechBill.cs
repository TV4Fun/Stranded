using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBill : KerbalEVA {
    private Task _assignedTask = null;
    private List<Vector3> _pathToTarget = null;

    public KerbalFSM AiFsm;

    public KFSMState AiStIdle;
    public KFSMState AiStGoingToContainer;
    public KFSMState AiStGettingPart;
    public KFSMState AiStGoingToTarget;
    public KFSMState AiStBuilding;
    public KFSMState AiStReturning;

    public KFSMEvent OnTaskAssigned;
    public KFSMEvent OnTaskCompleted;

    private static readonly FieldInfo _constructionTargetField =
        typeof(KerbalEVA).GetField("constructionTarget", BindingFlags.NonPublic | BindingFlags.Instance);

    // ReSharper disable once InconsistentNaming
    private Part constructionTarget {
      get => (Part)_constructionTargetField.GetValue(this);
      set => _constructionTargetField.SetValue(this, value);
    }

    private GameObject _debugSphere;

    public Task AssignedTask {
      get => _assignedTask;
      set {
        // vessel.targetObject = value;
        _assignedTask = value;
        AiFsm.RunEvent(OnTaskAssigned);
      }
    }

    public delegate void ControlCallback(MechBill eva);

    // Debug visualization of control states after automation applied.
    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control Linear", isPersistant = false)]
    public Vector3 CtrlLinear;

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control PYR", isPersistant = false)]
    public Vector3 CtrlPyr;

    // Callbacks for custom automation.
    public FlightInputCallback OnFlyByWire = st => { };
    public ControlCallback OnWalkByWire = eva => { };

    public float ApproachSpeedLimit = 1.0f;
    public float TgtApproachDistance = 1.0f;

    protected override void HandleMovementInput() {
      AiFsm.FixedUpdateFSM();
      if (AiFsm.CurrentState != AiStIdle) {
        // Ignore player input if this Kerbal is AI controlled
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
        if ((nextPoint - transform.position).magnitude > TgtApproachDistance) {
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
      _pathToTarget ??= _assignedTask.Board.Pathfinder.FindPath(transform.position,
          vessel.targetObject.GetTransform().position, TgtApproachDistance);

      bool approachingTarget = !GetNextPathPoint(out Vector3 nextPoint);
      if (approachingTarget) {
        nextPoint = vessel.targetObject.GetTransform().position;
      }

      if (Globals.ShowDebugOverlay) {
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
      Vector3 tgtRelativeVelocity = part.orbit.GetVel() - vessel.targetObject.GetObtVelocity();
      Vector3 velError = goalVelocity - tgtRelativeVelocity;
      if (velError.sqrMagnitude > 1.0f) {
        velError.Normalize();
      }

      packTgtRPos = velError;
      return reachedTarget;
    }

    private void GoingToTarget_OnFixedUpdate() {
      if (OnALadder) {
        fsm.RunEvent(On_ladderLetGo);
      }

      if (!JetpackDeployed) {
        ToggleJetpack(true);
      }

      vessel.targetObject = ((AttachmentTask)_assignedTask).AttachTarget; // FIXME

      if (MoveToTarget()) {
        ((AttachmentTask)_assignedTask).Attach(); // FIXME
        EnterConstructionMode();
      }
    }

    public override void OnAwake() {
      // Replace the stock KerbalEVA class with our subclass.
      ModuleAttributes.classID = "KerbalEVA".GetHashCode();
      base.OnAwake();
    }

    private void ConstructionEntered(KFSMState st) {
      constructionTarget = ((AttachmentTask)_assignedTask).GhostPart; // FIXME
      fsm.RunEvent(On_weldStart);
      // AssignedTask = null;  // TODO: Mark task as complete and return to vessel.
    }

    protected override void SetupFSM() {
      base.SetupFSM();
      st_enteringConstruction.OnLeave += ConstructionEntered;

      AiFsm = new KerbalFSM();

      AiStIdle = new KFSMState("Idle");
      AiFsm.AddState(AiStIdle);

      AiStGoingToContainer = new KFSMState("En route to container");
      AiStGoingToContainer.OnEnter = AiStGoingToContainer_OnEnter;
      AiFsm.AddState(AiStGoingToContainer);

      AiStGettingPart = new KFSMState("Getting part from container");
      AiFsm.AddState(AiStGettingPart);

      AiStGoingToTarget = new KFSMState("En route to target");
      AiStGoingToTarget.OnFixedUpdate += GoingToTarget_OnFixedUpdate;
      AiFsm.AddState(AiStGoingToTarget);

      AiStBuilding = new KFSMState("Attaching part");
      AiFsm.AddState(AiStBuilding);

      AiStReturning = new KFSMState("Returning to base");
      AiFsm.AddState(AiStReturning);

      OnTaskAssigned = new KFSMEvent(" Task assigned");
      OnTaskAssigned.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
      OnTaskAssigned.GoToStateOnEvent = AiStGoingToContainer;
      AiFsm.AddEvent(OnTaskAssigned, AiStIdle);

      AiFsm.StartFSM(AiStIdle);
    }

    public void AiStGoingToContainer_OnEnter(KFSMState st) { }

    public void MoveToPart(Part targetPart) {
      //targetPart.vel
    }

    public void EnterConstructionMode() {
      InConstructionMode = true;
      if (!OnALadder) {
        fsm.RunEvent(On_constructionModeEnter);
      }
    }
  }
}
