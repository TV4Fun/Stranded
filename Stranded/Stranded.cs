using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stranded
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Stranded : MonoBehaviour
    {
        //private GameObject sphere;
        private Vector3 target;
        private List<MechBill.MechBill> kerbals;
        private Camera mainCamera;

        public void Awake()
        {
            Debug.Log("Stranded.Awake()");
        }

        public void AttachToKerbal(GameEvents.FromToAction<Part, Part> action)
        {
            action.to.GetComponent<MechBill.MechBill>().OnFlyByWire += controlPitch;
        }

        public void Start()
        {
            /*sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;
            sphere.transform.SetParent(SpaceCenter.Instance.SpaceCenterTransform);
            sphere.transform.localScale = 1.0f * Vector3.one;*/

            kerbals = new List<MechBill.MechBill>();
            mainCamera = Camera.main;
            GameEvents.onCrewOnEva.Add(AttachToKerbal);
            // sphere.SetLayerRecursive(2);

            /*for (int i = 0; i < 32; ++i)
            {
                Debug.Log("Layer " + i + ": " + LayerMask.LayerToName(i));
            }*/

            /*Scene activeScene = SceneManager.GetActiveScene();
            
            foreach (GameObject obj in activeScene.GetRootGameObjects())
            {
                Debug.Log("Parent: " + obj.name);
                Debug.Log("Children:");
                foreach (Transform child in obj.transform)
                {
                    Debug.Log(child.name);
                }
            }*/
        }

        public void Update()
        {
            // Debug.Log("Hello world! " + Time.realtimeSinceStartup);
            RaycastHit hit;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            /*Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            Debug.Log("Mouse.screenPos: " + Mouse.screenPos);
            Debug.Log("Input.mousePosition: " + Input.mousePosition);
            Debug.Log("Casting ray " + ray);*/
            bool isHit = Physics.Raycast(ray, out hit, 10000.0f /*, 1 << 15*/);
            if (!isHit)
            {
                //Debug.Log("No hit");
            }
            else
            {
                //Debug.Log("Hit! Collider " + hit.collider.name + " transform " + hit.transform.name + " at " +
                //          hit.point + ", layer " + hit.transform.gameObject.layer + ", distance " + hit.distance);
                //sphere.transform.position = hit.point;
                target = hit.point;
                if (Input.GetKeyDown(KeyCode.J))
                {
                    MechBill.MechBill eva = SpawnAIKerbal(hit.point);
                    eva.OnWalkByWire += FlyKerbal;
                    kerbals.Add(eva);
                }
            }
        }

        protected void controlPitch(FlightCtrlState state)
        {
            if (Input.GetKey(KeyCode.Keypad8)) state.pitch = 1.0f;
            if (Input.GetKey(KeyCode.Keypad2)) state.pitch = -1.0f;
            if (Input.GetKey(KeyCode.Keypad4)) state.yaw = -1.0f;
            if (Input.GetKey(KeyCode.Keypad6)) state.yaw = 1.0f;
            if (Input.GetKey(KeyCode.Keypad7)) state.roll = -1.0f;
            if (Input.GetKey(KeyCode.Keypad9)) state.roll = 1.0f;
        }

        protected void FlyKerbal(MechBill.MechBill eva)
        {
            eva.SetWaypoint(target);
        }

        public MechBill.MechBill SpawnAIKerbal(Vector3 position)
        {
            ProtoCrewMember nextOrNewKerbal = HighLogic.CurrentGame.CrewRoster.GetNextOrNewKerbal();
            MechBill.MechBill eva = (MechBill.MechBill)FlightEVA.Spawn(nextOrNewKerbal);

            eva.gameObject.SetActive(true);
            eva.part.vessel = eva.gameObject.AddComponent<Vessel>();
            eva.vessel.Initialize();
            eva.vessel.id = Guid.NewGuid();
            eva.transform.position = position;
            eva.GetComponent<Rigidbody>().velocity = Vector3.zero;
            eva.part.AddCrewmember(nextOrNewKerbal);
            eva.gameObject.name = nextOrNewKerbal.GetKerbalEVAPartName() + " (" + nextOrNewKerbal.name + ")";
            eva.vessel.vesselName = nextOrNewKerbal.name;
            eva.vessel.vesselType = VesselType.EVA;
            eva.vessel.launchedFrom = FlightGlobals.ActiveVessel.launchedFrom;
            eva.vessel.orbit.referenceBody = FlightGlobals.getMainBody((Vector3d)eva.transform.position);
            eva.part.flagURL = FlightGlobals.ActiveVessel.rootPart.flagURL;
            eva.part.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            eva.part.missionID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            eva.part.launchID = FlightGlobals.ActiveVessel.rootPart.launchID;
            // GameEvents.onCrewOnEva.Fire(new GameEvents.FromToAction<Part, Part>((Part) null, eva.part));
            // GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(nextOrNewKerbal, (Part) null, eva.part));
            // Vessel.CrewWasModified(eva.vessel);
            // this.StartCoroutine(this.SwitchToEVAVesselWhenReady(eva));

            return eva;
        }
    }
}