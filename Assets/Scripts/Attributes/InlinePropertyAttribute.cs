using System;
using UnityEngine;


namespace Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InlinePropertyAttribute : PropertyAttribute
    {
        
    }
}