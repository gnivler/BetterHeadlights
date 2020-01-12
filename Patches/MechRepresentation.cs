using System;
using System.Linq;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
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
                        Log("BOMB");
                        return;
                    }
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
                !__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
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
                            Log("Activating GameObject");
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
