using Highlighting;
using UnityEngine;

namespace Stranded.MechBill {
  public class AttachmentTask : Task {
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

    public Part GhostPart { get; private set; } = null;

    /*public void SetAttachment(MechBillJira.Attachment attachment) {
      ParentPart = attachment.PotentialParent;
      TargetPartNode = attachment.CallerPartNode;
      ParentPartNode = attachment.OtherPartNode;
      AttachMode = attachment.Mode;
      TgtPosition = attachment.Position;
      TgtRotation = attachment.Rotation;
      _protoPartSnapshot = attachment.Caller.protoPartSnapshot;
    }*/

    [SerializeField] private ModuleInventoryPart Container;
    [SerializeField] private ModuleCargoPart PartInContainer;

    public TaskTarget ContainerTarget { get; private set; }
    public TaskTarget AttachTarget { get; private set; }

    public void Attach() {
      GhostPart.gameObject.SetLayerRecursive(0, true);
      GhostPart.PromoteToPhysicalPart();
      GhostPart.OnAttachFlight(GhostPart.parent);
      GhostPart.CreateAttachJoint(GhostPart.attachMode);
      GhostPart.SetHighlightColor(Highlighter.colorPartHighlightDefault);
      GhostPart.SetHighlightType(Part.HighlightType.AlwaysOn);
      GhostPart.SetHighlight(true, true);
      GhostPart.SetOpacity(1.0f);

      GhostPart = null;

      Cleanup();

      Complete();
    }

    protected override void CancelImpl() {
      Cleanup();
    }

    private void Cleanup() {
      if (GhostPart != null) {
        GhostPart.gameObject.DestroyGameObject();
      }
      Destroy(ContainerTarget);
      Destroy(AttachTarget);
    }

    private void OnDestroy() {
      Cleanup();
    }

    public static AttachmentTask Create(MechBillJira.Attachment attachment, ModuleInventoryPart container,
        ModuleCargoPart partInContainer) {
      AttachmentTask task = CreateInstance<AttachmentTask>();
      task.Init(attachment, container, partInContainer);

      return task;
    }

    private void Init(MechBillJira.Attachment attachment, ModuleInventoryPart container,
        ModuleCargoPart partInContainer) {
      GhostPart = UIPartActionControllerInventory.Instance.CreatePartFromInventory(attachment.Caller.protoPartSnapshot);
      // Instantiate(Caller, PotentialParent.transform, true);
      //UIPartActionControllerInventory.Instance.CreatePartFromInventory(Caller.protoPartSnapshot);
      Transform ghostPartTransform = GhostPart.transform;
      GhostPart.attRotation0 = attachment.Rotation;
      GhostPart.attRotation = attachment.Caller.attRotation;
      GhostPart.attPos0 = attachment.Position;
      ghostPartTransform.rotation = attachment.Rotation * attachment.Caller.attRotation;
      ghostPartTransform.position = attachment.Position;

      GhostPart.isAttached = true;
      ghostPartTransform.parent = attachment.PotentialParent.transform;

      if (attachment.CallerPartNode != null) {
        if (attachment.CallerPartNode.owner.persistentId == GhostPart.persistentId) {
          AttachNode attachNode = GhostPart.FindAttachNode(attachment.CallerPartNode.id);
          if (attachNode != null) {
            attachNode.attachedPart = attachment.PotentialParent;
          } else if (attachment.CallerPartNode.id == GhostPart.srfAttachNode.id) {
            GhostPart.srfAttachNode.attachedPart = attachment.PotentialParent;
            GhostPart.srfAttachNode.srfAttachMeshName = attachment.CallerPartNode.srfAttachMeshName;
          }
        } else {
          attachment.CallerPartNode.attachedPart = attachment.PotentialParent;
        }
      }

      if (attachment.OtherPartNode != null) {
        attachment.OtherPartNode.attachedPart = GhostPart;
      }

      //GhostPart.attPos0 = ghostPartTransform.localPosition;
      //GhostPart.attRotation0 = ghostPartTransform.localRotation;

      if (attachment.Mode == AttachModes.SRF_ATTACH) {
        GhostPart.attachMode = AttachModes.SRF_ATTACH;
        GhostPart.srfAttachNode.attachedPart = attachment.PotentialParent;
      }

      GhostPart.parent = attachment.PotentialParent;
      GhostPart.vessel = attachment.PotentialParent.vessel;
      GhostPart.State = PartStates.DEACTIVATED;
      GhostPart.ResumeState = PartStates.DEACTIVATED;

      //ghostPart.OnAttachFlight(attachment.PotentialParent);
      //ghostPart.vessel.Parts.Add(ghostPart);
      //PotentialParent.addChild(ghostPart);
      //ghostPart.sameVesselCollision = false;

      ModuleCargoPart cargoPart = GhostPart.FindModuleImplementing<ModuleCargoPart>();
      if (cargoPart != null) {
        cargoPart.isEnabled = false;
      }

      GhostPart.gameObject.SetLayerRecursive(Globals.GhostLayer, true);
      GhostPart.SetHighlightColor(Globals.GhostPartHighlightColor);
      GhostPart.SetHighlightType(Part.HighlightType.OnMouseOver);
      //ghostPart.SetHighlight(true, true);  TODO: Is there a way to have an always on un-outlined highlight?
      GhostPart.SetOpacity(0.5f);

      GhostPart.DemoteToPhysicslessPart();
      /*if (newPart.isCompound)
      {
        EVAConstructionModeController.Instance.evaEditor.selectedCompoundPart = newPart as CompoundPart;
      }*/

      AttachTarget = (TaskTarget)GhostPart.AddModule(nameof(TaskTarget));
      AttachTarget.Task.SetTarget(this);

      Container = container;
      ContainerTarget = (TaskTarget)Container.part.AddModule(nameof(TaskTarget));
      ContainerTarget.Task.SetTarget(this);

      PartInContainer = partInContainer;
      // task.enabled = true;
      GhostPart.enabled = true;
    }
  }
}
