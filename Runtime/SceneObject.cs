using System.Collections.Generic;

namespace Staple
{
    public class SceneObject
    {
        public SceneObjectKind kind;

        public string name;

        public int ID;

        public int parent;

        public SceneObjectTransform transform = new();

        public List<SceneComponent> components = new();

        public string layer;

        public bool enabled;

        public string prefabGuid;

        public int prefabLocalID;

        public EntityHierarchyVisibility hierarchyVisibility;
    }
}
