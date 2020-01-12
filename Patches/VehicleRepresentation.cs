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
        //private static readonly Stopwatch timer = new Stopwatch();
        public static void Postfix(VehicleRepresentation __instance)
        {
            try
            {
                if (!settings.BlipLights)
                {
                    return;
                }

                if (__instance.IsDead)
                {
                    return;
                }

                var visibilityLevel = UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam.VisibilityToTarget(__instance.parentActor);
                if (visibilityLevel != VisibilityLevel.None)
                {
                    List<Transform> transforms;
                    if (!vehicleMap.ContainsKey(__instance.parentVehicle.GUID))
                    {
                        Log($"Adding vehicle {__instance.parentVehicle.Nickname} with {__instance.VisibleLights.Length} lights");

                        var myList = new List<Transform>();
                        foreach (var btLight in __instance.VisibleLights)
                        {
                            transforms = btLight.GetComponentsInParent<Transform>(true).ToList();
                            myList.AddRange(transforms);
                        }

                        vehicleMap.Add(__instance.parentVehicle.GUID, myList);
                        if (!vehicleMap.ContainsKey(__instance.parentVehicle.GUID))
                        {
                            Log("We don't care about " + __instance.parentVehicle.DisplayName);
                            vehicleMap.Add(__instance.parentVehicle.GUID, null);
                        }
                    }

                    transforms = vehicleMap[__instance.parentVehicle.GUID];
                    foreach (var t in transforms)
                    {
                        if (t != null)
                        {
                            t.gameObject.SetActive(true);
                            t.GetComponentInChildren<BTLight>()?.ConfigureLight(true);
                            foreach (var component in t.GetComponentsInChildren<Component>())
                            {
                                if (component is SkinnedMeshRenderer skinnedMeshRenderer)
                                {
                                    skinnedMeshRenderer.enabled = visibilityLevel == VisibilityLevel.LOSFull;
                                }

                                if (component is BTLight btLight)
                                {
                                    btLight.enabled = visibilityLevel >= VisibilityLevel.Blip0Minimum;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception input)
            {
                Log(input);
            }
        }
    }
}
