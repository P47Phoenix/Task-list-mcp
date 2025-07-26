namespace TaskListMcp.Models;

/// <summary>
/// Represents the possible types of custom attributes
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// Text field
    /// </summary>
    Text = 0,
    
    /// <summary>
    /// Integer number
    /// </summary>
    Integer = 1,
    
    /// <summary>
    /// Decimal number
    /// </summary>
    Decimal = 2,
    
    /// <summary>
    /// Date value
    /// </summary>
    Date = 3,
    
    /// <summary>
    /// Date and time value
    /// </summary>
    DateTime = 4,
    
    /// <summary>
    /// Boolean flag
    /// </summary>
    Boolean = 5,
    
    /// <summary>
    /// Single choice from predefined options
    /// </summary>
    SingleChoice = 6,
    
    /// <summary>
    /// Multiple choices from predefined options
    /// </summary>
    MultipleChoice = 7,
    
    /// <summary>
    /// URL value
    /// </summary>
    Url = 8,
    
    /// <summary>
    /// File reference
    /// </summary>
    FileReference = 9
}
