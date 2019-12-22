using System.Reflection;
using BattleTech.Rendering;
using BetterHeadlights.Patches;
using Harmony;
using UnityEngine;
using static BetterHeadlights.Core;

// ReSharper disable InconsistentNaming

namespace BetterHeadlights
{
    public class Helpers
    {
        internal static MethodInfo original =
            AccessTools.Method(typeof(LightSpawner), "SpawnLight");

        internal static MethodInfo prefix =
            AccessTools.Method(typeof(LightSpawner_SpawnLight_Patch),
                nameof(LightSpawner_SpawnLight_Patch.Prefix));

        internal static MethodInfo postfix =
            AccessTools.Method(typeof(LightSpawner_SpawnLight_Patch),
                nameof(LightSpawner_SpawnLight_Patch.Postfix));

        internal static bool isRemaking;

        internal static void RemakeLight(Transform parentTransform)
        {
            var lightSpawner = parentTransform.GetComponentInChildren<LightSpawner>();
            var btLight = parentTransform.GetComponentInChildren<BTLight>();
            var btFlare = parentTransform.GetComponentInChildren<BTFlare>();
            Object.Destroy(lightSpawner);
            Object.Destroy(btLight);
            Object.Destroy(btFlare);
            isRemaking = true;
            harmony.Patch(original,
                new HarmonyMethod(prefix),
                new HarmonyMethod(postfix));
            parentTransform.gameObject.AddComponent<LightSpawner>();
            isRemaking = false;
            harmony.Unpatch(original, HarmonyPatchType.All);
        }
    }
}
