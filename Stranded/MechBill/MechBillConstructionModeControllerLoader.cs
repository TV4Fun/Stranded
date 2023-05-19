using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Stranded.MechBill
{
    [HarmonyPatch(typeof(EVAConstructionModeController))]
    public static class MechBillConstructionModeControllerLoader
    {
        private static readonly FieldInfo _loadedModuleInventoryPart = typeof(EVAConstructionModeController)
            .GetField("loadedModuleInventoryPart", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo _searchForInventoryParts = typeof(EVAConstructionModeController)
            .GetMethod("SearchForInventoryParts", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo _tryGetDisplayedInventory = typeof(EVAConstructionModeController)
            .GetMethod("TryGetDisplayedInventory", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type InventoryDisplayItem =
            typeof(EVAConstructionModeController).GetNestedType("InventoryDisplayItem", BindingFlags.NonPublic);

        private static readonly FieldInfo _uiPartActionInventoryContainerPrefab = typeof(EVAConstructionModeController)
            .GetField("uiPartActionInventoryContainerPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _uiPartActionInventoryParent = typeof(EVAConstructionModeController)
            .GetField("uiPartActionInventoryParent", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _displayedInventories = typeof(EVAConstructionModeController)
            .GetField("displayedInventories", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _displayedInventoryModule = InventoryDisplayItem.GetField("inventoryModule");
        private static readonly FieldInfo _displayedInventoryUIObject = InventoryDisplayItem.GetField("uiObject");
        private static readonly FieldInfo _displayedInventoryUIInventory = InventoryDisplayItem.GetField("uiInventory");

        private static readonly MethodInfo _listInventoryDisplay_Add =
            typeof(List<>).MakeGenericType(InventoryDisplayItem).GetMethod("Add");

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch("CanOpenConstructionPanel")]
        public static bool CanOpenConstructionPanel(ref bool __result)
        {
            __result = HighLogic.LoadedSceneIsFlight && !MapView.MapIsEnabled && FlightGlobals.ActiveVessel != null;
            return false;
        }

        private static Dictionary<uint, ModuleInventoryPart> LoadedModuleInventoryParts(
            EVAConstructionModeController instance)
        {
            return (Dictionary<uint, ModuleInventoryPart>)_loadedModuleInventoryPart.GetValue(instance);
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch("OnVesselChange")]
        public static void OnVesselChange(EVAConstructionModeController __instance)
        {
            LoadedModuleInventoryParts(__instance).Clear();
            _searchForInventoryParts.Invoke(__instance, null);
        }

        private static bool LoadModuleInventoryPart(EVAConstructionModeController instance, uint partPersistentId,
            ModuleInventoryPart inventoryModule)
        {
            Dictionary<uint, ModuleInventoryPart> loadedModuleInventoryParts = LoadedModuleInventoryParts(instance);
            if (loadedModuleInventoryParts.ContainsKey(partPersistentId)) return false;
            else
            {
                loadedModuleInventoryParts.Add(partPersistentId, inventoryModule);
                return true;
            }
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch("SearchForInventoryParts")]
        public static bool SearchForInventoryParts(EVAConstructionModeController __instance)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || vessel.isEVA) return true;
            else
            {
                List<Part> parts = vessel.parts;
                foreach (Part part in parts)
                {
                    ModuleInventoryPart inventoryModule = part.FindModuleImplementing<ModuleInventoryPart>();

                    if (inventoryModule != null && !part.isKerbalEVA())
                        LoadModuleInventoryPart(__instance, part.persistentId, inventoryModule);

                    if (part.protoModuleCrew != null)
                    {
                        foreach (ProtoCrewMember protoCrew in part.protoModuleCrew)
                        {
                            ModuleInventoryPart kerbalInventoryModule = protoCrew.KerbalInventoryModule;
                            if (kerbalInventoryModule != null)
                            {
                                kerbalInventoryModule.transform.position = part.transform.position;
                                LoadModuleInventoryPart(__instance, protoCrew.persistentID, kerbalInventoryModule);
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static bool TryGetDisplayedInventory(EVAConstructionModeController instance,
            ModuleInventoryPart moduleInventoryPart, out object displayItem)
        {
            object[] args = { moduleInventoryPart, null };
            bool result = (bool)_tryGetDisplayedInventory.Invoke(instance, args);
            displayItem = args[1];
            return result;
        }

        private static void AddInventoryDisplay(EVAConstructionModeController instance,
            ModuleInventoryPart moduleInventoryPart)
        {
            if (!TryGetDisplayedInventory(instance, moduleInventoryPart, out object displayItem))
            {
                displayItem = Activator.CreateInstance(InventoryDisplayItem);
                _displayedInventoryModule.SetValue(displayItem, moduleInventoryPart);
                GameObject uiObject =
                    Object.Instantiate((GameObject)_uiPartActionInventoryContainerPrefab.GetValue(instance));
                _displayedInventoryUIObject.SetValue(displayItem, uiObject);
                if (uiObject != null)
                {
                    uiObject.transform.SetParent(
                        ((GameObject)_uiPartActionInventoryParent.GetValue(instance)).transform);
                    uiObject.transform.localPosition = Vector3.zero;
                    uiObject.transform.localScale = Vector3.one;
                    UIPartActionInventory uiInventory = uiObject.GetComponentInChildren<UIPartActionInventory>(true);
                    _displayedInventoryUIInventory.SetValue(displayItem, uiInventory);
                    if (uiInventory != null)
                        uiInventory.SetupConstruction(moduleInventoryPart);
                }

                object displayedInventories = _displayedInventories.GetValue(instance);
                _listInventoryDisplay_Add.Invoke(displayedInventories, new[] { displayItem });
            }
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch("UpdateDisplayedInventories")]
        public static bool UpdateDisplayedInventories(EVAConstructionModeController __instance)
        {
            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.isEVA) return true;
            else
            {
                Dictionary<uint, ModuleInventoryPart> loadedModuleInventoryParts =
                    LoadedModuleInventoryParts(__instance);
                foreach (KeyValuePair<uint, ModuleInventoryPart> kv in loadedModuleInventoryParts)
                {
                    ModuleInventoryPart moduleInventoryPart = kv.Value;
                    if (moduleInventoryPart == null) loadedModuleInventoryParts.Remove(kv.Key);
                    else AddInventoryDisplay(__instance, moduleInventoryPart);
                }

                return false;
            }
        }
    }
}