using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Stranded.MechBill {
  [HarmonyPatch(typeof(EVAConstructionModeEditor))]
  // ReSharper disable InconsistentNaming
  public static class MechBillConstructionModeEditor {
    private class SoDoneWithThis : Exception { }

    private static VesselType _previousVesselType;
    private static float _previousEvaConstructionRange;

    private static readonly FieldInfo _moduleInventoryPart =
        typeof(UIPartActionInventorySlot).GetField("moduleInventoryPart",
            BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo _cargoPartRef =
        typeof(UIPartActionInventorySlot).GetField("cargoPartRef",
            BindingFlags.NonPublic | BindingFlags.Instance);

    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch("UpdatePartPlacementPosition")]
    public static bool UpdatePartPlacementPosition_prefix() {
      _previousVesselType = FlightGlobals.ActiveVessel.vesselType;
      _previousEvaConstructionRange = GameSettings.EVA_CONSTRUCTION_RANGE;

      if (!FlightGlobals.ActiveVessel.isEVA) {
        GameSettings.EVA_CONSTRUCTION_RANGE =
            float.MaxValue; // Allow construction anywhere on the vessel TODO: Maybe consider limiting range for ground attachments.
        FlightGlobals.ActiveVessel.vesselType =
            VesselType.EVA; // Have to set vessel type to EVA for construction UI to show up.
      }

      return true;
    }

    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch("UpdatePartPlacementPosition")]
    public static void UpdatePartPlacementPosition_postfix() {
      GameSettings.EVA_CONSTRUCTION_RANGE = _previousEvaConstructionRange;
      FlightGlobals.ActiveVessel.vesselType = _previousVesselType;
    }

    /*[UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch("AttachInput")]
    public static bool AttachInput() {
        if (FlightGlobals.ActiveVessel.isEVA || !Input.GetMouseButtonUp(0)) return true;

        // TODO: Add a task to MechBillJira to place this part
        return false;
    }*/

    [UsedImplicitly]
    [HarmonyFinalizer]
    [HarmonyPatch("Update")]
    public static Exception CatchDone(Exception __exception) {
      return __exception is SoDoneWithThis ? null : __exception;
    }

    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch("AttachPart")]
    public static bool AttachPart(Part part, object attach, ref Part __result) {
      if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.isEVA) {
        return true;
      }

      ModuleInventoryPart container = null;
      ModuleCargoPart partInContainer = null;

      if (UIPartActionControllerInventory.Instance != null) {
        UIPartActionInventorySlot currentSlot = UIPartActionControllerInventory.Instance.CurrentInventorySlotClicked;
        if (currentSlot != null) {
          currentSlot.ReturnHeldPartToThisSlot();
          container = (ModuleInventoryPart)_moduleInventoryPart.GetValue(currentSlot);
          partInContainer = (ModuleCargoPart)_cargoPartRef.GetValue(currentSlot);
          // TODO: Make this grayed out and disappear when an engineer collects it.
        }

        /*if (UIPartActionControllerInventory.Instance.CurrentInventoryOnlyIcon != null) {
          Object.Destroy(UIPartActionControllerInventory.Instance.CurrentInventoryOnlyIcon.gameObject);
        }*/
      }

      MechBillJira mechBillJira = FlightGlobals.ActiveVessel.GetComponent<MechBillJira>();
      if (mechBillJira != null) {
        __result = mechBillJira.AttachPart(new MechBillJira.Attachment(attach), container, partInContainer).GhostPart;
      }

      // Suppress normal post-attach updates that don't apply to a ghost part.
      throw new SoDoneWithThis();
    }
  }
}
