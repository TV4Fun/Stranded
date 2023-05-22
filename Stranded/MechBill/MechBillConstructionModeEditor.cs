using HarmonyLib;
using JetBrains.Annotations;

namespace Stranded.MechBill {
  [HarmonyPatch(typeof(EVAConstructionModeEditor))]
  public static class MechBillConstructionModeEditor {
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
    [HarmonyPrefix]
    [HarmonyPatch("AttachPart")]
    // ReSharper disable once InconsistentNaming
    public static bool AttachPart(Part part, object attach, ref Part __result) {
      if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.isEVA) {
        return true;
      }

      MechBillJira mechBillJira = FlightGlobals.ActiveVessel.GetComponent<MechBillJira>();
      if (mechBillJira != null) {
        __result = mechBillJira.AttachPart(new MechBillJira.Attachment(attach));
      }

      return false;
    }
  }
}
