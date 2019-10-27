using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            Log($"Starting up {DateTime.Now.ToShortTimeString()}");
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.BetterHeadlights");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void Log(object input)
        {
            FileLog.Log($"[BetterHeadlights] {input}");
        }

        // adjust headlight settings
        [HarmonyPatch(typeof(LightSpawner), "SpawnLight")]
        public class LightSpawner_SpawnLight_Patch
        {
            private const float extraRadius = 10_000f;

            // target spot lights only and configure them per the settings
            public static void Postfix(LightSpawner __instance, BTLight ___spawnedLight)
            {
                if (__instance.type == LightSpawner.LightTypes.point)
                {
                    return;
                }

                Log($"adjusting: {__instance.name}");
                if (settings.ExtraRange)
                {
                    //Log("Applying ExtraRange");
                    ___spawnedLight.radius = extraRadius;
                }

                if (settings.Intensity != "VANILLA" &&
                    IntensityMap.ContainsKey(settings.Intensity))
                {
                    //Log("Applying Intensity");
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity];
                }

                ___spawnedLight.spotlightAngleOuter = settings.Angle;
                //Log("Applying Angle");
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

        // TODO this doesn't make the lights appear 
        [HarmonyPatch(typeof(VehicleRepresentation), "Update")]
        public static class VehicleRepresentation_Update_Patch
        {
            public static void Postfix(VehicleRepresentation __instance)
            {
                try
                {
                    var lights = __instance.GetComponentsInChildren<BTLight>(true);
                    if (!lights.Any())
                    {
                        Log("No lights");
                        return;
                    }

                    foreach (var light in lights)
                    {
                        light.gameObject.SetActive(true);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        // toggle headlights - effect
        [HarmonyPatch(typeof(MechRepresentation), "Update")]
        public static class MechRepresentation_Update_Patch
        {
            public static void Postfix(MechRepresentation __instance)
            {
                // player headlights are controlled
                if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
                {
                    __instance.ToggleHeadlights(headlightsOn);
                    return;
                }

                // any enemy within sensor range of any friendly has lights on
                var combat = __instance.parentCombatant.Combat;
                var friendlies = combat.AllActors.Where(actor => actor.team.LocalPlayerControlsTeam).ToList();
                foreach (var friendly in friendlies)
                {
                    var enemies = friendly.GetDetectedEnemyUnits()
                        .Where(enemy => friendly.VisibilityToTargetUnit(enemy) >= VisibilityLevel.Blip0Minimum)
                        .ToList();
                    if (enemies.Count <= 0)
                    {
                        continue;
                    }

                    // it's an enemy mech, lights on
                    __instance.ToggleHeadlights(true);
                }
            }
        }
    }
}

public class Settings
{
    public bool ExtraRange = true;
    public string Intensity = "MID";
    public float Angle = 45;
}