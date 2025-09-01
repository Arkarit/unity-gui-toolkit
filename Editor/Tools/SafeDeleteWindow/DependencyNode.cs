// Assets/Editor/SafeDelete/DependencyNode.cs
using System;
using UnityEditor;
using UnityEngine;

namespace SafeDelete
{
    /// <summary>
    /// Data holder that uniquely identifies an object (asset or stage object).
    /// Assets/subassets are keyed by (GUID, LocalId). Stage objects by GlobalObjectId.
    /// </summary>
    [Serializable]
    public class DependencyNode : IEquatable<DependencyNode>
    {
        public string Name;        // Display name
        public string Path;        // Asset path, empty for stage objects
        public string Guid;        // Asset GUID, empty for stage objects
        public long   LocalId;     // Local file id (0 == main asset)
        public GlobalObjectId Goid;// Global id (works for assets and stage objects)
        public string TypeName;    // e.g. Texture2D, GameObject
        public bool   IsSceneObject;
        public bool   IsScript;
        public bool   IsInResources;

        public bool Equals(DependencyNode other)
        {
            if (other == null) return false;
            if (!string.IsNullOrEmpty(Guid) || !string.IsNullOrEmpty(other.Guid))
                return Guid == other.Guid && LocalId == other.LocalId;
            return Goid.Equals(other.Goid);
        }

        public override bool Equals(object obj) => Equals(obj as DependencyNode);

        public override int GetHashCode()
        {
            unchecked
            {
                if (!string.IsNullOrEmpty(Guid))
                    return (Guid.GetHashCode() * 397) ^ LocalId.GetHashCode();
                return Goid.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Path)) return Path;
            if (!string.IsNullOrEmpty(Name)) return Name;
            return "<DependencyNode>";
        }
    }
}
