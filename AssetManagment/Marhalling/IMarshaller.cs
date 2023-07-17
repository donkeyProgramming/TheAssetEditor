using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using AssetManagement.GenericFormats.Unmanaged;

namespace AssetManagement.Marhalling
{
    public interface IMarshaller<Manged, Native>
    {
        public abstract void CopyToNative(IntPtr thisPtr, out Native nativeValue);
        
        
        
        /// <summary>
        /// Copies the data from the native pointer into the class
        /// </summary>
        /// <param name="ptrNativeDes">The source pointer</param>
        /// <returns>reference to "this"</returns>
        public abstract void CopyFromNative(IntPtr ptrNativeDes);

    }

    public class Vertex : IMarshaller<Vertex, PackedCommonVertex>
    {
        public void CopyToNative(IntPtr thisPtr, out PackedCommonVertex nativeValue)
        {
            var ptr = Marshal.PtrToStructure(IntPt0 ptrNativeDes, typeof(PackedCommonVertex));

            Marshal.StructureToPtr(ptrNativeDes, typeof(PackedCommonVertex))
        }

        public void CopyFromNative(IntPtr ptrNativeDes)
        {
            var ptr = Marshal.PtrToStructure(ptrNativeDes, typeof(PackedCommonVertex));                       
        }        
    }
    
}
