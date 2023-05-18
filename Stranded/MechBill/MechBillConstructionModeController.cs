using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSP.Localization;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Stranded.MechBill
{
    public class MechBillConstructionModeController :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        protected static MechBillConstructionModeController instance;
        public EVAConstructionModeEditor evaEditor;
        public EVAConstructionToolsUI evaToolsUI;
        [SerializeField] protected LayerMask markerCamCullingMask;
        protected Camera markerCam;
        [SerializeField] protected UIPanelTransition constructionModeTransition;
        [SerializeField] protected UIPanelTransition navballTransition;
        protected bool navballPreviouslyOpen;
        protected bool isOpen;
        [SerializeField] protected GameObject uiPartActionInventoryContainerPrefab;
        [SerializeField] protected GameObject uiPartActionInventoryParent;
        protected Dictionary<uint, ModuleInventoryPart> loadedModuleInventoryParts;
        protected List<InventoryDisplayItem> displayedInventories;
        [SerializeField] protected RectTransform partList;
        protected Vector2 partListVector;
        [SerializeField] protected RectTransform footerConstruction;
        protected bool hover;
        protected ApplicationLauncherButton applauncherConstruction;
        [SerializeField] protected Button exitButton;
        [SerializeField] protected TooltipController_Text exitTooltipText;
        [SerializeField] protected RectTransform exitButtonOriginalPos;

        public TextMeshProUGUI AssistingKerbalsLabel;
        public TextMeshProUGUI AssistingKerbalsNumber;
        public TextMeshProUGUI MaxMassLimitLabel;
        public TextMeshProUGUI MaxMassLimitNumber;
        protected double constructionGravity;
        protected double lastConstructionGravity;

        public bool IsOpen => isOpen;
        public bool Hover => hover;
        public static MechBillConstructionModeController Instance => instance;

        protected void Awake()
        {
            if (Instance != null) Destroy(this);
            else
            {
                instance = this;
                loadedModuleInventoryParts = new Dictionary<uint, ModuleInventoryPart>();
                displayedInventories = new List<InventoryDisplayItem>();
                GameEvents.onLevelWasLoaded.Add(OnLoadedScene);
            }
        }

        protected void Start()
        {
            if (footerConstruction != null) footerConstruction.anchoredPosition = Vector2.zero;

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
            GameEvents.OnMapExited.Add(SetAppLauncherButtonVisibility);
            GameEvents.onGUIActionGroupFlightShowing.Add(OnActionGroupsOpened);
            GameEvents.OnCombinedConstructionWeightLimitChanged.Add(UpdateInfoLabels);
            exitButton.onClick.AddListener(ClosePanel);
            partListVector = partList.offsetMin;
            SetAppLauncherButtonVisibility();
            AssistingKerbalsLabel.transform.parent.gameObject.SetActive(GameSettings
                .EVA_CONSTRUCTION_COMBINE_ENABLED);
        }

        protected void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
            GameEvents.OnMapExited.Remove(SetAppLauncherButtonVisibility);
            GameEvents.onLevelWasLoaded.Remove(OnLoadedScene);
            GameEvents.onGUIActionGroupFlightShowing.Remove(OnActionGroupsOpened);
            GameEvents.OnCombinedConstructionWeightLimitChanged.Remove(UpdateInfoLabels);
            if (markerCam != null && markerCam.gameObject != null) Destroy(markerCam.gameObject);

            exitButton.onClick.RemoveListener(ClosePanel);
        }

        protected void SetAppLauncherButtonVisibility()
        {
            if (applauncherConstruction != null)
            {
                applauncherConstruction.VisibleInScenes = CanOpenConstructionPanel()
                    ? ApplicationLauncher.AppScenes.FLIGHT
                    : ApplicationLauncher.AppScenes.NEVER;
            }
        }

        protected void Update()
        {
            if (GameSettings.EVA_CONSTRUCTION_MODE_TOGGLE.GetKeyUp())
            {
                if (IsOpen) ClosePanel();
                else OpenConstructionPanel();
            }

            if (!isOpen) return;

            SearchForInventoryParts();
            UpdateDisplayedInventories();

            constructionGravity =
                EVAConstructionUtil.GetConstructionGee(FlightGlobals.ActiveVessel);
            if (Math.Abs(lastConstructionGravity - constructionGravity) > 0.001) UpdateInfoLabels();

            lastConstructionGravity = constructionGravity;
        }

        public void RegisterAppButtonConstruction(ApplicationLauncherButton button)
        {
            applauncherConstruction = button;
            SetAppLauncherButtonVisibility();
        }

        public void OpenConstructionPanel()
        {
            if (!CanOpenConstructionPanel() || IsOpen) return;

            navballPreviouslyOpen = navballTransition.State == "In";
            if (navballPreviouslyOpen) navballTransition.Transition("Out");

            ControlTypes controlTypes = InputLockManager.SetControlLock(ControlTypes.MAP_TOGGLE,
                nameof(MechBillConstructionModeController));
            constructionModeTransition.Transition("In");
            exitButton.transform.position = this.exitButtonOriginalPos.position;
            typeof(EVAConstructionToolsUI).GetMethod("ShowModeTools", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(evaToolsUI, null);
            exitTooltipText.textString = Localizer.Format("#autoLOC_8003410");
            footerConstruction.gameObject.SetActive(true);
            partListVector.y = footerConstruction.offsetMax.y;
            partList.offsetMin = partListVector;

            isOpen = true;
            if (InputLockManager.IsLocked(ControlTypes.EDITOR_SOFT_LOCK))
                InputLockManager.RemoveControlLock("EVACompoundPart_Placement");

            if (FlightCamera.fetch != null) FlightCamera.fetch.DisableCameraHighlighter();

            SearchForInventoryParts();
            UpdateInfoLabels();
            if (applauncherConstruction != null) applauncherConstruction.SetTrue(false);

            GameEvents.OnEVAConstructionMode.Fire(true);
        }

        public void ClosePanel()
        {
            if (!IsOpen) return;
            evaEditor.ForceDrop();
            if (constructionModeTransition != null) constructionModeTransition.Transition("Out");
            InputLockManager.RemoveControlLock(nameof(MechBillConstructionModeController));
            isOpen = false;

            if (navballPreviouslyOpen) navballTransition.Transition("In");
            if (FlightCamera.fetch != null) FlightCamera.fetch.CycleCameraHighlighter();

            loadedModuleInventoryParts.Clear();

            foreach (InventoryDisplayItem displayedInventory in displayedInventories.Where(displayedInventory =>
                         displayedInventory.uiObject != null)) Destroy(displayedInventory.uiObject);

            displayedInventories.Clear();

            if (applauncherConstruction != null) applauncherConstruction.SetFalse();
            GameEvents.OnEVAConstructionMode.Fire(false);
        }

        protected bool CanOpenConstructionPanel()
        {
            return HighLogic.LoadedSceneIsFlight && !MapView.MapIsEnabled && FlightGlobals.ActiveVessel != null &&
                   !FlightGlobals.ActiveVessel.isEVA;
        }

        protected void OnGameSceneLoadRequested(GameScenes scene)
        {
            if (isOpen) ClosePanel();
            SetAppLauncherButtonVisibility();
        }

        protected void OnVesselChange(Vessel vessel)
        {
            if (isOpen && !CanOpenConstructionPanel()) ClosePanel();
            SetAppLauncherButtonVisibility();
            loadedModuleInventoryParts.Clear();
            SearchForInventoryParts();
        }

        protected void OnEditorPartEvent(ConstructionEventType eventType, Part p)
        {
            switch (eventType)
            {
                case ConstructionEventType.PartDropped:
                case ConstructionEventType.PartDetached:
                case ConstructionEventType.PartPicked:
                    RemoveInvDisplayItem(p);
                    goto case ConstructionEventType.PartAttached;
                case ConstructionEventType.PartAttached:
                    DeselectAllDisplayItems();
                    return;
            }
        }

        protected void DeselectAllDisplayItems()
        {
            foreach (InventoryDisplayItem displayedInventory in displayedInventories.Where(displayedInventory =>
                         displayedInventory.uiObject != null))
                displayedInventory.uiInventory.SetAllSlotsNotSelected();
        }

        protected void RemoveInvDisplayItem(Part p)
        {
            if (p == null) return;
            if (!loadedModuleInventoryParts.TryGetValue(p.persistentId,
                    out ModuleInventoryPart loadedModuleInventoryPart)) return;

            if (TryGetDisplayedInventory(loadedModuleInventoryPart, out InventoryDisplayItem displayItem))
            {
                int index = displayedInventories.IndexOf(displayItem);
                if (index >= 0)
                {
                    Destroy(displayItem.uiObject);
                    displayedInventories.RemoveAt(index);
                }
            }

            Destroy(loadedModuleInventoryPart);
            loadedModuleInventoryParts.Remove(p.persistentId);
        }

        protected void OnActionGroupsOpened() => ClosePanel();

        protected bool LoadModuleInventoryPart(uint partPersistentId, ModuleInventoryPart inventoryModule)
        {
            if (loadedModuleInventoryParts.ContainsKey(partPersistentId)) return false;
            else
            {
                loadedModuleInventoryParts.Add(partPersistentId, inventoryModule);
                return true;
            }
        }

        protected void SearchForInventoryParts()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            List<Part> parts = vessel.parts;
            foreach (Part part in parts)
            {
                ModuleInventoryPart inventoryModule = part.FindModuleImplementing<ModuleInventoryPart>();

                if (inventoryModule != null && (!part.isKerbalEVA() || parts.Count == 1))
                    LoadModuleInventoryPart(part.persistentId, inventoryModule);

                if (!vessel.isEVA && part.protoModuleCrew != null)
                {
                    foreach (ProtoCrewMember protoCrew in part.protoModuleCrew)
                    {
                        ModuleInventoryPart kerbalInventoryModule = protoCrew.KerbalInventoryModule;
                        if (kerbalInventoryModule != null)
                        {
                            kerbalInventoryModule.transform.position = part.transform.position;
                            LoadModuleInventoryPart(protoCrew.persistentID, kerbalInventoryModule);
                        }
                    }
                }
            }
        }

        protected InventoryDisplayItem AddInventoryDisplay(ModuleInventoryPart moduleInventoryPart)
        {
            if (!TryGetDisplayedInventory(moduleInventoryPart, out InventoryDisplayItem displayItem))
            {
                displayItem = new InventoryDisplayItem();
                displayItem.inventoryModule = moduleInventoryPart;
                displayItem.uiObject = Instantiate(uiPartActionInventoryContainerPrefab);
                if (displayItem.uiObject != null)
                {
                    displayItem.uiObject.transform.SetParent(uiPartActionInventoryParent.transform);
                    displayItem.uiObject.transform.localPosition = Vector3.zero;
                    displayItem.uiObject.transform.localScale = Vector3.one;
                    displayItem.uiInventory =
                        displayItem.uiObject.GetComponentInChildren<UIPartActionInventory>(true);
                    if (displayItem.uiInventory != null)
                        displayItem.uiInventory.SetupConstruction(moduleInventoryPart);
                }

                displayedInventories.Add(displayItem);
            }

            return displayItem;
        }

        protected void UpdateDisplayedInventories()
        {
            foreach (KeyValuePair<uint, ModuleInventoryPart> kv in loadedModuleInventoryParts)
            {
                ModuleInventoryPart moduleInventoryPart = kv.Value;
                if (moduleInventoryPart == null) loadedModuleInventoryParts.Remove(kv.Key);
                else AddInventoryDisplay(moduleInventoryPart);
            }
        }

        protected void UpdateInfoLabels()
        {
            TextMeshProUGUI assistingKerbalsLabel = AssistingKerbalsLabel;
            string template;
            template = GameSettings.EVA_CONSTRUCTION_COMBINE_NONENGINEERS ? "#autoLOC_8014163" : "#autoLOC_8014164";

            string str = Localizer.Format(template);
            assistingKerbalsLabel.text = str;
            AssistingKerbalsNumber.text = evaEditor.AssistingKerbals.ToString();
            double num = evaEditor.CombinedConstructionWeightLimit / Math.Max(constructionGravity, 1E-06);
            if (num < 100000.0)
            {
                MaxMassLimitNumber.text =
                    StringBuilderCache.Format("{0:F2}t", (object)(float)(num * (1.0 / 1000.0)));
            }
            else
                MaxMassLimitNumber.text = Localizer.Format("#autoLOC_8014166");
        }

        public void DestroyHeldIcons(UIPartActionInventory callingInventory)
        {
            foreach (InventoryDisplayItem displayedInventory in displayedInventories)
            {
                if (displayedInventory.uiInventory != callingInventory)
                {
                    MethodInfo destroyHeldPart = typeof(UIPartActionInventory).GetMethod("DestroyHeldPart",
                        BindingFlags.NonPublic | BindingFlags.Instance, null, new[]
                            { typeof(bool) }, null);
                    destroyHeldPart.Invoke(displayedInventory.uiInventory, new object[] { true });
                }
            }
        }

        protected bool TryGetDisplayedInventory(ModuleInventoryPart inventoryModule,
            out InventoryDisplayItem displayItem)
        {
            displayItem = displayedInventories.Find(x => x.inventoryModule == inventoryModule);
            return displayItem != null;
        }

        public void OnPointerEnter(PointerEventData eventData) => this.hover = true;

        public void OnPointerExit(PointerEventData eventData) => this.hover = false;

        protected void SpawnMarkerCamera()
        {
            markerCam = new GameObject("markerCam").AddComponent<Camera>();
            markerCam.cullingMask = (int)this.markerCamCullingMask;
            markerCam.orthographic = false;
            markerCam.nearClipPlane = 0.3f;
            markerCam.farClipPlane = 1000f;
            markerCam.depth = 1f;
            markerCam.fieldOfView = 60f;
            markerCam.clearFlags = CameraClearFlags.Depth;
            markerCam.usePhysicalProperties = false;
            markerCam.useOcclusionCulling = true;
            markerCam.allowHDR = false;
            markerCam.transform.SetParent(Camera.main.transform);
            markerCam.transform.localPosition = Vector3.zero;
            markerCam.transform.localRotation = Quaternion.identity;
        }

        protected void OnLoadedScene(GameScenes loadedScene)
        {
            if (loadedScene == GameScenes.FLIGHT) SpawnMarkerCamera();
            SetAppLauncherButtonVisibility();
        }

        protected class InventoryDisplayItem
        {
            public ModuleInventoryPart inventoryModule;
            public GameObject uiObject;
            public UIPartActionInventory uiInventory;
        }
    }
}