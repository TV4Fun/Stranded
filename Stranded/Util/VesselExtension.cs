using System.Collections.Generic;
using UnityEngine;

namespace Stranded.Util {
  public static class VesselExtension {
    public static Bounds CalculateCraftBounds(this Vessel vessel) {
      List<Part> parts = vessel.Parts;
      Part rootPart = vessel.rootPart;
      if (parts.Count == 0 || rootPart == null) {
        return new Bounds();
      }

      Transform rootTransform = rootPart.transform;
      List<Bounds> localBoundsList = new();

      foreach (Part part in parts) {
        if (part.Modules.GetModule<LaunchClamp>() == null) {
          Bounds[] worldBounds = PartGeometryUtil.GetPartColliderBounds(part);
          foreach (Bounds worldBound in worldBounds) {
            Bounds localBound = new(rootTransform.InverseTransformPoint(worldBound.center),
              rootTransform.InverseTransformVector(worldBound.size));

            localBound.size *= part.boundsMultiplier;
            localBound.Expand(part.GetModuleSize(localBound.size));
            localBoundsList.Add(localBound);
          }
        }
      }

      if (localBoundsList.Count > 0) {
        Bounds result = localBoundsList[0];
        for (int i = 1; i < localBoundsList.Count; i++) {
          result.Encapsulate(localBoundsList[i]);
        }

        return result;
      }

      return new Bounds();
    }
  }
}