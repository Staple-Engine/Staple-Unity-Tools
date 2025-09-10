using UnityEngine;

namespace Staple
{
    public class Vector4Holder
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4Holder()
        {
        }

        public Vector4Holder(Vector4 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = v.w;
        }

        public Vector4Holder(Quaternion v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = v.w;
        }

        public Vector4Holder(Rect v)
        {
            x = v.x;
            y = v.y;
            z = v.x + v.width;
            w = v.y + v.height;
        }
    }
}
