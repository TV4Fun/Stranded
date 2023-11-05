using System;
using Highlighting;
using Stranded.Util;
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

    public KerbalFSM Fsm;

    public KFSMState StIdle;
    public KFSMState StGoingToContainer;
    public KFSMState StGettingPart;
    public KFSMState StGoingToTarget;
    public KFSMState StBuilding;
    public KFSMState StReturning;

    public KFSMEvent OnTaskAssigned;
    public KFSMEvent OnContainerReached;
    public KFSMEvent OnPartAcquired;
    public KFSMEvent OnTargetReached;

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

    public override void FixedUpdate() {
      Fsm.FixedUpdateFSM();
    }

    public static AttachmentTask Create(MechBillJira.Attachment attachment, ModuleInventoryPart container,
        ModuleCargoPart partInContainer) {
      AttachmentTask task = CreateInstance<AttachmentTask>();
      task.Init(attachment, container, partInContainer);

      return task;
    }

    private Part SetupGhostPart(MechBillJira.Attachment attachment) {
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

      GhostPart.enabled = true;

      return GhostPart;
    }

    private void SetupTargets(ModuleInventoryPart container) {
      AttachTarget = (TaskTarget)GhostPart.AddModule(nameof(TaskTarget));
      AttachTarget.Task = new WeakReference<Task>(this);

      Container = container;
      ContainerTarget = (TaskTarget)Container.part.AddModule(nameof(TaskTarget));
      ContainerTarget.Task = new WeakReference<Task>(this);
    }

    // ReSharper disable once InconsistentNaming
    private void SetupFSM() {

      Fsm = new KerbalFSM();

      StIdle = new KFSMState("Idle");
      Fsm.AddState(StIdle);

      StGoingToContainer = new KFSMState("En route to container");
      StGoingToContainer.OnEnter = StGoingToContainer_OnEnter;
      StGoingToContainer.OnLeave = StGoingToContainer_OnLeave;
      Fsm.AddState(StGoingToContainer);

      StGettingPart = new KFSMState("Getting part from container");
      StGettingPart.OnEnter = StGettingPart_OnEnter;
      Fsm.AddState(StGettingPart);

      StGoingToTarget = new KFSMState("En route to target");
      StGoingToTarget.OnEnter = StGoingToTarget_OnEnter;
      StGoingToTarget.OnLeave = StGoingToTarget_OnLeave;
      Fsm.AddState(StGoingToTarget);

      StBuilding = new KFSMState("Attaching part");
      Fsm.AddState(StBuilding);

      StReturning = new KFSMState("Returning to base");
      Fsm.AddState(StReturning);

      OnTaskAssigned = new KFSMEvent("Task assigned");
      OnTaskAssigned.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
      OnTaskAssigned.GoToStateOnEvent = StGoingToContainer;
      Fsm.AddEvent(OnTaskAssigned, StIdle);

      OnContainerReached = new KFSMEvent("Container reached");
      OnContainerReached.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
      OnContainerReached.GoToStateOnEvent = StGettingPart;
      Fsm.AddEvent(OnContainerReached, StGoingToContainer);

      OnPartAcquired = new KFSMEvent("Part acquired");
      OnPartAcquired.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
      OnPartAcquired.GoToStateOnEvent = StGoingToTarget;
      Fsm.AddEvent(OnPartAcquired, StGettingPart);

      OnTargetReached = new KFSMEvent("Target reached");
      OnTargetReached.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
      OnTargetReached.GoToStateOnEvent = StBuilding;
      Fsm.AddEvent(OnTargetReached, StGoingToTarget);

      Fsm.StartFSM(StIdle);
    }

    protected override void OnAssigned() {
      Fsm.RunEvent(OnTaskAssigned);
    }

    private void StGoingToContainer_OnEnter(KFSMState st) {
      Assignee.Target = ContainerTarget;
      Assignee.TgtApproachDistance = GameSettings.EVA_INVENTORY_RANGE;
      Assignee.OnTargetReached += new KFSMEventCallback(Fsm, OnContainerReached);
    }

    private void StGoingToContainer_OnLeave(KFSMState st) {
      Assignee.OnTargetReached -= new KFSMEventCallback(Fsm, OnContainerReached);
    }

    private void StGettingPart_OnEnter(KFSMState st) {
      // TODO
      Fsm.RunEvent(OnPartAcquired);
    }

    private void StGoingToTarget_OnEnter(KFSMState st) {
      Assignee.Target = AttachTarget;
      Assignee.TgtApproachDistance = GameSettings.EVA_CONSTRUCTION_RANGE;
      Assignee.OnTargetReached += new KFSMEventCallback(Fsm, OnTargetReached);
    }

    private void StGoingToTarget_OnLeave(KFSMState st) {
      Assignee.OnTargetReached -= new KFSMEventCallback(Fsm, OnTargetReached);
    }

    private void Init(MechBillJira.Attachment attachment, ModuleInventoryPart container,
        ModuleCargoPart partInContainer) {
      SetupGhostPart(attachment);
      SetupTargets(container);
      SetupFSM();

      PartInContainer = partInContainer;
      // task.enabled = true;
    }
  }
}
