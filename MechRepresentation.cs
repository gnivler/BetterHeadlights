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
                                SpawnedLight = transform.GetComponentInChildren<BTLight>(true),
                                Transform = transform
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
                if (lightTrackers.Any(x => x.Transform == null))
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
                    foreach (var tracker in lightTrackers.Where(x => x.Transform.gameObject.activeSelf != headlightsOn))
                    {
                        Log("SetActive: " + headlightsOn);
                        tracker.Transform.gameObject.SetActive(headlightsOn);
                        tracker.IsAdjusted = !headlightsOn;
                    }

                    foreach (var tracker in lightTrackers.Where(x => !x.IsAdjusted && x.SpawnedLight != null))
                    {
                        Helpers.SetRange(tracker.SpawnedLight, false);
                        Helpers.SetProfile(tracker.SpawnedLight, false);
                        Log("Refreshing");
                        tracker.SpawnedLight.RefreshLightSettings(true);
                        tracker.IsAdjusted = true;
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
                        lightTrackers.Do(tracker => tracker.Transform.gameObject.SetActive(true));
                        // configure them
                        lightTrackers.Where(tracker => !tracker.IsAdjusted)
                            .Where(tracker => tracker.SpawnedLight != null).Do(tracker =>
                            {
                                Log("Enemy");
                                Helpers.SetRange(tracker.SpawnedLight, true);
                                Helpers.SetProfile(tracker.SpawnedLight, true);
                                Log("Refreshing");
                                tracker.SpawnedLight.RefreshLightSettings(true);
                                tracker.IsAdjusted = true;
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