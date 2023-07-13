using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FinePrint.Utilities;
using KSP.UI.Screens;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBillJira : VesselModule {
    [SerializeField] private Queue<AttachmentTask> _backlog = new();
    private Stack<ProtoCrewMember> _availableEngineers;

    public Pathfinder Pathfinder;

    private void OnDestroy() {
      GameEvents.OnEVAConstructionMode.Remove(OnEVAConstructionMode);
    }

    protected override void OnStart() {
      GameEvents.OnEVAConstructionMode.Add(OnEVAConstructionMode);
      CameraManager.GetCurrentCamera().cullingMask |= 1 << Globals.GhostLayer;
      Part.layerMask |= 1 << Globals.GhostLayer;
      RebuildAvailableEngineers();
      GameEvents.onVesselCrewWasModified.Add(OnVesselCrewWasModified);
      Pathfinder = new Pathfinder(vessel);
      SetupCollisionIgnores();
    }

    private void Update() {
      AssignTasks();
      Pathfinder.Transform = transform;
    }

    private static void SetupCollisionIgnores() {
      for (int i = 0; i < 32; ++i) {
        Physics.IgnoreLayerCollision(i, Globals.GhostLayer);
      }
    }

    private void OnVesselCrewWasModified(Vessel modifiedVessel) {
      if (vessel == modifiedVessel) {
        RebuildAvailableEngineers();
      }
    }

    public void OnEVAConstructionMode(bool enabled) {
      StageManager.ShowHideStageStack(!enabled);
      if (enabled) {
        InputLockManager.SetControlLock(ControlTypes.PARTIAL_SHIP_CONTROLS, nameof(MechBillJira));
      } else {
        InputLockManager.RemoveControlLock(nameof(MechBillJira));
      }
    }

    private void RebuildAvailableEngineers() {
      _availableEngineers = new Stack<ProtoCrewMember>(VesselUtilities.VesselCrewWithTrait("Engineer", vessel));
    }

    private void AssignTasks() {
      if (_availableEngineers.Count > 0 && _backlog.Count > 0) {
        ProtoCrewMember assignedEngineer = _availableEngineers.Pop();
        AttachmentTask assignedTask = _backlog.Dequeue();

        MechBill mechBill = (MechBill)FlightEVA.SpawnEVA(assignedEngineer.KerbalRef);
        FlightGlobalsOverrides.StopNextForcedVesselSwitch();
        mechBill.AssignedTask = assignedTask;
      }
    }

    public Part AttachPart(Attachment attachment) {
      if (attachment == null || attachment.PotentialParent == null) return null;
      AttachmentTask task = AttachmentTask.Create(attachment);
      task.Board = this;

      _backlog.Enqueue(task);

      //EVAConstructionModeController.Instance.evaEditor.PlayAudioClip(EVAConstructionModeController.Instance.evaEditor
      //    .attachClip);

      return task.part;
    }

    public void CancelTask(AttachmentTask task) {
      _backlog = new Queue<AttachmentTask>(_backlog.Where(x => x != task));
      Destroy(task.gameObject);
    }


    // Class describing how to attach a new part to an existing vessel.
    public class Attachment {
      private static readonly Type _kspAttachment =
          Assembly.GetAssembly(typeof(EVAConstructionModeEditor)).GetType("Attachment");

      private static readonly FieldInfo _possible = _kspAttachment.GetField("possible");

      // private static readonly FieldInfo _collision = _kspAttachment.GetField("collision");
      private static readonly FieldInfo _mode = _kspAttachment.GetField("mode");
      private static readonly FieldInfo _potentialParent = _kspAttachment.GetField("potentialParent");
      private static readonly FieldInfo _caller = _kspAttachment.GetField("caller");
      private static readonly FieldInfo _callerPartNode = _kspAttachment.GetField("callerPartNode");
      private static readonly FieldInfo _otherPartNode = _kspAttachment.GetField("otherPartNode");
      private static readonly FieldInfo _position = _kspAttachment.GetField("position");
      private static readonly FieldInfo _rotation = _kspAttachment.GetField("rotation");

      public Part Caller; // Part to be attached
      public Part PotentialParent; // Part we attaching to
      public AttachNode CallerPartNode; // Attachment point on the part to be attached
      public AttachNode OtherPartNode; // Attachment point on the part being attached to

      public AttachModes Mode; // Attach on stack node or on surface
      public Vector3 Position;

      public bool Possible;
      public Quaternion Rotation;

      // public bool Collision;  // Appears to be unsued

      public Attachment(object attachment) {
        Possible = (bool)_possible.GetValue(attachment);
        // Collision = (bool)_collision.GetValue(attachment);
        Mode = (AttachModes)_mode.GetValue(attachment);
        PotentialParent = (Part)_potentialParent.GetValue(attachment);
        Caller = (Part)_caller.GetValue(attachment);
        CallerPartNode = (AttachNode)_callerPartNode.GetValue(attachment);
        OtherPartNode = (AttachNode)_otherPartNode.GetValue(attachment);
        Position = (Vector3)_position.GetValue(attachment);
        Rotation = (Quaternion)_rotation.GetValue(attachment);
      }
    }
  }
}
