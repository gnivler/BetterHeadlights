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

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace BetterHeadlights
{
    public static class Core
    {
        private static Settings settings;
        private static bool headlightsOn = true;

        private static readonly Dictionary<string, float> IntensityMap = new Dictionary<string, float>
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

            Log("Starting up");
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.BetterHeadlights");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void Log(object input)
        {
            //FileLog.Log($"[BetterHeadlights] {input}");
        }

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

        [HarmonyPatch(typeof(LightSpawner), "SpawnLight")]
        public class LightSpawner_SpawnLight_Patch
        {
            [HarmonyPriority(Priority.Low)]
            public static bool Prefix(LightSpawner __instance)
            {
                var GO = (Component) __instance;

                // how this info was found:
                //GO.GetComponentsInChildren<Component>().Do(x => Log(x.ToString()));
                // we want to return true and let the transpiler go if it matches
                // limited options for determining if this is a mech light
                return GO.GetComponentsInChildren<Component>()
                    .Any(x => Regex.IsMatch(x.name.ToLower(), @"torso|shoulder"));
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var spawnedLightField = AccessTools.Field(typeof(LightSpawner), "spawnedLight");
                var radiusField = AccessTools.Field(typeof(BTLight), "radius");
                var intensityField = AccessTools.Field(typeof(BTLight), "intensity");
                var outerAngleField = AccessTools.Field(typeof(BTLight), "spotlightAngleOuter");

                // just setup the field values and insert at the end
                var setupCodes = new List<CodeInstruction>();
                if (settings.ExtraRange)
                {
                    Log("Applying ExtraRange");
                    SetRange(setupCodes, spawnedLightField, radiusField);
                }

                if (settings.Intensity != "VANILLA" &&
                    IntensityMap.ContainsKey(settings.Intensity))
                {
                    Log("Applying Intensity");
                    SetIntensity(setupCodes, spawnedLightField, intensityField);
                }

                Log("Applying Angle");
                SetAngle(setupCodes, spawnedLightField, outerAngleField);
                codes.InsertRange(codes.Count - 1, setupCodes);
                return codes.AsEnumerable();
            }

            private static void SetAngle(List<CodeInstruction> setupCodes, FieldInfo spawnedLightField, FieldInfo outerAngleField)
            {
                setupCodes.InsertRange(0, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, spawnedLightField),
                    new CodeInstruction(OpCodes.Ldc_R4, settings.Angle),
                    new CodeInstruction(OpCodes.Stfld, outerAngleField)
                });
            }

            private static void SetIntensity(List<CodeInstruction> setupCodes, FieldInfo spawnedLight, FieldInfo intensityField)
            {
                setupCodes.InsertRange(0, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, spawnedLight),
                    new CodeInstruction(OpCodes.Ldc_R4, IntensityMap[settings.Intensity]),
                    new CodeInstruction(OpCodes.Stfld, intensityField)
                });
            }

            private static void SetRange(List<CodeInstruction> setupCodes, FieldInfo spawnedLightField, FieldInfo radiusField)
            {
                setupCodes.InsertRange(0, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, spawnedLightField),
                    new CodeInstruction(OpCodes.Ldc_R4, 10_000f),
                    new CodeInstruction(OpCodes.Stfld, radiusField)
                });
            }
        }

        // light toggling
        [HarmonyPatch(typeof(PilotableActorRepresentation), "Update")]
        public static class PilotableActorRepresentation_Update_Patch
        {
            public static void Postfix(PilotableActorRepresentation __instance)
            {
                if (__instance.parentActor is Mech mech)
                {
                    var lights = __instance.gameObject.GetComponentsInChildren<Component>(true)
                        .Where(x => mech.pilot.Team.LocalPlayerControlsTeam)
                        .Where(x => x.name.Contains("headlight")).ToList();

                    if (!lights.Any())
                    {
                        return;
                    }

                    foreach (var light in lights)
                    {
                        light.gameObject.SetActive(headlightsOn);
                    }
                }
            }
        }

        private class Settings
        {
            public bool ExtraRange = true;
            public string Intensity = "MID";
            public float Angle = 45;
        }
    }
}
