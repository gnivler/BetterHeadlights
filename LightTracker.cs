using BattleTech.Rendering;
using UnityEngine;


namespace BetterHeadlights
{
    internal class LightTracker
    {
       internal BTLight SpawnedLight;
       internal bool IsAdjusted;
       internal Transform Transform;

       public override string ToString()
       {
           return $"{SpawnedLight}, {IsAdjusted}, {Transform.name}";
       }
    }
}
