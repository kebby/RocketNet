using System;

namespace RocketNet
{
    /// <summary>
    /// GNU Rocket track object. You can query values with it.
    /// </summary>
    public class Track
    {       
        /// <summary>
        /// Retrieve track value for a certain point in time
        /// </summary>
        /// <param name="row">Current row (floating point because time is continuous and in flux and shit)</param>
        /// <returns>Track value for given row</returns>
        public double GetValue(double row)
        {	       
	        /* If we have no keys at all, return a constant 0 */
	        if (keys == null || keys.Length == 0)
		        return 0.0f;

	        var irow = (int)Math.Floor(row);
	        var idx = IdxFloor(irow);

	        /* at the edges, return the first/last value */
	        if (idx < 0)
		        return keys[0].value;
	        if (idx > keys.Length - 2)
                return keys[keys.Length - 1].value;

	        /* interpolate according to key-type */
            double t = (row - keys[idx].row) / (keys[idx + 1].row - keys[idx].row);
	        switch (keys[idx].type) 
            {
	        case Key.Type.Step:
                t = 0;
                break;
	        case Key.Type.Smooth:
                t = t * t * (3 - 2 * t);
                break;
            case Key.Type.Ramp:
                t = Math.Pow(t, 2.0);
                break;            
	        }
            return keys[idx].value + (keys[idx + 1].value - keys[idx].value) * t;
        }

        int IdxFloor(int row)
        {
            int idx = FindKey(row);
            if (idx < 0)
                idx = -idx - 2;
            return idx;
        }

        int FindKey(int row)
        {
            int lo = 0;
            int hi = keys.Length;

            /* binary search, t->keys is sorted by row */
            while (lo < hi)
            {
                int mi = (lo + hi) / 2;

                if (keys[mi].row < row)
                    lo = mi + 1;
                else if (keys[mi].row > row)
                    hi = mi;
                else
                    return mi; /* exact hit */
            }

            /* return first key after row, negated and biased (to allow -0) */
            return -lo - 1;
        }

        internal void SetKey(Key key)
        {
            int idx = FindKey(key.row);

	        if (idx < 0) 
            {
		        /* no exact hit, we need to allocate a new key */
		        idx = -idx - 1;

                var tmp = new Key[keys.Length+1];
                Array.Copy(keys, 0, tmp, 0, idx);
                Array.Copy(keys, idx, tmp, idx+1, keys.Length-idx);
                keys = tmp;
	        }

	        keys[idx] = key;
        }

        internal void DeleteKey(int row)
        {
	        int idx = FindKey(row);
            if (idx < 0) throw new InvalidOperationException("key not found");

            var tmp = new Key[keys.Length-1];
            Array.Copy(keys, 0, tmp, 0, idx);
            Array.Copy(keys, idx+1, tmp, idx, tmp.Length-idx);
            keys = tmp;
        }
        
        internal struct Key
        {
            public enum Type
            {
                Step,   /* stay constant */
                Linear, /* lerp to the next value */
                Smooth, /* smooth curve to the next value */
                Ramp,   /* quadratic curve to the next value (ease-in) */
            }
            public int row;
            public float value;
            public Type type;
        }

        internal string name;
        internal Key[] keys = new Key[0];
    }
}
