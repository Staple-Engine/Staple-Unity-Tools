using UnityEngine;

namespace Staple
{
    public class Vector2Holder
    {
        public float x;
        public float y;

        public Vector2Holder()
        {
        }

        public Vector2Holder(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }
    }
}
