using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using HBS;
using HBS.Extensions;
using UnityEngine;
using static BetterHeadlights.Core;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights.Patches
{
    [HarmonyPatch(typeof(MechRepresentation), "Update")]
    public static class MechRepresentation_Update_Patch
    {
        //private static Stopwatch timer = new Stopwatch();
        private static LightSpawner lightSpawner;

        public static void Postfix(MechRepresentation __instance)
        {
            try
            {
                if (__instance.IsDead)
                    
                {
                    return;
                }

                //timer.Restart();
                if (!mechMap.ContainsKey(__instance.parentMech.GUID))
                {
                    Log(new string('-', 80));
                    Log("MEMOIZING " + __instance.parentMech.DisplayName);

                    __instance
                        .GetComponentsInChildren<Transform>(true)
                        .Where(t => t.name.Contains("headlight"))
                        .Where(t => t.GetComponentInChildren<BTFlare>(true) != null)
                        .Do(Log);

                    var numSpawners = __instance.GetComponentsInChildren<Transform>(true)
                        .Where(t => t.name.Contains("headlight"))
                        .Count(t => t.GetComponentInChildren<BTFlare>(true) != null);

                    // brittle... 
                    lightSpawner = __instance
                        .GetComponentsInChildren<Transform>(true)
                        .LastOrDefault(t => t.GetComponentInChildren<BTFlare>(true) != null)
                        ?.GetComponentInChildren<LightSpawner>(true);

                    // also brittle
                    //lightSpawner = __instance
                    //    .GetComponentsInChildren<Transform>()
                    //    .Where(t => t.GetComponentInChildren<BTFlare>() != null)
                    //    .FirstOrDefault(t => t.name.Contains("Torso"))
                    //    ?.GetComponentInChildren<LightSpawner>();

                    if (lightSpawner != null)
                    {
                        // memoize headlights (should capture new spawns too)
                        Log($"Configure ({lightSpawner}) - ({__instance.parentMech.DisplayName})");
                        lightSpawner.spawnedLight.ConfigureLight();
                        mechMap.Add(__instance.parentMech.GUID, lightSpawner);
                    }
                    else
                    {
                        mechMap.Add(
                            __instance.parentMech.GUID,
                            new GameObject("BetterHeadlightsDummy").AddComponent<LightSpawner>());
                        return;
                    }

                    //if (numSpawners > 1)
                    //{
                    //    var spawners = __instance
                    //        .GetComponentsInChildren<Transform>(true)
                    //        .Where(t => t.name.Contains("headlight"))
                    //        .Where(t => t.GetComponentInChildren<BTFlare>(true) != null)
                    //        .Select(x => x.GetComponentInChildren<LightSpawner>());
                    //    
                    //    lightSpawner = __instance
                    //        .GetComponentsInChildren<Transform>(true)
                    //        .First(t => t.GetComponentInChildren<BTFlare>(true) != null)
                    //        .GetComponentInChildren<LightSpawner>(true);
                    //    lightSpawner.spawnedLight.ConfigureLight();

                    // DESTROY WITH FIRE THE SECOND LIGHT
                    //var otherLight = __instance
                    //    .GetComponentsInChildren<Transform>(true)
                    //    .Where(t => t.name.Contains("headlight"))
                    //    .First(t => t.GetComponentInChildren<BTFlare>(true) != null)
                    //    .GetComponentInChildren<LightSpawner>()
                    //    .transform.gameObject;
                    //
                    //UnityEngine.Object.Destroy(otherLight);
                    //}
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            // Update() runs over by several frames after loading/restarting a mission
            // so hooking separately those is problematic because it repopulates with bad data
            // have to deal with it inline

            lightSpawner = mechMap[__instance.parentMech.GUID];
            if (lightSpawner.spawnedLight == null)
            {
                Log(new string('>', 50) + " Invalid mechs in dictionary, clearing all data");
                mechMap.Clear();
                vehicleMap.Clear();
                headlightsOn = true;
                return;
            }

            // player controlled lights
            if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
            {
                // the goal being to do nothing unless necessary
                if (lightSpawner.spawnedLight.enabled != headlightsOn)
                {
                    Log($"{__instance.parentMech.DisplayName} SetActive: " + headlightsOn);
                    lightSpawner.spawnedLight.enabled = headlightsOn;
                }
            }

            if (settings.BlipLights &&
                !__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam &&
                lightSpawner.transform.gameObject.name != "BetterHeadlightsDummy")
            {
                var localPlayerTeam = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam;
                var visibilityLevel = localPlayerTeam.VisibilityToTarget(__instance.parentActor);
                if (visibilityLevel != VisibilityLevel.None)
                {
                    try
                    {
                        lightSpawner.spawnedLight.ConfigureLight();

                        if (!lightSpawner.transform.parent.gameObject.activeSelf)
                        {
                            lightSpawner.transform.parent.gameObject.SetActive(true);
                        }

                        if (!lightSpawner.spawnedLight.enabled)
                        {
                            lightSpawner.spawnedLight.enabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"harmless?\n" + ex);
                        // do nothing (harmless NREs at load)
                    }
                }
            }

            //Log(timer.Elapsed);
        }
    }
}
