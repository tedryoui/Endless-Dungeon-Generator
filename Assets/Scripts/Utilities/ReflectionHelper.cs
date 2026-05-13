using System.Reflection;

namespace Core.Scripts.Helpers
{
    public static class ReflectionHelper
    {
        public static void SetField(object target, string fieldName, object value, bool includeParents = true)
        {
            var       type  = target.GetType();
            FieldInfo field = null;
    
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, 
                    BindingFlags.NonPublic | 
                    BindingFlags.Public | 
                    BindingFlags.Instance);
                type = includeParents ? type.BaseType : null;
            }
    
            field?.SetValue(target, value);
        }
    }
}