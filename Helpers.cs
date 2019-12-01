using BattleTech.Rendering;
using static BetterHeadlights.Core;

namespace BetterHeadlights
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Helpers
    {
        internal const float ExtraRange = 500f;

        internal static void SetRange(BTLight spawnedLight, bool isBlip)
        {
            if (settings.BlipLightsExtraRange && isBlip ||
                settings.ExtraRange && !isBlip)
            {
                Log("ExtraRange");
                spawnedLight.radius = ExtraRange;
            }
        }

        internal static void SetProfile(BTLight spawnedLight, bool isBlip)
        {
            if (isBlip)
            {
                if (IntensityMap.ContainsKey(settings.BlipIntensity))
                {
                    Log("BlipIntensity " + IntensityMap[settings.BlipIntensity]);
                    spawnedLight.intensity = IntensityMap[settings.BlipIntensity] / 5f;
                    spawnedLight.spotlightAngleOuter = settings.Angle * 1.2f;
                }
            }
            else
            {
                if (IntensityMap.ContainsKey(settings.Intensity))
                {
                    Log("Intensity " + IntensityMap[settings.Intensity]);
                    spawnedLight.intensity = IntensityMap[settings.Intensity];
                    spawnedLight.spotlightAngleOuter = settings.Angle;
                }
            }
        }
    }
}