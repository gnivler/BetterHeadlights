using BattleTech.Rendering;
using UnityEngine;
using static BetterHeadlights.Core;

namespace BetterHeadlights
{
    public class Helpers
    {
        internal static void SetRange(BTLight spawnedLight, bool isBlip)
        {
            if (settings.BlipLightsExtraRange && isBlip ||
                settings.ExtraRange && !isBlip)
            {
                Log("ExtraRange");
                spawnedLight.radius = 500f;
                spawnedLight.enabled = false;
            }
        }

        internal static void SetProfile(BTLight spawnedLight, bool isBlip)
        {
            if (isBlip)
            {
                if (settings.Intensity != "VANILLA" &&
                    IntensityMap.ContainsKey(settings.BlipIntensity))
                {
                    Log("BlipIntensity " + IntensityMap[settings.BlipIntensity]);
                    spawnedLight.intensity = IntensityMap[settings.BlipIntensity] / 5f;
                    spawnedLight.spotlightAngleOuter = settings.Angle * 1.2f;
                }
            }
            else
            {
                if (settings.Intensity != "VANILLA" &&
                    IntensityMap.ContainsKey(settings.Intensity))
                {
                    Log("Intensity " + IntensityMap[settings.Intensity]);
                    spawnedLight.intensity = IntensityMap[settings.Intensity];
                    spawnedLight.spotlightAngleOuter = settings.Angle;
                }
            }
        }
    }
}
