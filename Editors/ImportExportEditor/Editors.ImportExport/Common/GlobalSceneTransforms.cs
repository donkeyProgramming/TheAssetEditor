using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Editors.ImportExport.Common
{
    /// <summary>
    /// For transforming mesh data into different coordinate systems
    /// 
    /// </summary>
    public class GlobalSceneTransforms
    {    
        static public Quaternion FlipQuaternion(Quaternion q, bool doMirror)
        {            
            if (doMirror)
            {
                return new Quaternion(q.X, -q.Y, -q.Z, q.W);
            }
            else
            {
                return q;
            }
        }

        static public Vector3 FlipVector(Vector3 v, bool doMirror)
        {
            if (doMirror)
            {
                return new Vector3(-v.X, v.Y, v.Z);
            }
            else
            {
                return v;
            }
        }
        static public Vector4 FlipVector(Vector4 v, bool doMirror)
        {
            if (doMirror)
            {
                return new Vector4(-v.X, v.Y, v.Z, v.W);
            }
            else
            {
                return v;
            }
        }
    }
}
