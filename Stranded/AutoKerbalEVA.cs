using UnityEngine;

namespace Stranded
{
    public class AutoKerbalEVA : KerbalEVA
    {
        public delegate void ControlCallback(AutoKerbalEVA eva);

        public ControlCallback OnWalkByWire = (AutoKerbalEVA eva) => { };

        protected override void HandleMovementInput()
        {
            base.HandleMovementInput();
            OnWalkByWire(this);
        }
    }
}