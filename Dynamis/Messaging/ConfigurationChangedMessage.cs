namespace Dynamis.Messaging;

public record ConfigurationChangedMessage(string? ChangedPropertyHint)
{
    public bool IsPropertyChanged(string property)
        => ChangedPropertyHint is null || ChangedPropertyHint.Equals(property, StringComparison.Ordinal);
}
