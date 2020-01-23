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
                    //lightSpawner = __instance
                    //    .GetComponentsInChildren<Transform>(true)
                    //    .LastOrDefault(t => t.GetComponentInChildren<BTFlare>(true) != null)
                    //    ?.GetComponentInChildren<LightSpawner>(true);

                    // also brittle
                    //lightSpawner = __instance
                    //    .GetComponentsInChildren<Transform>()
                    //    .Where(t => t.GetComponentInChildren<BTFlare>() != null)
                    //    .FirstOrDefault(t => t.name.Contains("Torso"))
                    //    ?.GetComponentInChildren<LightSpawner>();
                                
                    var btLights = __instance
                        .GetComponentsInChildren<Transform>(true)
                        .Select(x => x.GetComponentInChildren<BTLight>(true))
                        .Where(y => y != null)
                        .Where(z => z.lightType == BTLight.LightTypes.Spot)
                        .ToList();

                    if (btLights.Any())
                    {
                        // memoize headlights (should capture new spawns too)
                        Log($"Configure {__instance.parentMech.DisplayName}");
                        btLights.Do(x =>
                        {
                            Log("\t" + x);
                            x.ConfigureLight();
                        });
                        mechMap.Add(__instance.parentMech.GUID, btLights);
                    }
                    else
                    {
                        mechMap.Add(__instance.parentMech.GUID,
                            new List<BTLight>
                            {
                                new GameObject("BetterHeadlightsDummy").AddComponent<BTLight>()
                            });

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
            var lights = mechMap[__instance.parentMech.GUID];
            if (mechMap[__instance.parentMech.GUID][0] == null)
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
                if (lights.Any(x => x.enabled != headlightsOn))
                {
                    Log($"{__instance.parentMech.DisplayName} SetActive: " + headlightsOn);
                    lights.Do(x => x.enabled = headlightsOn);
                }

                lights.Do(x => x.ConfigureLight());
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
                        lights
                            .Where(x => x.gameObject.name != "BetterHeadlightsDummy")
                            .Do(x =>
                            {
                                //x.ConfigureLight();
                                x.transform.parent.gameObject.SetActive(true);
                                if (!x.enabled)
                                {
                                    x.enabled = true;
                                }
                            });
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
