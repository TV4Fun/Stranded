using UnityEngine;

namespace Stranded.MechBill {
  public static class Globals {
    public static readonly Color GhostPartHighlightColor = new(0.0f, 1.0f, 1.0f, 1.0f);
    public const int GhostLayer = 3;
    public static bool ShowDebugOverlay = false;  // Set to true for added navigation visuals.
  }
}
