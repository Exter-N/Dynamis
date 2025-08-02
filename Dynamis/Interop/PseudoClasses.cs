namespace Dynamis.Interop;

public static class PseudoClasses
{
    public static ClassInfo Generate(string name, uint size, Template template, ClassKind kind)
    {
        var classInfo = new ClassInfo()
        {
            Name = name,
            Kind = kind,
            EstimatedSize = size,
            SizeFromContext = size,
        };

        switch (template) {
            case Template.None:
                // This case intentionally left blank.
                break;
            case Template.SingleArray:
                classInfo.SetFields(
                    [
                        new FieldInfo
                        {
                            Name = "Data",
                            Offset = 0,
                            Size = size,
                            Type = FieldType.Single,
                        },
                    ]
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(template), template, $"Unrecognized class template {template}"
                );
        }

        return classInfo;
    }

    public static ClassInfo GenerateArray(ClassInfo elementClass, int length)
    {
        var classInfo = new ClassInfo()
        {
            Name = $"{elementClass.Name}[{length}]",
            Kind = ClassKind.Regular,
            EstimatedSize = elementClass.EstimatedSize * unchecked((uint)length),
            SizeFromContext = elementClass.EstimatedSize * unchecked((uint)length),
        };

        classInfo.SetFields(
            [
                new FieldInfo
                {
                    Name = "Elements",
                    Offset = 0,
                    Size = classInfo.EstimatedSize,
                    Type = FieldType.ObjectArray,
                    ElementClass = elementClass,
                },
            ]
        );

        return classInfo;
    }

    public enum Template : uint
    {
        None        = 0,
        SingleArray = 1,
    }
}
