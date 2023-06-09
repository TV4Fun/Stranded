using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded.MechBill {
  [HarmonyPatch(typeof(PartLoader), nameof(PartLoader.StartLoad))]
  // ReSharper disable InconsistentNaming
  public static class MechBillLoader {
    [UsedImplicitly]
    private static bool Prefix(PartLoader __instance) {
      foreach (AvailablePart part in __instance.parts) {
        KerbalEVA oldEva = part.partPrefab.GetComponent<KerbalEVA>();
        if (oldEva != null) {
          bool wasActive = part.partPrefab.gameObject.activeSelf;
          part.partPrefab.gameObject.SetActive(false);
          MechBill eva = part.partPrefab.gameObject.AddComponent<MechBill>();
          var sourceFields =
              typeof(KerbalEVA).GetFields(
                  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

          foreach (FieldInfo field in sourceFields) {
            field.SetValue(eva, field.GetValue(oldEva));
          }

          FieldInfo kerbalEvaField =
              typeof(LadderEndCheck).GetField("kerbalEVA", BindingFlags.NonPublic | BindingFlags.Instance);
          kerbalEvaField.SetValue(eva.bottomLadderEnd, eva);
          kerbalEvaField.SetValue(eva.topLadderEnd, eva);
          Object.DestroyImmediate(oldEva);
          part.partPrefab.gameObject.SetActive(wasActive);
          // eva.Awake();
        }
      }

      return true;
    }

    /*[UsedImplicitly]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICanvasPrefabSpawner), "Awake")]
    static bool UICanvasPrefabSpawnerPrefix(UICanvasPrefabSpawner __instance)
    {
        MechBillConstructionModeController controller = __instance.prefabs[0].prefab.transform.Find("Canvas")
            .gameObject.AddComponent<MechBillConstructionModeController>();
        controller.AssistingKerbalsLabel =
        return true;
    }*/
  }
}
