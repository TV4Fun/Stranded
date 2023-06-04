using System;
using UnityEngine;

namespace Stranded.MechBill {
  public class AttachmentTask : PartModule, ITargetable {
    // public Part Part;
    // public MechBillJira.Attachment Attachment;
    //[SerializeField]
    //private ProtoPartSnapshot _protoPartSnapshot;
    //public Part ParentPart;
    //public AttachNode TargetPartNode;
    //public AttachNode ParentPartNode;
    //public AttachModes AttachMode;
    //public Vector3 TgtPosition;
    //public Quaternion TgtRotation;

    public MechBillJira Board;

    //public Part GhostPart { get; private set; } = null;

    /*public void SetAttachment(MechBillJira.Attachment attachment) {
      ParentPart = attachment.PotentialParent;
      TargetPartNode = attachment.CallerPartNode;
      ParentPartNode = attachment.OtherPartNode;
      AttachMode = attachment.Mode;
      TgtPosition = attachment.Position;
      TgtRotation = attachment.Rotation;
      _protoPartSnapshot = attachment.Caller.protoPartSnapshot;
    }*/

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

    public static AttachmentTask Create(MechBillJira.Attachment attachment) {
      Part ghostPart =
          UIPartActionControllerInventory.Instance.CreatePartFromInventory(attachment.Caller
              .protoPartSnapshot); // Instantiate(Caller, PotentialParent.transform, true); //UIPartActionControllerInventory.Instance.CreatePartFromInventory(Caller.protoPartSnapshot);
      Transform ghostPartTransform = ghostPart.transform;
      ghostPart.attRotation0 = attachment.Rotation;
      ghostPart.attRotation = attachment.Caller.attRotation;
      ghostPart.attPos0 = attachment.Position;
      ghostPartTransform.rotation = attachment.Rotation * attachment.Caller.attRotation;
      ghostPartTransform.position = attachment.Position;

      ghostPart.isAttached = true;
      ghostPartTransform.parent = attachment.PotentialParent.transform;

      if (attachment.CallerPartNode != null) {
        if (attachment.CallerPartNode.owner.persistentId == ghostPart.persistentId) {
          AttachNode attachNode = ghostPart.FindAttachNode(attachment.CallerPartNode.id);
          if (attachNode != null) {
            attachNode.attachedPart = attachment.PotentialParent;
          } else if (attachment.CallerPartNode.id == ghostPart.srfAttachNode.id) {
            ghostPart.srfAttachNode.attachedPart = attachment.PotentialParent;
            ghostPart.srfAttachNode.srfAttachMeshName = attachment.CallerPartNode.srfAttachMeshName;
          }
        } else {
          attachment.CallerPartNode.attachedPart = attachment.PotentialParent;
        }
      }

      if (attachment.OtherPartNode != null) {
        attachment.OtherPartNode.attachedPart = ghostPart;
      }

      //GhostPart.attPos0 = ghostPartTransform.localPosition;
      //GhostPart.attRotation0 = ghostPartTransform.localRotation;

      if (attachment.Mode == AttachModes.SRF_ATTACH) {
        ghostPart.attachMode = AttachModes.SRF_ATTACH;
        ghostPart.srfAttachNode.attachedPart = attachment.PotentialParent;
      }

      ghostPart.parent = attachment.PotentialParent;
      ghostPart.vessel = attachment.PotentialParent.vessel;
      ghostPart.State = PartStates.DEACTIVATED;
      ghostPart.ResumeState = PartStates.DEACTIVATED;

      //ghostPart.OnAttachFlight(attachment.PotentialParent);
      //ghostPart.vessel.Parts.Add(ghostPart);
      //PotentialParent.addChild(ghostPart);
      ghostPart.sameVesselCollision = false;

      ModuleCargoPart cargoPart = ghostPart.FindModuleImplementing<ModuleCargoPart>();
      if (cargoPart != null) {
        cargoPart.isEnabled = false;
      }

      ghostPart.gameObject.SetLayerRecursive(Globals.GhostLayer, true);
      ghostPart.SetHighlightColor(Globals.GhostPartHighlightColor);
      ghostPart.SetHighlightType(Part.HighlightType.OnMouseOver);
      //ghostPart.SetHighlight(true, true);  TODO: Is there a way to have an always on un-outlined highlight?
      ghostPart.SetOpacity(0.5f);

      ghostPart.DemoteToPhysicslessPart();
      /*if (newPart.isCompund)
      {
        EVAConstructionModeController.Instance.evaEditor.selectedCompoundPart = newPart as CompoundPart;
      }*/

      AttachmentTask task = (AttachmentTask)ghostPart.AddModule(nameof(AttachmentTask));
      task.enabled = true;
      ghostPart.enabled = true;
      //task.GhostPart = ghostPart;
      return task;
    }
  }
}
