using System;

namespace Service
{
    public interface IService : IDisposable
    {
        public void Initialize();
    }
}