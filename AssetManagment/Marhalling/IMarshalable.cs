
namespace AssetManagement.Marhalling
{
    /// <summary>
    /// Makes it simple to from class and temp struct
    /// </summary>
    /// <typeparam name="Class">Managed Class</typeparam>
    /// <typeparam name="Struct">TempStruct </typeparam>
    interface IMarshalable<Struct>
    {
        /// <summary>
        /// Filll a struct of tupe "struct" with the class data
        /// </summary>
        /// <param name="destStruct"></param>
        abstract void FillStruct(out Struct destStruct);

        /// <summary>
        /// Fills the class from a struct of type "Struct"
        /// </summary>
        /// <param name="destStruct"></param>
        abstract void FillFromStruct(in Struct srcStruct);
    }
}
