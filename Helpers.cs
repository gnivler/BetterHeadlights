using System;
using System.Reflection;
using BattleTech;
using BattleTech.Rendering;
using BetterHeadlights.Patches;
using Harmony;
using UnityEngine;
using static BetterHeadlights.Core;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights
{
    public static class Helpers
    {
        private static readonly MethodInfo original =
            AccessTools.Method(typeof(LightSpawner), "SpawnLight");

        private static readonly MethodInfo prefix =
            AccessTools.Method(typeof(LightSpawner_SpawnLight_Patch),
                nameof(LightSpawner_SpawnLight_Patch.Prefix));

        private static readonly MethodInfo postfix =
            AccessTools.Method(typeof(LightSpawner_SpawnLight_Patch),
                nameof(LightSpawner_SpawnLight_Patch.Postfix));

        // magic numbers.. sorry.  adjusted to visuals
        private const float radius = 500;
        private const float vehicleAngleFactor = 1.2f;

        internal static void RemakeLight(Transform headlightTransform)
        {
            var lightSpawner = headlightTransform.GetComponentInChildren<LightSpawner>();
            var btLight = headlightTransform.GetComponentInChildren<BTLight>();
            var btFlare = headlightTransform.GetComponentInChildren<BTFlare>();
            Object.Destroy(lightSpawner);
            Object.Destroy(btLight);
            Object.Destroy(btFlare);

            harmony.Patch(original);
            harmony.Patch(original,
                new HarmonyMethod(prefix),
                new HarmonyMethod(postfix));
            Log("Create patched LightSpawner");
            headlightTransform.gameObject.AddComponent<LightSpawner>();
            harmony.Unpatch(original, postfix);
            harmony.Unpatch(original, prefix);
        }

        internal static void ConfigureLight(this BTLight btLight, bool isVehicle = false)
        {

            Log($"{btLight.name} is type {btLight.lightType}");

            // because for whatever goddamn reason Torso lights are point lights
            // must be being switched elsewhere?  nonsense
            if (btLight.lightType == BTLight.LightTypes.Point &&
                !btLight.name.Contains("Torso"))
            {
                Log("ABORT");
                return;
            }
            
            if (settings.ExtraRange)
            {
                btLight.radius = radius;
            }

            if (isVehicle)
            {
                if (IntensityMap.ContainsKey(settings.VehicleIntensity))
                {
                    Log("VEHICLE INTENSITY " + IntensityMap[settings.VehicleIntensity]);
                    btLight.intensity = IntensityMap[settings.VehicleIntensity];
                }

                if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                {
                    Log("VEHICLE ANGLE " + settings.Angle / vehicleAngleFactor);
                    btLight.spotlightAngleOuter = settings.Angle / vehicleAngleFactor;
                }

                Log("VEHICLE VOLUMETRICS " + settings.VehicleVolumetricsFactor);
                btLight.volumetricsMultiplier *= settings.VehicleVolumetricsFactor;
            }
            else
            {
                if (IntensityMap.ContainsKey(settings.MechIntensity))
                {
                    Log("MECH INTENSITY " + IntensityMap[settings.MechIntensity]);
                    btLight.intensity = IntensityMap[settings.MechIntensity];
                }

                if (Math.Abs(settings.Angle - 30) > float.Epsilon)
                {
                    Log("MECH ANGLE " + settings.Angle);
                    btLight.spotlightAngleOuter = settings.Angle;
                }

                Log("MECH VOLUMETRICS " + settings.MechVolumetricsFactor);
                btLight.volumetricsMultiplier *= settings.MechVolumetricsFactor;
            }

            //btLight.RefreshLightSettings(true);
        }
    }
}
