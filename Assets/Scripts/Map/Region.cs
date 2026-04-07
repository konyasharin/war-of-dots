using System.Collections.Generic;
using UnityEngine;

namespace DotWars.Map
{
    public class Region
    {
        public int Id { get; }
        public City City { get; set; }
        public int OwnerIndex { get; private set; }
        public HashSet<Vector2Int> Tiles { get; } = new();
        public List<Port> Ports { get; } = new();

        public Region(int id, int ownerIndex)
        {
            Id = id;
            OwnerIndex = ownerIndex;
        }

        public void SetOwner(int newOwner)
        {
            OwnerIndex = newOwner;
            City?.SetOwnerSilent(newOwner);
            foreach (var port in Ports)
                port.SetOwner(newOwner);
        }
    }
}
