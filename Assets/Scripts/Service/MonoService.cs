using UnityEngine;

namespace Service
{
    public abstract class MonoService : MonoBehaviour, IService
    {
        public abstract void Dispose();
        public abstract void Initialize();
    }
}