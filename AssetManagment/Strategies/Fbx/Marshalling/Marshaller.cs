using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AssetManagement.GenericFormats;
using CommonControls.ModelFiles.Marshalling;

namespace CommonControls.ModelFiles.Marshalling
{

    public interface IMarshaller<ManagedStruct, NativeStruct>
    where ManagedStruct : class, new()
     where NativeStruct : struct
    {
        public abstract void CopyToNative(IntPtr thisPtr, out NativeStruct nativeValue);

        public abstract void CopyToManaged(string srcValue, out IntPtr nativePtr);
    }

    //class VertexMarshaller : IMarshaller<PackedMesh, NtPackedMesh>
    //{
    //    override public string FromNative(IntPtr srcNativePtr)
    //    {
    //        return Marshal.PtrToStringUTF8(srcNativePtr);
    //    }

    //    public override void ToNative(string srcValue, out IntPtr nativePtr)
    //    {
    //        nativePtr = Marshal.StringToHGlobalAnsi(srcValue);
    //    }
    //}

    //class MarshalHelper
    //{
    //    private Dictionary<Type, object> _dictionryOfTypeToMashaller = new Dictionary<Type, object>();
    //    public MarshalHelper()
    //    {
    //        _dictionryOfTypeToMashaller[typeof(string)] = new StringMashaller();
    //        _dictionryOfTypeToMashaller[typeof(string)] = new Vector3Mashaller();
    //        _dictionryOfTypeToMashaller[typeof(string)] = new Vector4Mashaller();
    //        _dictionryOfTypeToMashaller[typeof(string)] = new ArrayMashaller();
    //    }

    //    public void Register<T, F>(F marshaller)
    //    where T : class
    //    {
    //        _dictionryOfTypeToMashaller[typeof(T)] = marshaller;
    //    }

    //    public T Get<T>(IntPtr ptr) where T : class
    //    {
    //        var marhsaller = this.GetMarshaller<T>();
    //        return marhsaller.FromNative<T>(ptr);
    //    }

    //    private IMarshaller<T> GetMarshaller<T>() where T : class
    //    {
    //        return _dictionryOfTypeToMashaller[typeof(T)] as IMarshaller<T>;
    //    }
    //};

    class MarshalHelperSimple
    {
        void CopyToNative<ManagedType, NativeType>(IntPtr nativePtr, NativeType dest)
            where ManagedType : class, IMarshaller<ManagedType, NativeType>, new()
            where NativeType : struct
        {
            ManagedType source = Activator.CreateInstance<ManagedType>();            
            source.CopyToNative(nativePtr, out dest);
        }
    }
}
