using System;
using Service.Concrete;

namespace Core.Scripts.Helpers
{
    public enum AssemblyTypeNameEnum
    {
        UserInterfaceService,
        WorldDesignService,
        UserCacheService
    }

    public static class AssemblyTypeNameFabric
    {
        public static string Resolve(AssemblyTypeNameEnum assemblyTypeName)
        {
            var assemblyTypeNameString = "";
            
            switch (assemblyTypeName)
            {
                case AssemblyTypeNameEnum.UserInterfaceService:
                    assemblyTypeNameString = typeof(UserInterfaceService).AssemblyQualifiedName;
                    break;
                case AssemblyTypeNameEnum.WorldDesignService:
                    assemblyTypeNameString = typeof(WorldDesignService).AssemblyQualifiedName;
                    break;
                case AssemblyTypeNameEnum.UserCacheService:
                    assemblyTypeNameString = typeof(UserCacheService).AssemblyQualifiedName;
                    break;
                default:
                    assemblyTypeNameString = string.Empty;
                    throw new ArgumentOutOfRangeException(nameof(assemblyTypeName), assemblyTypeName, null);
            }
            
            return assemblyTypeNameString;
        }
    }
}