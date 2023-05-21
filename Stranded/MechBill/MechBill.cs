using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBill : KerbalEVA {
    public delegate void ControlCallback(MechBill eva);

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control Linear", isPersistant = false)]
    public Vector3 ctrlLinear;

    [UsedImplicitly] [KSPField(guiActive = true, guiName = "Control PYR", isPersistant = false)]
    public Vector3 ctrlPyr;

    public FlightCtrlState CtrlState = new();

    public FlightInputCallback OnFlyByWire = st => { };

    public ControlCallback OnWalkByWire = eva => { };

    protected override void HandleMovementInput() {
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

    public override void OnAwake() {
      ModuleAttributes.classID = "KerbalEVA".GetHashCode();
      base.OnAwake();
    }

    public void MoveToPart(Part targetPart) {
      //targetPart.vel
    }
  }
}
