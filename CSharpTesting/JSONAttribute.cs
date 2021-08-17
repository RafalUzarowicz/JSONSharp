using System;

namespace CSharpTesting
{
    #region Custom attributes

    /// <summary>
    /// Shows that field can be serialized to JSON.
    /// </summary>
    public class JSONField : JSONAttribute
    {
    
    }

    /// <summary>
    /// Shows that class has fields with JSONField attribute and can be serialized to JSON.
    /// </summary>
    public class JSONable : JSONAttribute
    {
    
    }

    #endregion

    public class JSONAttribute : Attribute
    {
        
    }
}