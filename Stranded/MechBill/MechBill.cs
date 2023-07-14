using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBill : KerbalEVA {
    private AttachmentTask _assignedTask = null;
    private List<Vector3> _pathToTarget = null;

    private static FieldInfo _constructionTargetField =
        typeof(KerbalEVA).GetField("constructionTarget", BindingFlags.NonPublic | BindingFlags.Instance);

    private GameObject _sphere;

    public AttachmentTask AssignedTask {
      get => _assignedTask;
      set {
        vessel.targetObject = value;
        _assignedTask = value;
      }
    }

    public delegate void ControlCallback(MechBill eva);

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control Linear", isPersistant = false)]
    public Vector3 ctrlLinear;

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control PYR", isPersistant = false)]
    public Vector3 ctrlPyr;

    public FlightCtrlState CtrlState = new();

    public FlightInputCallback OnFlyByWire = st => { };

    public ControlCallback OnWalkByWire = eva => { };

    public float ApproachSpeedLimit = 1.0f;
    public float TgtApproachDistance = 1.0f;

    protected override void HandleMovementInput() {
      if (AssignedTask != null) {
        // Ignore player input if this Kerbal is AI controlled
        ExecuteTask();
        return;
      }

      base.HandleMovementInput();
      OnWalkByWire(this);
      Quaternion localToWorld = transform.rotation;
      Quaternion worldToLocal = localToWorld.Inverse();

      Vector3 xyz = worldToLocal * packTgtRPos;
      CtrlState.X = xyz.x;
      CtrlState.Y = xyz.y;
      CtrlState.Z = xyz.z;

      Vector3 pyr = worldToLocal * cmdRot;
      CtrlState.pitch = pyr.x;
      CtrlState.yaw = pyr.y;
      CtrlState.roll = pyr.z;

      OnFlyByWire(CtrlState);

      xyz = CtrlState.GetXYZ();
      pyr = new Vector3(CtrlState.pitch, CtrlState.yaw,
          CtrlState.roll); // For some reason, GetPYR() switches around y and z.

      packTgtRPos = localToWorld * xyz;

      if (pyr != Vector3.zero) {
        manualAxisControl = true;
        cmdRot = localToWorld * pyr;
      } else {
        manualAxisControl = false;
      }

      ctrlLinear = xyz;
      ctrlPyr = pyr;
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
      if (Globals.ShowDebugOverlay && _sphere == null) {
        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.GetComponent<Collider>().enabled = false;
        Object.Destroy(_sphere.GetComponent<Rigidbody>());
        _sphere.transform.SetParent(transform);
        _sphere.transform.localScale = 0.1f * Vector3.one;
      }

      _pathToTarget ??= _assignedTask.Board.Pathfinder.FindPath(transform.position,
          vessel.targetObject.GetTransform().position, TgtApproachDistance);
      Vector3 tgtRelativeVelocity = part.orbit.GetVel() - vessel.targetObject.GetObtVelocity();

      bool approachingTarget = !GetNextPathPoint(out Vector3 nextPoint);
      if (approachingTarget) {
        nextPoint = vessel.targetObject.GetTransform().position;
      }

      if (Globals.ShowDebugOverlay) {
        _sphere.transform.position = nextPoint;
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
      Vector3 velError = goalVelocity - tgtRelativeVelocity;
      if (velError.sqrMagnitude > 1.0f) {
        velError.Normalize();
      }

      packTgtRPos = velError;
      return reachedTarget;
    }

    private void ExecuteTask() {
      if (OnALadder) {
        fsm.RunEvent(On_ladderLetGo);
      }

      if (!JetpackDeployed) {
        ToggleJetpack(true);
      }

      if (vessel.targetObject == null) {
        vessel.targetObject = _assignedTask;
      }

      if (AssignedTask.Status == AttachmentTask.TaskStatus.OnTheWay && MoveToTarget()) {
        AssignedTask.Attach();
        _constructionTargetField.SetValue(this, AssignedTask.part);
        fsm.RunEvent(On_weldStart);
        // AssignedTask = null;  // TODO: Mark task as complete and return to vessel.
      }
    }

    public override void OnAwake() {
      ModuleAttributes.classID = "KerbalEVA".GetHashCode();
      base.OnAwake();
    }

    public void MoveToPart(Part targetPart) {
      //targetPart.vel
    }
  }
}
