using UnityEngine;

namespace Service.Concrete
{
    public class UserInterfaceService : MonoService
    {
        [SerializeField] private GameplayUserInterfaceViewModel _guiViewModel;

        public GameplayUserInterfaceViewModel GUIViewModel => _guiViewModel;

        public override void Dispose()
        {
            Debug.Log("<color=white>User interface service disposed</color>");
        }

        public override void Initialize()
        {
            Debug.Log("<color=green>User interface service initialized</color>");
            
            _guiViewModel.Initialize();
        }
    }
}