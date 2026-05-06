using UnityEngine;

namespace Service.Concrete
{
    public class UserInterfaceService : MonoService
    {
        public override void Dispose()
        {
            Debug.Log("<color=white>User interface service disposed</color>");
        }

        public override void Initialize()
        {
            Debug.Log("<color=green>User interface service initialized</color>");
        }
    }
}