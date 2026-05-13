using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer
{
    public class Exit : MonoBehaviour
    {
        [SerializeField] private string    _tunnelIdentity = "CST_DEF_001";
        [SerializeField] private string    _snapshotIdentity = "EXIT_001_OPENED";
        [SerializeField] private Direction _direction;
        [SerializeField] private int3      _start;

        public string    TunnelIdentity   => _tunnelIdentity;
        public string    SnapshotIdentity => _snapshotIdentity;
        public Direction Direction        => _direction;
        public int3      Start            => _start;
    }
}