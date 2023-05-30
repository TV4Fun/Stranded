using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBill : KerbalEVA {
    private AttachmentTask _assignedTask = null;

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
    public float TgtApproachDistance = 0.5f;

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

    private void MoveToTarget() {
      Vector3 tgtRelativePosition = vessel.targetObject.GetTransform().position - transform.position;
      tgtFwd = tgtRelativePosition.normalized;
      Vector3 tgtRelativeVelocity = part.orbit.GetVel() - vessel.targetObject.GetObtVelocity();
      Vector3 goalVelocity;
      if (tgtRelativePosition.magnitude <= TgtApproachDistance) {
        goalVelocity = Vector3.zero;
      } else {
        goalVelocity = tgtFwd * ApproachSpeedLimit;
      }

      /*Debug.Log("Target Position: " + vessel.targetObject.GetTransform().position + "; Vessel Position: " +
                transform.position + "; Relative Position: " + tgtRelativePosition);
      Debug.Log("Target Velocity: " + vessel.targetObject.GetObtVelocity() + "; Vessel Velocity: " +
                part.orbit.GetVel() + "; Relative Velocity: " + tgtRelativeVelocity + "; Goal Velocity: " +
                goalVelocity);*/

      Vector3 velError = goalVelocity - tgtRelativeVelocity;
      if (velError.magnitude > 1.0f) {
        velError.Normalize();
      }

      packTgtRPos = velError;
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

      MoveToTarget();
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
