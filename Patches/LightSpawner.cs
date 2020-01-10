using System;
using System.Linq;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using HBS.Extensions;
using UnityEngine;
using static BetterHeadlights.Core;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights.Patches
{
    public static class LightSpawner_SpawnLight_Patch
    {
        // magic numbers.. sorry.  adjusted to visuals
        private const float radius = 500;
        private const float vehicleAngleFactor = 1.2f;

        // recreate a spotlight, with settings
        public static void Prefix(LightSpawner __instance)
        {
            try
            {
                __instance.type = LightSpawner.LightTypes.spot;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        //static VehicleRepresentation[] vehicles = Resources.FindObjectsOfTypeAll<VehicleRepresentation>();

        public static void Postfix(LightSpawner __instance, BTLight ___spawnedLight)
        {
            try
            {
                //vehicles.Do(x => x.GetComponentsInChildren<LightSpawner>(true).Do(Log));
                //Log(vehicles.Any(x => x.GetComponentsInChildren<LightSpawner>(true).Any(y=>y ==__instance)));
               //var vee = __instance.transform.parent.gameObject.FindFirstChildNamed("j_COCKPIT").transform.parent.gameObject
               //    .GetComponentInChildren<VehicleRepresentation>();
               //
               //Log(vee);
                Log(new string('=', 50));
                Log($"Patching {__instance}");
                if (settings.ExtraRange)
                {
                    Log("RADIUS");
                    ___spawnedLight.radius = radius;
                }

                // vehicle lights are named this way...
                if (__instance.name.StartsWith("light"))
                {
                    if (IntensityMap.ContainsKey(settings.VehicleIntensity))
                    {
                        Log("VEHICLE INTENSITY " + IntensityMap[settings.VehicleIntensity]);
                        ___spawnedLight.intensity = IntensityMap[settings.VehicleIntensity];
                    }

                    if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                    {
                        ___spawnedLight.spotlightAngleOuter = settings.Angle * vehicleAngleFactor;
                    }

                    ___spawnedLight.volumetricsMultiplier *= settings.VehicleVolumetricsFactor;
                }
                else
                {
                    if (IntensityMap.ContainsKey(settings.MechIntensity))
                    {
                        Log("MECH INTENSITY " + IntensityMap[settings.MechIntensity]);
                        ___spawnedLight.intensity = IntensityMap[settings.MechIntensity];
                    }

                    if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                    {
                        ___spawnedLight.spotlightAngleOuter = settings.Angle;
                    }

                    ___spawnedLight.volumetricsMultiplier *= settings.MechVolumetricsFactor;
                }

                ___spawnedLight.RefreshLightSettings(true);
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
    }
}
