using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace System.Collections.Extension
{
    [Serializable]
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Value;

        [NonSerialized] 
        private TreeNode<T> _parent;
        public TreeNode<T> Parent { get => _parent; private set => _parent = value; }

        [SerializeReference] 
        private List<TreeNode<T>> _children = new List<TreeNode<T>>();

        public TreeNode(T value)
        {
            Value = value;
        }

        public TreeNode<T> AddChild(T value)
        {
            var node = new TreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            return _children.Remove(node);
        }

        public IReadOnlyList<TreeNode<T>> Children => _children.AsReadOnly();

        public bool IsRoot => Parent == null;
        public bool IsLeaf => _children.Count == 0;
        public int  Level  => IsRoot ? 0 : Parent.Level + 1;

        public IEnumerator<TreeNode<T>> GetEnumerator() => new[] {this}.Concat(_children.SelectMany(x => x.Flatten())).GetEnumerator();
        IEnumerator IEnumerable.        GetEnumerator() => GetEnumerator();

        public IEnumerable<TreeNode<T>> Flatten()
        {
            return new[] { this }.Concat(_children.SelectMany(x => x.Flatten()));
        }
    }
}