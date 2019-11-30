using BattleTech.Rendering;
using static BetterHeadlights.Core;

namespace BetterHeadlights

{
    public class Helpers
    {
        internal static void SetRange(BTLight spawnedLight, bool blip)
        {
            if (settings.BlipLightsExtraRange && blip ||
                settings.ExtraRange && !blip)
            {
                Log("ExtraRange");
                spawnedLight.radius = 500f;
            }
        }

        internal static void SetProfile(BTLight spawnedLight, bool blip)
        {
            if (blip)
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
