using BattleTech;
using BattleTech.Rendering;
using static BetterHeadlights.Core;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights.Patches
{
    public static class LightSpawner_SpawnLight_Patch
    {
        // magic numbers.. sorry.  adjusted to visuals
        private const float radius = 500;
        private const float volumetricFactor = 0.33f;
        private const float vehicleIntensityFactor = 5f;
        private const float vehicleAngleFactor = 1.2f;

        public static void Prefix(LightSpawner __instance)
        {
            // TODO delete these
            if (!Helpers.isRemaking)
            {
                Log("Unpatched execution");
                return;
            }

            __instance.type = LightSpawner.LightTypes.spot;
        }

        // recreate a spotlight, with settings
        public static void Postfix(LightSpawner __instance, BTLight ___spawnedLight)
        {
            // TODO delete these
            if (!Helpers.isRemaking)
            {
                Log("Unpatched execution");
                return;
            }

            if (__instance.type == LightSpawner.LightTypes.point)
            {
                Log("Skipping point light");
                return;
            }

            var mech = __instance.transform.parent.gameObject
                .GetComponentInParent<MechRepresentation>().parentMech;
            Log($"adjusting: {__instance.name} ({__instance.transform.parent.name}) - {mech?.team.DisplayName}");

            if (settings.ExtraRange)
            {
                Log("RADIUS");
                ___spawnedLight.radius = radius;
            }

            if (IntensityMap.ContainsKey(settings.Intensity))
            {
                Log("INTENSITY");
                // TODO scale it with the brightness setting?
                ___spawnedLight.volumetricsMultiplier *= volumetricFactor;
                // vehicle lights are named this way...
                if (__instance.name.StartsWith("light"))
                {
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity] / vehicleIntensityFactor;
                    ___spawnedLight.spotlightAngleOuter = settings.Angle * vehicleAngleFactor;
                }
                else
                {
                    ___spawnedLight.intensity = IntensityMap[settings.Intensity];
                    ___spawnedLight.spotlightAngleOuter = settings.Angle;
                }
            }
        }
    }
}
