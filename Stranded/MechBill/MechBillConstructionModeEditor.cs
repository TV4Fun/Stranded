using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill
{
    [HarmonyPatch(typeof(EVAConstructionModeEditor))]
    public static class MechBillConstructionModeEditor
    {
        private static VesselType previousVesselType;
        private static float previousEVAConstructionRange;

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch("UpdatePartPlacementPosition")]
        public static bool UpdatePartPlacementPosition_prefix()
        {
            previousVesselType = FlightGlobals.ActiveVessel.vesselType;
            previousEVAConstructionRange = GameSettings.EVA_CONSTRUCTION_RANGE;
            
            if (!FlightGlobals.ActiveVessel.isEVA)
            {
                GameSettings.EVA_CONSTRUCTION_RANGE = float.MaxValue;
                FlightGlobals.ActiveVessel.vesselType = VesselType.EVA;
            }
            
            return true;
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch("UpdatePartPlacementPosition")]
        public static void UpdatePartPlacementPosition_postfix()
        {
            GameSettings.EVA_CONSTRUCTION_RANGE = previousEVAConstructionRange;
            FlightGlobals.ActiveVessel.vesselType = previousVesselType;
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch("AttachInput")]
        public static bool AttachInput()
        {
            if (FlightGlobals.ActiveVessel.isEVA || !Input.GetMouseButtonUp(0)) return true;
            else
            {
                // TODO: Add a task to MechBillJira to place this part
            }
        }
    }
}