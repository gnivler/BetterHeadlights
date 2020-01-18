using System;
using BattleTech.Rendering;
using UnityEngine;
using static BetterHeadlights.Core;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights
{
    public static class Helpers
    {
        // magic numbers.. sorry.  adjusted to visuals
        private const float radius = 500;
        private const float vehicleAngleFactor = 1.1f;

        internal static void ConfigureLight(this BTLight btLight, bool isVehicle = false)
        {
            //Log($"{btLight.name} is type {btLight.lightType}");

 
            if (settings.ExtraRange)
            {
                btLight.radius = radius;
            }

            if (isVehicle)
            {
                if (IntensityMap.ContainsKey(settings.VehicleIntensity))
                {
                    //Log("VEHICLE INTENSITY " + IntensityMap[settings.VehicleIntensity]);
                    btLight.intensity = IntensityMap[settings.VehicleIntensity];
                }

                if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                {
                    //Log("VEHICLE ANGLE " + settings.Angle / vehicleAngleFactor);
                    btLight.spotlightAngleOuter = settings.Angle / vehicleAngleFactor;
                }

                //Log("VEHICLE VOLUMETRICS " + settings.VehicleVolumetricsFactor);
                btLight.volumetricsMultiplier *= settings.VehicleVolumetricsFactor;
            }
            else
            {
                if (IntensityMap.ContainsKey(settings.MechIntensity))
                {
                    //Log("MECH INTENSITY " + IntensityMap[settings.MechIntensity]);
                    btLight.intensity = IntensityMap[settings.MechIntensity];
                }

                if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                {
                    //Log("MECH ANGLE " + settings.Angle);
                    btLight.spotlightAngleOuter = settings.Angle;
                }

                //Log("MECH VOLUMETRICS " + settings.MechVolumetricsFactor);
                btLight.volumetricsMultiplier *= settings.MechVolumetricsFactor;
            }

            // HBS default = 0
            btLight.spotlightAngleInner = 0;

            var parent = btLight.transform.parent;
            var lightSpawner = parent.GetComponentInChildren<LightSpawner>();
            btLight.castShadows = lightSpawner.castShadows;
            btLight.contributeVolumetrics = true;
            btLight.shadowBias = 0.0005f;
            btLight.shadowNearPlane = 0.005f;
            if (lightSpawner.spawnedFlare != null && !lightSpawner.spawnedFlare.enabled)
            {
                lightSpawner.spawnedFlare.enabled = true;
            }
            else if (lightSpawner.spawnedFlare == null)
            {
                lightSpawner.spawnedFlare = lightSpawner.gameObject.AddComponent<BTFlare>();
            }

            lightSpawner.spawnedFlare.useCone = true;
            lightSpawner.spawnedFlare.innerAngle = 0;
            lightSpawner.spawnedFlare.outerAngle = 30;
            lightSpawner.spawnedFlare.intensity = lightSpawner.spawnedLight.intensity * 1.25f / 500000f;
            lightSpawner.spawnedFlare.color = lightSpawner.spawnedLight.lightColor;
            btLight.RefreshLightSettings(true);
        }
    }
}
