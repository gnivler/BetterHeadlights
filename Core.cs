using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable InconsistentNaming

public static class Core
{
    private static Settings settings;
    private static bool headlightsOn = true;

    private static Dictionary<string, float> IntensityMap = new Dictionary<string, float>
    {
        {"LOW", 200_000f},
        {"MID", 400_000f},
        {"HIGH", 800_000},
        {"SUPER", 2_000_000}
    };

    public static void Init(string modDir, string settings)
    {
        // read settings
        try
        {
            Core.settings = JsonConvert.DeserializeObject<Settings>(settings);
            Core.settings.modDirectory = modDir;
        }
        catch (Exception)
        {
            Core.settings = new Settings();
        }

        Log("Starting up");
        var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.BetterHeadlights");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void Log(string input)
    {
        //FileLog.Log($"[BetterHeadlights] {(string.IsNullOrEmpty(input) ? "EMPTY" : input)}");
    }

    [HarmonyPatch(typeof(CombatGameState), "Update")]
    public static class CombatGameState_Update_Patch
    {
        public static void Postfix()
        {
            var hotkeyH = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                          (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.H);
            if (hotkeyH)
                headlightsOn = !headlightsOn;
        }
    }

    [HarmonyPatch(typeof(LightSpawner), "SpawnLight")]
    public static class LightSpawner_SpawnLight_Patch
    {
        [HarmonyPriority(Priority.Low)]
        public static bool Prefix(LightSpawner __instance)
        {
            var GO = (Component) __instance;

            //GO.GetComponentsInChildren<Component>().Do(x => Log(x.ToString()));
            // limited options for determining if this is a mech
            return GO.GetComponentsInChildren<Component>()
                .Any(x => Regex.IsMatch(x.name.ToLower(), @"torso|shoulder"));
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            var radiusTarget = codes.FindLastIndex(x => x.opcode == OpCodes.Stfld && x.operand == AccessTools.Field(typeof(BTLight), "lightType")) + 1;
            var intensityTarget = codes.FindLastIndex(x => x.opcode == OpCodes.Stfld && x.operand == AccessTools.Field(typeof(BTLight), "radius")) + 1;
            var spotTarget = codes.FindLastIndex(x => x.opcode == OpCodes.Stfld && x.operand == AccessTools.Field(typeof(BTLight), "spotlightAngleInner")) + 1;

            // replace radius (range) assignment
            for (var i = radiusTarget + 2; i < radiusTarget + 5; i++)
            {
                codes[i].opcode = OpCodes.Nop;
            }

            // vanilla is 60
            codes[radiusTarget + 5].opcode = OpCodes.Ldc_R4;
            codes[radiusTarget + 5].operand = settings.ExtraRange ? 10_000f : 60f;

            // spot outer
            codes[spotTarget + 2].opcode = OpCodes.Ldc_R4;
            codes[spotTarget + 2].operand = settings.Angle;

            // replace intensity assignment
            for (var i = intensityTarget + 2; i < intensityTarget + 6; i++)
            {
                codes[i].opcode = OpCodes.Nop;
            }

            codes[intensityTarget + 5].opcode = OpCodes.Ldc_R4;
            codes[intensityTarget + 5].operand = IntensityMap[settings.Intensity];

            // spot outer
            codes[spotTarget + 2].opcode = OpCodes.Ldc_R4;
            codes[spotTarget + 2].operand = settings.Angle;
            return codes.AsEnumerable();
        }
    }

    // light toggling
    [HarmonyPatch(typeof(PilotableActorRepresentation), "Update")]
    public static class PilotableActorRepresentation_Update_Patch
    {
        private static List<Component> lightList = new List<Component>();

        public static void Postfix(PilotableActorRepresentation __instance)
        {
            if (__instance.parentActor is Mech mech)
            {
                var lights = __instance.gameObject.GetComponentsInChildren<Component>()
                    .Where(x => x.name.Contains("headlight"))
                    .Where(x => mech.pilot.Team.LocalPlayerControlsTeam).ToList();

                if (headlightsOn && lightList.Count != 0)
                {
                    foreach (var disabledLight in lightList)
                    {
                        disabledLight.gameObject.SetActive(true);
                    }
                }
                else if (!headlightsOn)
                {
                    foreach (var enabledLight in lights)
                    {
                        // cache the light components because they vanish from 
                        // the child component list for reasons unknown to me
                        if (!lightList.Contains(enabledLight))
                            lightList.Add(enabledLight);
                        enabledLight.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public class Settings
    {
        public bool enableDebug;
        public string modDirectory;

        public string Range;
        public bool ExtraRange;
        public string Intensity;
        public float Angle;
    }
}
