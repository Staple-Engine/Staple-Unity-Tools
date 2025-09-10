using UnityEngine;

namespace Staple
{
    public class Vector3Holder
    {
        public float x;
        public float y;
        public float z;

        public Vector3Holder()
        {
        }

        public Vector3Holder(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
    }
}
