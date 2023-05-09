using UnityEngine;

namespace Stranded
{
    public class StrandedKerbalEVA : KerbalEVA
    {
        public delegate void ControlCallback(StrandedKerbalEVA eva);

        public ControlCallback OnWalkByWire = (StrandedKerbalEVA eva) => { };

        protected override void HandleMovementInput()
        {
            base.HandleMovementInput();
            OnWalkByWire(this);
            packTgtRPos = tgtRpos;
        }

        public override void OnAwake()
        {
            ModuleAttributes.classID = "KerbalEVA".GetHashCode();
            base.OnAwake();
        }
    }
}