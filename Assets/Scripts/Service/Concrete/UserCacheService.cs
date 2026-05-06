using UnityEngine;

namespace Service.Concrete
{
    public class UserCacheService : IService
    {
        public void Dispose()
        {
            Debug.Log("<color=white>User cache service disposed</color>");
        }

        public void Initialize()
        {
            Debug.Log("<color=green>User cache service initialized</color>");
        }
    }
}