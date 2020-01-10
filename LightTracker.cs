using BattleTech;
using BattleTech.Rendering;
using UnityEngine;
           
namespace BetterHeadlights
{
    internal class LightTracker
    {
       internal BTLight SpawnedLight;
       internal Transform HeadlightTransform;
       internal VisibilityLevel HighestVisibilityLevel;

       public override string ToString()
       {
           return $"{SpawnedLight}, {HeadlightTransform.name}";
       }
    }
}
