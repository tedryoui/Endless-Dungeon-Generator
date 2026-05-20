using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Scripts.Helpers
{
    public class Label : MonoBehaviour
    {
        [SerializeField] private List<string> _labels;

        public List<string> Labels => _labels;
        
        public bool HasLabel(string value) => _labels.Contains(value);
    }
}