using System;
using System.Collections.Generic;
using System.IO;

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
        /// <param name="sinceRows">Difference of current row and the row of the preceding keyframe</param>
        /// <returns>Track value for given row</returns>
        public float GetValue(float row, out float sinceRows)
        {	       
	        // If we have no keys at all, return a constant 0 
            if (keys.Count == 0)
            {
                sinceRows = row;
                return 0.0f;
            }

            // find key at/before the current row
	        var irow = (int)Math.Floor(row);
	        var idx = FindKey(irow);
            if (idx < 0)
                idx = -idx - 2;

	        // at the edges, return the first/last value 
            if (idx < 0)
            {
                sinceRows = row - keys[0].row;
                return keys[0].value;
            }
            if (idx > keys.Count - 2)
            {
                sinceRows = row - keys[keys.Count - 1].row;
                return keys[keys.Count - 1].value;
            }

	        // interpolate according to key-type 
            float t = (row - keys[idx].row) / (keys[idx + 1].row - keys[idx].row);
	        switch (keys[idx].type) 
            {
	        case Key.Type.Step:
                t = 0;
                break;
	        case Key.Type.Smooth:
                t = t * t * (3 - 2 * t);
                break;
            case Key.Type.Ramp:
                t = (float)Math.Pow(t, 2.0);
                break;            
	        }
            sinceRows = row - keys[idx].row;
            return keys[idx].value + (keys[idx + 1].value - keys[idx].value) * t;
        }

        /// <summary>
        /// Retrieve track value for a certain point in time
        /// </summary>
        /// <param name="row">Current row (floating point because time is continuous and in flux and shit)</param>
        /// <returns>Track value for given row</returns>
        public float GetValue(float row)
        {
            float dummy;
            return GetValue(row, out dummy);
        }

        // find key at or after the given row
        int FindKey(int row)
        {
            int lo = 0;
            int hi = keys.Count;

            // binary search, t->keys is sorted by row 
            while (lo < hi)
            {
                int mi = (lo + hi) / 2;

                if (keys[mi].row < row)
                    lo = mi + 1;
                else if (keys[mi].row > row)
                    hi = mi;
                else
                    return mi; // exact hit 
            }

            // return first key after row, negated and biased (to allow -0) 
            return -lo - 1;
        }

        // create/set a key
        internal void SetKey(Key key)
        {
            int idx = FindKey(key.row);

	        if (idx < 0) 
            {
		        // no exact hit, we need to allocate a new key 
		        idx = -idx - 1;
                keys.Insert(idx, key);
	        }
            else
    	        keys[idx] = key;
        }

        // delete a key
        internal void DeleteKey(int row)
        {
	        int idx = FindKey(row);
            if (idx < 0) throw new InvalidOperationException("key not found");

            keys.RemoveAt(idx);
        }
        
        // load track from a stream
        internal void Load(Stream s)
        {
            using (var br = new BinaryReader(s))
            {
                int nKeys = br.ReadInt32();
                keys.Clear();
                keys.Capacity = nKeys;
                for (int i = 0; i < nKeys; i++)
                {
                    int row = br.ReadInt32();
                    float value = br.ReadSingle();
                    int type = br.ReadByte();

                    keys.Add(new Key
                    {
                        row = row,
                        value = value,
                        type = (Key.Type)type,
                    });
                }
            }
        }

        // save track to a stream
        internal void Save(Stream s)
        {
            using (var bw = new BinaryWriter(s))
            {
                bw.Write((Int32)keys.Count);
                foreach (var key in keys)
                {
                    bw.Write((Int32)key.row);
                    bw.Write((Single)key.value);
                    bw.Write((byte)key.type);
                }
            }
        }

        internal struct Key
        {
            public enum Type
            {
                Step   = 0, // stay constant 
                Linear = 1, // lerp to the next value 
                Smooth = 2, // smooth curve to the next value 
                Ramp   = 3, // quadratic curve to the next value (ease-in) 
            }
            public int row;
            public float value;
            public Type type;
        }

        internal string name;
        private List<Key> keys = new List<Key>();
    }
}
