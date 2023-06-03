using System;
using HarmonyLib;
using JetBrains.Annotations;

namespace Stranded.MechBill {
  [HarmonyPatch(typeof(EVAConstructionModeEditor))]
  // ReSharper disable InconsistentNaming
  public static class MechBillConstructionModeEditor {
    private class SoDoneWithThis : Exception { }

    private static VesselType _previousVesselType;
    private static float _previousEvaConstructionRange;

    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch("UpdatePartPlacementPosition")]
    public static bool UpdatePartPlacementPosition_prefix() {
      _previousVesselType = FlightGlobals.ActiveVessel.vesselType;
      _previousEvaConstructionRange = GameSettings.EVA_CONSTRUCTION_RANGE;

      if (!FlightGlobals.ActiveVessel.isEVA) {
        GameSettings.EVA_CONSTRUCTION_RANGE = float.MaxValue;
        FlightGlobals.ActiveVessel.vesselType = VesselType.EVA;
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

      MechBillJira mechBillJira = FlightGlobals.ActiveVessel.GetComponent<MechBillJira>();
      if (mechBillJira != null) {
        __result = mechBillJira.AttachPart(new MechBillJira.Attachment(attach));
      }

      if (UIPartActionControllerInventory.Instance != null) {
        if (UIPartActionControllerInventory.Instance.CurrentInventorySlotClicked != null) {
          UIPartActionControllerInventory.Instance.CurrentInventorySlotClicked.ReturnHeldPartToThisSlot();
          // TODO: Make this grayed out and disappear when an engineer collects it.
        }

        /*if (UIPartActionControllerInventory.Instance.CurrentInventoryOnlyIcon != null) {
          Object.Destroy(UIPartActionControllerInventory.Instance.CurrentInventoryOnlyIcon.gameObject);
        }*/
      }

      throw new SoDoneWithThis();
    }
  }
}
