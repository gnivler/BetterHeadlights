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

        internal static void RemakeLight(Transform headlightTransform)
        {
            var lightSpawner = headlightTransform.GetComponentInChildren<LightSpawner>();
            var btLight = headlightTransform.GetComponentInChildren<BTLight>();
            var btFlare = headlightTransform.GetComponentInChildren<BTFlare>();
            Object.Destroy(lightSpawner);
            Object.Destroy(btLight);
            Object.Destroy(btFlare);
            // TODO this bool was just to check for Harmony bug
            isRemaking = true;
            harmony.Patch(original);
            harmony.Patch(original,
                new HarmonyMethod(prefix),
                new HarmonyMethod(postfix));
            Log("Create LightSpawner");
            headlightTransform.gameObject.AddComponent<LightSpawner>();
            harmony.Unpatch(original, postfix);
            harmony.Unpatch(original, prefix);
            isRemaking = false;
        }
    }
}
