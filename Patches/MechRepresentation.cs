using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using HBS;
using UnityEngine;
using static BetterHeadlights.Core;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights.Patches
{
    [HarmonyPatch(typeof(MechRepresentation), "Update")]
    public static class MechRepresentation_Update_Patch
    {
        private static Stopwatch timer = new Stopwatch();

        public static void Postfix(MechRepresentation __instance, List<GameObject> ___headlightReps)
        {
            try
            {
                timer.Restart();
                // memoize headlights (should capture new spawns too)
                if (!mechMap.ContainsKey(__instance.parentMech.GUID))
                {
                    //__instance.GetComponentsInChildren<Component>().Do(Log);
                    var lightSpawner = ___headlightReps
                        .First(x => x.name.Contains("centertorso_headlight"))
                        .GetComponentInChildren<LightSpawner>();

                    var headlight = lightSpawner.transform.parent.gameObject;

                    // just delete the LightSpawner and remake it with the patch in place
                    Log($"Remake {headlight.name} ({__instance.parentMech.DisplayName})");
                    Helpers.RemakeLight(headlight.transform);
                    mechMap.Add(__instance.parentMech.GUID, new LightTracker
                    {
                        SpawnedLight = headlight.GetComponentInChildren<BTLight>(),
                        HeadlightTransform = headlight.transform
                    });

                    mechMap[__instance.parentMech.GUID]?.SpawnedLight.RefreshLightSettings(true);
                    Log(timer.Elapsed);
                }

                // Update() runs over by several frames after loading/restarting a mission
                // so hooking separately those is problematic because it repopulates with bad data
                // have to deal with it inline
                var lightTracker = mechMap[__instance.parentMech.GUID];
                if (lightTracker.HeadlightTransform == null)
                {
                    Log(new string('>', 50) + " Invalid mechs in dictionary, clearing");
                    mechMap.Clear();
                    headlightsOn = true;
                    return;
                }

                // player controlled lights
                if (__instance.pilotRep.pilot.Team.LocalPlayerControlsTeam)
                {
                    // the goal being to do nothing unless necessary
                    if (lightTracker.HeadlightTransform.gameObject.activeSelf != headlightsOn)
                    {
                        // when the lights are reactivated they are recreated without
                        // custom settings, unless patched and remade
                        // BUG the lights don't pick up the new settings here...
                        Log($"{__instance.parentMech.DisplayName} SetActive: " + headlightsOn);
                        if (headlightsOn)
                        {
                            Log("Remaking in toggle");
                            lightTracker.HeadlightTransform.gameObject.SetActive(true);
                            Helpers.RemakeLight(lightTracker.HeadlightTransform);
                            lightTracker.SpawnedLight.RefreshLightSettings(true);
                        }
                        else
                        {
                            lightTracker.HeadlightTransform.gameObject.SetActive(false);
                        }
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
                        if (!lightTracker.HeadlightTransform.gameObject.activeSelf)
                        {
                            lightTracker.HeadlightTransform.gameObject.SetActive(true);
                        }
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
