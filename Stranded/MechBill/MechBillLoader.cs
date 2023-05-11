using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Stranded
{
    [HarmonyPatch(typeof(PartLoader), nameof(PartLoader.StartLoad))]
    public class MechBillLoader
    {
        [UsedImplicitly]
        static bool Prefix(PartLoader __instance)
        {
            foreach (AvailablePart part in __instance.parts)
            {
                KerbalEVA oldEva = part.partPrefab.GetComponent<KerbalEVA>();
                if (oldEva != null)
                {
                    bool wasActive = part.partPrefab.gameObject.activeSelf;
                    part.partPrefab.gameObject.SetActive(false);
                    MechBill eva = part.partPrefab.gameObject.AddComponent<MechBill>();
                    FieldInfo[] sourceFields =
                        typeof(KerbalEVA).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    foreach (FieldInfo field in sourceFields)
                    {
                        field.SetValue(eva, field.GetValue(oldEva));
                    }

                    FieldInfo kerbalEvaField = typeof(LadderEndCheck).GetField("kerbalEVA", BindingFlags.NonPublic | BindingFlags.Instance);
                    kerbalEvaField.SetValue(eva.bottomLadderEnd, eva);
                    kerbalEvaField.SetValue(eva.topLadderEnd, eva);
                    Object.DestroyImmediate(oldEva);
                    part.partPrefab.gameObject.SetActive(wasActive);
                    // eva.Awake();
                }
            }

            return true;
        }
    }
}