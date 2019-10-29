using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using BattleTech.Rendering;
using Harmony;
using HBS;
using Newtonsoft.Json;
using UnityEngine;
using Object = System.Object;

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
            FileLog.Log($"[BetterHeadlights] {input ?? "null"}");
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

                Log($"adjusting: {__instance.name} ({__instance.transform.parent.name})");
                if (settings.ExtraRange)
                {
                    ___spawnedLight.radius = extraRadius;
                }

                if (settings.Intensity != "VANILLA" &&
                    IntensityMap.ContainsKey(settings.Intensity))
                {
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity];
                }

                ___spawnedLight.spotlightAngleOuter = settings.Angle;

                ___spawnedLight.volumetricsMultiplier = 1000f;
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


        [HarmonyPatch(typeof(PilotableActorRepresentation), "Update")]
        public static class PilotableActorRepresentation_Update_Patch
        {
            public static void Postfix(PilotableActorRepresentation __instance)
            {
                try
                {
                    // only want to patch when the mech is a blip
                    var localPlayerTeam = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam;
                    var visibilityLevel = localPlayerTeam.VisibilityToTarget(__instance.parentActor);
                    if (visibilityLevel == VisibilityLevel.None || visibilityLevel == VisibilityLevel.LOSFull)
                    {
                        return;
                    }

                    Log($"{__instance.parentActor.DisplayName,-40}: {visibilityLevel.ToString()}");
                    var lights = __instance.VisibleLights;

                    // there is one (of four) parent transforms which is not active
                    // is has to be enabled for the lights to be visible but that makes the whole vee appear
                    // so enable the transform but disable its children so it stays invisible but for lights?
                    if (__instance is VehicleRepresentation)
                    {
                        foreach (var light in lights)
                        {
                            var transform = light.GetComponentsInParent<Transform>(true)
                                .FirstOrDefault(t => !t.gameObject.activeSelf);
                            if (transform == null)
                            {
                                return;
                            }

                            // found the inactive transform, activate it but disable the mesh
                            transform.gameObject.SetActive(true);
                            foreach (var child in transform.GetComponentsInChildren<Component>())
                            {
                                if (child is SkinnedMeshRenderer skinnedMesh)
                                {
                                    skinnedMesh.enabled = false;
                                }
                            }

                            light.enabled = true;
                        }
                    }

                    //Log(timer.Elapsed);
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
                var lights = __instance.GetComponentsInChildren<BTLight>(true);
                lights.Do(Log);
                // player controlled lights
                if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
                {
                    lights.Do(x => x.enabled = headlightsOn);
                    return;
                }

                // enemy mech, lights on
                lights.Do(x => x.enabled = true);
                //lights = __instance.GetComponentsInChildren<Component>(true).ToList();
                //lights.Where(c => c.name.Contains("light")).Do(x => x.gameObject.SetActive(headlightsOn));
                // player headlights are controlled
            }
        }
    }

    public class Settings
    {
        public bool ExtraRange = true;
        public string Intensity = "MID";
        public float Angle = 45;
    }
}