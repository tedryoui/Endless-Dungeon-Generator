using System;
using UnityEngine;

namespace Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisabledPropertyAttribute : PropertyAttribute
    {
        public DisabledPropertyAttribute(bool applyToCollection = false) : base(applyToCollection)
        {
            
        }
    }
}