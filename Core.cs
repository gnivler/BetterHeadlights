using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;
using static BetterHeadlights.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace BetterHeadlights
{
    public static class Core
    {
        internal static Settings settings;
        internal static bool headlightsOn = true;
        internal static readonly Dictionary<string, LightSpawner> mechMap = new Dictionary<string, LightSpawner>();
        internal static readonly Dictionary<string, List<Transform>> vehicleMap = new Dictionary<string, List<Transform>>();
        internal static HarmonyInstance harmony;

        internal static readonly Dictionary<string, float> IntensityMap = new Dictionary<string, float>
        {
            {"LOW", 200_000f},
            {"MID", 400_000f},
            {"HIGH", 800_000f},
            {"SUPER", 2_000_000f},
        };

        public static void Init(string modSettings)
        {
            // read settings
            try
            {
                settings = JsonConvert.DeserializeObject<Settings>(modSettings);
            }
            catch (Exception)
            {
                settings = new Settings();
            }

            Log($"Starting up {DateTime.Now.ToShortTimeString()}");
            harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.BetterHeadlights");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal static void Log(object input)
        {
            FileLog.Log($"[BetterHeadlights] {input ?? "null"}");
        }
    }

    // toggle headlights - hotkey hook
    [HarmonyPatch(typeof(CombatGameState), "Update")]
    public static class CombatGameState_Update_Patch
    {
        public static void Postfix()
        {
            var hotkeyH = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                          (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                          Input.GetKeyDown(KeyCode.H);
            if (hotkeyH)
            {
                Log("Toggling headlights");
                headlightsOn = !headlightsOn;
            }
        }
    }
}
