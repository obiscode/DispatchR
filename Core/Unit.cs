namespace Dispatcher;

/// <summary>
/// The Unit struct is a singleton type that represents a unit of work or a void return type.
/// It is used in scenarios where a method does not return a meaningful value.          
/// /// </summary>
   public struct Unit
{
    /// <summary>   
    /// The singleton instance of the Unit struct.          
    /// </summary>  
    public static readonly Unit Value = new();
}