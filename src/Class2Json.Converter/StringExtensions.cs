namespace Class2Json.Converter;

public static class StringExtensions
{
    
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
            return str;

        var chars = str.ToCharArray();
        chars[0] = char.ToLower(chars[0]);
        return new string(chars);
    }
    
}