using CommonControls.ModelFiles.Mesh.Native;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CommonControls.ModelFiles.FBX.
{
    /// <summary>
    /// Very WIP class, dont review, please!!!!
    /// </summary>
    /// <typeparam name="MANAGED_TYPE"></typeparam>
    abstract class ISimperMarshaller<MANAGED_TYPE>
    {
        public virtual MANAGED_TYPE FromNative(IntPtr nativePtr)
        {
            throw new NotImplementedException();
        }
    }


    class StringMashaller : ISimperMarshaller<string>
    {
        override public string FromNative(IntPtr nativePtr)
        {
            var managedDest = Marshal.PtrToStringUTF8(nativePtr);
            return managedDest;
        }
    }

    class MarshalHelper
    {
        private Dictionary<Type, object> _dictionryOfTypeToMashaller = new Dictionary<Type, object>();
        public MarshalHelper()
        {
            _dictionryOfTypeToMashaller[typeof(string)] = new StringMashaller();
        }

        static T Get<T>(IntPtr ptr) where T : class
        {
            var marhalHelper = new MarshalHelper();
            var marhsaller = marhalHelper.GetMarshaller<T>();
            return marhsaller.FromNative(ptr);
        }

        private ISimperMarshaller<T> GetMarshaller<T>() where T : class
        {
            return _dictionryOfTypeToMashaller[typeof(T)] as ISimperMarshaller<T>;
        }
    };




    public abstract class IMarshalller<Native, Managed>
    {
        private bool _bIsBlitable = false;

        // expertimental helper: instead of constructor calling constructor
        private void ConstructorHelper(bool isBlitable)
        {
            _bIsBlitable = isBlitable;
        }
        public IMarshalller() => ConstructorHelper(false);

        public IMarshalller(bool isBlitable) => ConstructorHelper(isBlitable);
                

        public virtual Managed FromNative(IntPtr nativePtr, Managed des)
        {
            throw new NotImplementedException();
        }

        public virtual IntPtr ToNative(Managed src)
        {
            throw new NotImplementedException();
        }        
    }

    // --------------------------- WIP ---------------------------------
    public class Vector3Marhaller : IMarshalller<XMFLOAT3, Vector3>
    {
        public Vector3 FromNative(IntPtr nativePtr)
        {
            Vector3? vector3 = null;
            Marshal.PtrToStructure<Vector3>(nativePtr, vector3.Value);

            if (vector3 == null)
            {
                throw new Exception($"Could not convert {typeof(XMFLOAT3)} to IntrPtr  {typeof(Vector3)} Result is NULL!");
            };

            return vector3.Value;
        }

        public IntPtr ToNative(Vector3 srcValue)
        {
            IntPtr ptrVector3 = IntPtr.Zero;
            Marshal.StructureToPtr<Vector3>(srcValue, ptrVector3, false);

            if (ptrVector3 == IntPtr.Zero)       
            {                
                throw new Exception($"Could not convert {typeof(Vector3)} to IntrPtr {typeof(XMFLOAT3)} Result is NULL!");
            };

            return ptrVector3;
        }
    }

    public class Marshalller
    {
        public Dictionary<(Type, Type), object> _dic;

        Marshalller()
        {
            _dic[(typeof(XMFLOAT3), typeof(Vector3))] = (object) new Vector3Marhaller();
        }


        private
        IMarshalller<T, N> GetMarshaller<T, N>() 
        where T : class
        where N : class
        {
            return _dic[(typeof(T), typeof(N))] as IMarshalller<T, N>;
        }
    }

}
