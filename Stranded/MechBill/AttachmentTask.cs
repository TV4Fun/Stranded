using UnityEngine;

namespace Stranded.MechBill {
  public class AttachmentTask : PartModule, ITargetable {
    // public Part Part;
    // public MechBillJira.Attachment Attachment;
    public Part ParentPart;
    public AttachNode TargetPartNode;
    public AttachNode ParentPartNode;
    public AttachModes AttachMode;
    public Vector3 TgtPosition;
    public Quaternion TgtRotation;

    public MechBillJira Board;

    public void SetAttachment(MechBillJira.Attachment attachment) {
      ParentPart = attachment.PotentialParent;
      TargetPartNode = attachment.CallerPartNode;
      ParentPartNode = attachment.OtherPartNode;
      AttachMode = attachment.Mode;
      TgtPosition = attachment.Position;
      TgtRotation = attachment.Rotation;
    }

    [KSPEvent(guiActive = true, guiName = "Cancel")]
    public void Cancel() {
      Board.CancelTask(this);
    }

    public bool GetActiveTargetable() => true;
    public string GetDisplayName() => name;
    public Vector3 GetFwdVector() => transform.forward;
    public string GetName() => name;

    public Vector3 GetObtVelocity() => vessel.obt_velocity;
    public Orbit GetOrbit() => vessel.orbit;
    public OrbitDriver GetOrbitDriver() => vessel.orbitDriver;
    public Vector3 GetSrfVelocity() => vessel.srf_velocity;
    public VesselTargetModes GetTargetingMode() => VesselTargetModes.DirectionVelocityAndOrientation;
    public Transform GetTransform() => transform;
    public Vessel GetVessel() => vessel;
  }
}
