using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3N = System.Numerics.Vector3;
using QuaternionN = System.Numerics.Quaternion;

namespace anakinsoft.utilities
{
    public static class XnaToBepuConverters
    {

        public static Vector3 ToVector3(this Vector3N vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector3N ToVector3N(this Vector3 vector)
        {
            return new Vector3N(vector.X, vector.Y, vector.Z);
        }

        public static Quaternion ToQuaternion(this QuaternionN q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static QuaternionN ToQuaternionN(this Quaternion q)
        {
            return new QuaternionN(q.X, q.Y, q.Z, q.W);
        }
    }
}
