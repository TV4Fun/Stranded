using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Stranded {
  [KSPAddon(KSPAddon.Startup.Instantly, true)]
  public class StrandedLoader : MonoBehaviour {
    private Harmony _harmony;

    private void Awake() {
#if DEBUG
      Harmony.DEBUG = true;
#endif
      _harmony = new Harmony("com.joelcroteau.stranded");
      Assembly assembly = Assembly.GetExecutingAssembly();
      _harmony.PatchAll(assembly);
    }
  }
}
