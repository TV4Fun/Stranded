using UnityEngine;

namespace Stranded
{
    public class AIKerbalEVA : KerbalEVA
    {
        public delegate void ControlCallback(AIKerbalEVA eva);

        public ControlCallback OnWalkByWire = (AIKerbalEVA eva) => { };

        protected override void HandleMovementInput()
        {
            base.HandleMovementInput();
            OnWalkByWire(this);
        }
    }
}