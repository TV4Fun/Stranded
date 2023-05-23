using HarmonyLib;
using JetBrains.Annotations;

namespace Stranded.MechBill {
  [HarmonyPatch(typeof(FlightGlobals))]
  public class FlightGlobalsOverrides {
    private static bool _stopNextForcedVesselSwitch = false;

    [UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch("ForceSetActiveVessel")]
    public static bool ForceSetActiveVessel() {
      if (_stopNextForcedVesselSwitch) {
        _stopNextForcedVesselSwitch = false;
        return false;
      }

      return true;
    }

    public static void StopNextForcedVesselSwitch() {
      _stopNextForcedVesselSwitch = true;
    }
  }
}
