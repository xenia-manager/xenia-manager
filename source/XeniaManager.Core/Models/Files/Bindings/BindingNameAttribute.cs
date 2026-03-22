namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Attribute to specify the string representation of a binding key or value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class BindingNameAttribute : Attribute
{
    public string Name { get; }
    public string[] Alternatives { get; }

    public BindingNameAttribute(string name, params string[] alternatives)
    {
        Name = name;
        Alternatives = alternatives;
    }
}