using UnityEngine;

namespace Stranded.MechBill {
  public class TaskTarget : PartModule, ITargetable {
    public bool GetActiveTargetable() => true;
    public string GetDisplayName() => name;
    public Vector3 GetFwdVector() => transform.forward;
    public string GetName() => name;

    public Vector3 GetObtVelocity() => vessel.obt_velocity;
    public Orbit GetOrbit() => vessel.orbit;
    public OrbitDriver GetOrbitDriver() => vessel.orbitDriver;
    public Vector3 GetSrfVelocity() => vessel.srf_velocity;
    public VesselTargetModes GetTargetingMode() => VesselTargetModes.DirectionVelocityAndOrientation;
    public Transform GetTransform() => transform;
    public Vessel GetVessel() => vessel;

    public Task Task;

    [KSPEvent(guiActive = true, guiName = "Cancel")]
    public void Cancel() {
      Task.Cancel();
    }
  }
}
