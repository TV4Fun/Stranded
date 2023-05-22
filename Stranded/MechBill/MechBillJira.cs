using System;
using System.Collections.Generic;
using System.Reflection;
using Highlighting;
using KSP.UI.Screens;
using UnityEngine;

namespace Stranded.MechBill {
  public class MechBillJira : VesselModule {
    private readonly Queue<Task> _backlog = new();

    private void OnDestroy() {
      GameEvents.OnEVAConstructionMode.Remove(OnEVAConstructionMode);
    }

    protected override void OnStart() {
      GameEvents.OnEVAConstructionMode.Add(OnEVAConstructionMode);
    }

    public void OnEVAConstructionMode(bool enabled) {
      StageManager.ShowHideStageStack(!enabled);
      if (enabled) {
        InputLockManager.SetControlLock(ControlTypes.PARTIAL_SHIP_CONTROLS, nameof(MechBillJira));
      } else {
        InputLockManager.RemoveControlLock(nameof(MechBillJira));
      }
    }

    private void Update() {

    }

    public Part AttachPart(Attachment attachment) {
      if (attachment == null || attachment.PotentialParent == null) return null;

      _backlog.Enqueue(new Task(attachment));

      return attachment.CreateGhostPart();
    }


    public struct Task {
      // public Part Part;
      public Attachment Attachment;

      public Task(Attachment attachment) {
        // Part = part;
        Attachment = attachment;
      }
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
      public Part PotentialParent;  // Part we attaching to
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

      public Part CreateGhostPart() {
        Part ghostPart = Caller; // Instantiate(Caller, PotentialParent.transform, true); //UIPartActionControllerInventory.Instance.CreatePartFromInventory(Caller.protoPartSnapshot);
        ghostPart.isAttached = true;
        Transform ghostPartTransform = ghostPart.transform;

        //Destroy(Caller.gameObject);
        /*if (UIPartActionControllerInventory.Instance != null)
        {
          UIPartActionControllerInventory.Instance.DestroyHeldPartAsIcon();
        }*/

        if (CallerPartNode != null) {
          if (CallerPartNode.owner.persistentId == Caller.persistentId) {
            AttachNode attachNode = ghostPart.FindAttachNode(CallerPartNode.id);
            if (attachNode != null) {
              attachNode.attachedPart = PotentialParent;
            } else if (CallerPartNode.id == ghostPart.srfAttachNode.id) {
              ghostPart.srfAttachNode.attachedPart = PotentialParent;
              ghostPart.srfAttachNode.srfAttachMeshName = CallerPartNode.srfAttachMeshName;
            }
          } else {
            CallerPartNode.attachedPart = PotentialParent;
          }
        }

        if (OtherPartNode != null) {
          OtherPartNode.attachedPart = ghostPart;
        }

        ghostPart.attPos0 = ghostPartTransform.localPosition;
        ghostPart.attRotation0 = ghostPartTransform.localRotation;
        // ghostPart.OnAttachFlight(attachment.PotentialParent);  // TODO: Try replacing this with OnAttach
        if (Mode == AttachModes.SRF_ATTACH) {
          ghostPart.attachMode = AttachModes.SRF_ATTACH;
          ghostPart.srfAttachNode.attachedPart = PotentialParent;
        }

        ghostPart.SetHighlightColor(Highlighter.colorPartHighlightDefault);
        ghostPart.SetHighlightType(Part.HighlightType.OnMouseOver);
        ghostPart.SetHighlight(true, true);
        //ghostPart.gameObject.SetLayerRecursive(13, true, 1 << 21);
        ghostPart.SetOpacity(0.5f);
        EVAConstructionModeController.Instance.evaEditor.PlayAudioClip(EVAConstructionModeController.Instance.evaEditor
            .attachClip);
        //ghostPart.DemoteToPhysicslessPart();
        /*if (newPart.isCompund)
        {
          EVAConstructionModeController.Instance.evaEditor.selectedCompoundPart = newPart as CompoundPart;
        }*/
        return ghostPart;
      }
    }
  }
}
