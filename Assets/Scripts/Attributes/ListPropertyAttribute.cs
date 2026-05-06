using System;
using UnityEngine;

namespace Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ListPropertyAttribute : PropertyAttribute
    {
        private bool _isFolded;

        public bool IsFolded => _isFolded;

        public ListPropertyAttribute(bool isFolded)
        {
            _isFolded = isFolded;
        }
    }
}