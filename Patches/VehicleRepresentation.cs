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
                        UnityEngine.Object.Destroy(light.GetComponentInChildren<LightSpawner>());
                        UnityEngine.Object.Destroy(light.GetComponentInChildren<BTLight>());
                        Helpers.RemakeLight(light.transform);
                        transform = light.GetComponentsInParent<Transform>(true)
                            .FirstOrDefault(x => !x.gameObject.activeSelf);
                        if (transform != null)
                        {
                            // add single inactive transform
                            vehicleMap.Add(__instance.parentVehicle.GUID, new List<LightTracker>
                            {
                                new LightTracker
                                {
                                    HeadlightTransform = transform,
                                }
                            });
                            break;
                        }
                    }

                    // it doesn't have any inactive transforms so we don't care about it really
                    // missiles seem to run this method.  memoize it
                    // TODO test if missiles are impacted and stop it?
                    if (!vehicleMap.ContainsKey(vehicleGUID))
                    {
                        vehicleMap.Add(__instance.parentVehicle.GUID, new List<LightTracker>());
                    }
                }

                // grab the inactive transform, activate it but disable the mesh
                transform = vehicleMap[__instance.parentVehicle.GUID].FirstOrDefault()?.HeadlightTransform;
                if (transform != null &&
                    !transform.gameObject.activeSelf)
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
}
