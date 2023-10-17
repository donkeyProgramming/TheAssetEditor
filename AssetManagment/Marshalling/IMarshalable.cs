
namespace AssetManagement.Marshalling
{
    /// <summary>
    /// Makes it simple to from class and temp struct
    /// </summary>    
    /// <typeparam name="TUnmangedStruct">The unmanges struct to copy/toFrom</typeparam>
    public abstract class IMarshalable<TUnmangedStruct>
    {
        /// <summary>
        /// Filll a struct of tupe "struct" with the class data
        /// </summary>
        /// <param name="destStruct"></param>
        public abstract void FillStruct(out TUnmangedStruct destStruct);

        /// <summary>
        /// Fills the class from a struct of type "Struct"
        /// </summary>
        /// <param name="destStruct"></param>
        public abstract void FillFromStruct(in TUnmangedStruct srcStruct);

        /// <summary>
        /// Determines whether the data can be copies by a simple Marshal.PtrToStruct
        /// </summary>
        public bool IsBlitable { get; } = true;
    }
}
