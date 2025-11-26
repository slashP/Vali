namespace SchemaGenerator;

public static class Extensions
{
    public static string Prop(this string propertyName) =>
        char.ToLower(propertyName[0]) + new string(propertyName.Skip(1).ToArray());
}