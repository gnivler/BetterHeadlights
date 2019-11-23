using System;
using System.Collections.Generic;
using System.Linq;
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

        internal static Dictionary<string, List<Transform>> lightTracker =
            new Dictionary<string, List<Transform>>();

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
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.BetterHeadlights");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal static void Log(object input)
        {
            //FileLog.Log($"[BetterHeadlights] {input ?? "null"}");
        }
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
                if (__instance.name.StartsWith("light"))
                {
                    // magic numbers.. sorry.  adjusted to visuals
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity] / 5f;
                    ___spawnedLight.spotlightAngleOuter = settings.Angle * 1.20f;
                }
                else
                {
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity];
                    ___spawnedLight.spotlightAngleOuter = settings.Angle;
                }
            }
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

    [HarmonyPatch(typeof(VehicleRepresentation), "Update")]
    public static class VehicleRepresentation_Update_Patch
    {
        public static void Postfix(VehicleRepresentation __instance)
        {
            try
            {
                if (!settings.BlipLights)
                {
                    return;
                }

                // only want to patch blips
                var localPlayerTeam = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam;
                var visibilityLevel = localPlayerTeam.VisibilityToTarget(__instance.parentActor);
                if (visibilityLevel == VisibilityLevel.None)
                {
                    return;
                }

                Transform transform;
                // there is one parent transform which is not active
                // only try to find the transform if not memoized
                var vehicleGUID = __instance.parentVehicle.GUID;
                if (!lightTracker.ContainsKey(vehicleGUID))
                {
                    Log($"Adding vehicle {__instance.parentVehicle.Nickname} with {__instance.VisibleLights.Length} lights");
                    foreach (var light in __instance.VisibleLights)
                    {
                        transform = light.GetComponentsInParent<Transform>(true)
                            .FirstOrDefault(x => !x.gameObject.activeSelf);
                        if (transform != null)
                        {
                            // add single inactive transform
                            lightTracker.Add(__instance.parentVehicle.GUID, new List<Transform> {transform});
                            break;
                        }
                    }

                    // it doesn't have any inactive transforms so we don't care about it really
                    // missiles seem to run this method.  add it so it doesn't check every frame
                    if (!lightTracker.ContainsKey(vehicleGUID))
                    {
                        lightTracker.Add(__instance.parentVehicle.GUID, new List<Transform> {__instance.transform.parent});
                    }
                }

                // grab the inactive transform, activate it but disable the mesh
                transform = lightTracker[__instance.parentVehicle.GUID].FirstOrDefault();
                if (transform != null)
                {
                    transform.gameObject.SetActive(true);
                    var mesh = transform.GetComponentInChildren<SkinnedMeshRenderer>();
                    foreach (var child in transform.GetComponentsInChildren<Component>())
                    {
                        if (child is SkinnedMeshRenderer)
                        {
                            mesh.enabled = visibilityLevel == VisibilityLevel.LOSFull;
                        }

                        if (child is BTLight btLight)
                        {
                            btLight.enabled = visibilityLevel >= VisibilityLevel.Blip0Minimum;
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                // do nothing (harmless NREs at game load)
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
            try
            {
                if (!settings.BlipLights)
                {
                    return;
                }

                // memoize all mech lights (should capture new spawns too)
                if (!lightTracker.ContainsKey(__instance.parentMech.GUID))
                {
                    var transforms = __instance.GetComponentsInChildren<Transform>(true)
                        .Where(x => x.name.Contains("headlight")).ToList();
                    Log($"Adding mech {__instance.parentMech.MechDef.Name} with {transforms.Count} lights");
                    lightTracker.Add(__instance.parentMech.GUID, transforms);
                }

                // Update() runs over by several frames after loading/restarting a mission
                // so hooking separately those is problematic because it repopulates with bad data
                // have to deal with it inline
                var lights = lightTracker[__instance.parentMech.GUID].Where(x => x != null).ToList();
                if (lights.Count == 0)
                {
                    Log(new string('>', 100) + " Invalid mechs in dictionary, clearing");
                    lightTracker.Clear();
                    headlightsOn = true;
                    return;
                }

                // player controlled lights
                if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
                {
                    // Where clause should return Count 0 if the lights are already set, skipping SetActive()
                    foreach (var light in lights.Where(x => x.gameObject.activeSelf != headlightsOn))
                    {
                        light.gameObject.SetActive(headlightsOn);
                    }
                }

                try
                {
                    var localPlayerTeam = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam;
                    var visibilityLevel = localPlayerTeam.VisibilityToTarget(__instance.parentActor);
                    if (visibilityLevel == VisibilityLevel.None || visibilityLevel == VisibilityLevel.LOSFull)
                    {
                        return;
                    }

                    // enemy mech is a blip, lights on
                    lights.Do(light => light.gameObject.SetActive(true));
                }
                catch (NullReferenceException)
                {
                    // do nothing (harmless NREs at load)
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
    }
}

public class Settings
{
    public bool ExtraRange = true;
    public string Intensity = "MID";
    public float Angle = 45;
    public bool BlipLights = true;
}
