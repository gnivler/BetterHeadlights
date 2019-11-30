using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using UnityEngine;
using static BetterHeadlights.Core;

namespace BetterHeadlights
{
    [HarmonyPatch(typeof(MechRepresentation), "Update")]
    public static class MechRepresentation_Update_Patch
    {
        public static void Postfix(MechRepresentation __instance)
        {
            try
            {
                // memoize all mech lights (should capture new spawns too)
                if (!trackerMap.ContainsKey(__instance.parentMech.GUID))
                {
                    var lightTransforms = __instance.GetComponentsInChildren<Transform>(true)
                        // ReSharper disable once StringLiteralTypo
                        .Where(x => x.name.Contains("centertorso_headlight")).ToList();

                    Log($"Adding mech {__instance.parentMech.MechDef.Name} with {lightTransforms.Count} lights");
                    trackerMap.Add(__instance.parentMech.GUID, GetLightTrackers());

                    // build LightTrackers
                    List<LightTracker> GetLightTrackers()
                    {
                        var lighTrackers = new List<LightTracker>();
                        foreach (var transform in lightTransforms)
                        {
                            lighTrackers.Add(new LightTracker()
                            {
                                spawnedLight = transform.GetComponentInChildren<BTLight>(true),
                                transform = transform
                            });
                        }

                        return lighTrackers;
                    }
                }

                // Update() runs over by several frames after loading/restarting a mission
                // so hooking separately those is problematic because it repopulates with bad data
                // have to deal with it inline
                // if this mech tracker has no valid lights
                var lightTrackers = trackerMap[__instance.parentMech.GUID];
                if (lightTrackers.Any(x => x.transform == null))
                {
                    Log(new string('>', 50) + " Invalid mechs in dictionary, clearing");
                    trackerMap.Clear();
                    headlightsOn = true;
                    return;
                }

                // player controlled lights
                if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
                {
                    // Where clause should return Count 0 if the lights are already set, skipping SetActive()
                    // the goal being to do nothing unless necessary
                    foreach (var tracker in lightTrackers.Where(x => x.transform.gameObject.activeSelf != headlightsOn))
                    {
                        Log("SetActive: " + headlightsOn);
                        tracker.transform.gameObject.SetActive(headlightsOn);
                        tracker.adjusted = !headlightsOn;
                    }

                    foreach (var tracker in lightTrackers.Where(x => !x.adjusted && x.spawnedLight != null))
                    {
                        Helpers.SetRange(tracker.spawnedLight, false);
                        Helpers.SetProfile(tracker.spawnedLight, false);
                        tracker.adjusted = true;
                    }
                }
                else if (settings.BlipLights)
                {
                    try
                    {
                        var localPlayerTeam = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam;
                        var visibilityLevel = localPlayerTeam.VisibilityToTarget(__instance.parentActor);
                        if (visibilityLevel == VisibilityLevel.None || visibilityLevel == VisibilityLevel.LOSFull)
                        {
                            return;
                        }

                        // enemy mech is a blip, lights on
                        lightTrackers.Do(tracker => tracker.transform.gameObject.SetActive(true));
                        // configure them
                        lightTrackers.Where(tracker => !tracker.adjusted)
                            .Where(tracker => tracker.spawnedLight != null).Do(tracker =>
                            {
                                Log("Enemy");
                                Helpers.SetRange(tracker.spawnedLight, true);
                                Helpers.SetProfile(tracker.spawnedLight, true);
                                tracker.adjusted = true;
                            });
                    }
                    catch (NullReferenceException)
                    {
                        // do nothing (harmless NREs at load)
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
    }
}
