using System;
using System.Collections.Generic;
using System.Linq;
using Service.Concrete;
using UnityEngine;
using Random = System.Random;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "World Design Set", menuName = "World Design/Set", order = 0)]
    public class WorldDesignSet : ScriptableObject
    {
        public List<RoomDescription> RoomDescriptions;

        public RoomDescription Take(RoomType type, string identity)
        {
            var result = RoomDescriptions
                .FirstOrDefault(x => x.Identity.Equals(identity));
            
            if (result == null)
                throw new Exception($"No such [{identity}] of type [{type}] was found!");
            
            return result;
        }

        public RoomDescription TakeRandom(RoomType type)
        {
            var rnd    = new Random();
            var result = RoomDescriptions
                .Where(x => x.Type.Equals(type))
                .OrderBy(x => rnd.Next())
                .FirstOrDefault();
            
            if (result == null)
                throw new Exception($"No such rooms of type [{type}] was found!");
            
            return result;
        }
    }
}