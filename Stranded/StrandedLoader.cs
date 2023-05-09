using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Stranded
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class StrandedLoader : MonoBehaviour
    {
        private Harmony harmony;
        private void Awake()
        {
#if DEBUG
            Harmony.DEBUG = true;
#endif
            harmony = new Harmony("com.joelcroteau.stranded");
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
    }
}