using BattleTech.Rendering;
using UnityEngine;
           
namespace BetterHeadlights
{
    internal class LightTracker
    {
       internal BTLight SpawnedLight;
       internal Transform ParentTransform;

       public override string ToString()
       {
           return $"{SpawnedLight}, {ParentTransform.name}";
       }
    }
}
