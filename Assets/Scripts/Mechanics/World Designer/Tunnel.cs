using UnityEngine;

namespace Mechanics.World_Designer
{
    public class Tunnel : MonoBehaviour
    {
        public void Build(RoomStructure.RoomTunnel tunnel)
        {
            var grid = Grid.Default;
            
            foreach (var pathGridIndex in tunnel.PathGridIndices)
            {
                var point = grid.ToWorld(pathGridIndex);

                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                primitive.transform.localScale = Vector3.one;
                primitive.transform.position = point;
                primitive.transform.parent = transform;
            }
        }
    }
}