using Service.Concrete;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "Room Description", menuName = "World Design/Room Description", order = 0)]
    public class RoomDescription : ScriptableObject
    {
        [SerializeField] private string                   _identity;
        [SerializeField] private RoomType                 _type;
        [SerializeField] private AssetReferenceGameObject _reference;

        public string Identity => _identity;
        public RoomType Type => _type;
        public AssetReferenceGameObject Reference => _reference;
    }
}