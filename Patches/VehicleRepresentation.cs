using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using UnityEngine;
using static BetterHeadlights.Core;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights.Patches
{
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
                // only try to find the transform if not memoized
                // there is one parent transform which is not active
                var vehicleGUID = __instance.parentVehicle.GUID;
                if (!vehicleMap.ContainsKey(vehicleGUID))
                {
                    Log($"Adding vehicle {__instance.parentVehicle.Nickname} with {__instance.VisibleLights.Length} lights");
                    foreach (var light in __instance.VisibleLights)
                    {
                        //Log($"Remaking {light.transform}");
                        //Helpers.RemakeLight(light.transform);
                        transform = light.GetComponentsInParent<Transform>(true)
                            .FirstOrDefault(x => !x.gameObject.activeSelf);
                        if (transform != null)
                        {
                            // add single inactive transform
                            // adjust instead of Remake
                            //var lightSpawner = transform.gameObject.GetComponentInChildren<LightSpawner>();
                            var btLight = transform.gameObject.GetComponentInChildren<BTLight>();
                            UnityEngine.Object.Destroy(btLight);
                            var newLight = transform.gameObject.AddComponent<BTLight>();
                            newLight.ConfigureLight(true);
                            vehicleMap.Add(vehicleGUID, new List<LightTracker>
                            {
                                new LightTracker
                                {
                                    SpawnedLight = newLight,
                                    HeadlightTransform = transform,
                                }
                            });
                            break;
                        }
                    }
                }

                // it doesn't have any inactive transforms so we don't care about it really
                // we want to memoize it so we don't recheck
                // missiles seem to run this method
                // TODO test if missiles are impacted and stop it?
                // BUG this isn't working yet
                // TODO fix type of blip shown
                if (!vehicleMap.ContainsKey(vehicleGUID))
                {
                    vehicleMap.Add(vehicleGUID, new List<LightTracker>());
                }

                // grab the inactive transform, activate it but disable the mesh
                var lightTrackers = vehicleMap[__instance.parentVehicle.GUID];
                // ignored tracker, has no inactive transforms
                if (lightTrackers.Count == 0)
                {
                    return;
                }

                var lightTracker = lightTrackers.First();
                if (lightTracker == null)
                {
                    Log("BOMB");
                    return;
                }

                transform = lightTracker.HeadlightTransform;
                if (visibilityLevel > lightTracker.HighestVisibilityLevel)
                {
                    lightTracker.HighestVisibilityLevel = visibilityLevel;
                }

                if (transform == null)
                {
                    Log("BOMB2");
                    return;
                }

                if (!transform.gameObject.activeSelf)
                {
                    Log("Inactive Transform SetActive");
                    transform.gameObject.SetActive(true);
                    var mesh = transform.GetComponentInChildren<SkinnedMeshRenderer>();
                    foreach (var child in transform.GetComponentsInChildren<Component>())
                    {
                        //Log(child);
                        if (child is SkinnedMeshRenderer)
                        {
                            mesh.enabled = visibilityLevel == VisibilityLevel.LOSFull;
                            Log("Visible? " + mesh.enabled);
                        }

                        // for some reason the mesh has a BTLight?
                        // mesh (BattleTech.Rendering.BTLight)
                        if (child is BTLight btLight &&
                            child.name != "mesh")
                        {
                            if (visibilityLevel >= VisibilityLevel.Blip0Minimum)
                            {
                                //Log("BTLight");
                                //UnityEngine.Object.Destroy(child);
                                //Log(child.transform.parent);
                                btLight.ConfigureLight(true);
                                //child.transform.gameObject.GetComponentsInChildren<BTLight>().Do(Log);
                                //child.transform.gameObject.GetComponentsInChildren<BTLight>()[0].enabled = true;
                                //child.transform.gameObject.GetComponentsInChildren<BTLight>()[1].enabled = true;
                                
                            }
                        }
                    }
                }

                // make sure it becomes visible again...
                // apparently these shenanigans are required for vehicles
                // their blips aren't appearing and disappearing normally like mechs do
                // so we toggle them by hand...super ghetto but it works well enough?
                if (transform.gameObject.activeSelf)
                {
                    if (visibilityLevel == VisibilityLevel.LOSFull)
                    {
                        transform.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
                        lightTracker.SpawnedLight.enabled = true;
                    }
                    else if (!__instance.BlipDisplayed &&
                             visibilityLevel > VisibilityLevel.None)
                    {
                        switch ((int) lightTracker.HighestVisibilityLevel)
                        {
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            {
                                __instance.BlipObjectIdentified.SetActive(false);
                                __instance.BlipObjectUnknown.SetActive(true);
                                break;
                            }
                            case 5:
                            case 6:
                            case 8:
                            case 9:
                            {
                                __instance.BlipObjectIdentified.SetActive(true);
                                __instance.BlipObjectUnknown.SetActive(false);
                                break;
                            }
                        }
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
