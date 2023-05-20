using KSP.UI.Screens;

namespace Stranded.MechBill
{
    public class MechBillJira : VesselModule
    {
        protected override void OnStart()
        {
            GameEvents.OnEVAConstructionMode.Add(OnEVAConstructionMode);
        }

        private void OnDestroy()
        {
            GameEvents.OnEVAConstructionMode.Remove(OnEVAConstructionMode);
        }

        public void OnEVAConstructionMode(bool enabled)
        {
            StageManager.ShowHideStageStack(!enabled);
        }
    }
}