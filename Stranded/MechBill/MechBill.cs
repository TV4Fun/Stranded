using JetBrains.Annotations;
using UnityEngine;

namespace Stranded
{
    public class MechBill : KerbalEVA
    {
        public FlightCtrlState ctrlState = new();
        public delegate void ControlCallback(MechBill eva);

        public ControlCallback OnWalkByWire = eva => { };

        public FlightInputCallback OnFlyByWire = st => { };

        [UsedImplicitly]
        [KSPField(guiActive = true, guiName = "Control Linear", isPersistant = false)]
        public Vector3 ctrlLinear;

        [UsedImplicitly]
        [KSPField(guiActive = true, guiName = "Control PYR", isPersistant = false)]
        public Vector3 ctrlPYR;

        protected override void HandleMovementInput()
        {
            base.HandleMovementInput();
            OnWalkByWire(this);
            Quaternion localToWorld = transform.rotation;
            Quaternion worldToLocal = localToWorld.Inverse();
            
            Vector3 xyz = worldToLocal * packTgtRPos;
            ctrlState.X = xyz.x;
            ctrlState.Y = xyz.y;
            ctrlState.Z = xyz.z;

            Vector3 pyr = worldToLocal * cmdRot;
            ctrlState.pitch = pyr.x;
            ctrlState.yaw = pyr.y;
            ctrlState.roll = pyr.z;
            
            OnFlyByWire(ctrlState);

            xyz = ctrlState.GetXYZ();
            pyr = new Vector3(ctrlState.pitch, ctrlState.yaw, ctrlState.roll);  // For some reason, GetPYR() switches around y and z.

            packTgtRPos = localToWorld * xyz;
            
            if (pyr != Vector3.zero)
            {
                manualAxisControl = true;
                cmdRot = localToWorld * pyr;
            }
            else manualAxisControl = false;

            ctrlLinear = xyz;
            ctrlPYR = pyr;
        }

        public void SetPackWaypoint(Vector3 tgtPos)
        {
            packTgtRPos = (tgtPos - transform.position).normalized;
        }

        public override void OnAwake()
        {
            ModuleAttributes.classID = "KerbalEVA".GetHashCode();
            base.OnAwake();
        }
    }
}