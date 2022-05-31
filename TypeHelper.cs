namespace EZAccess.Data;

public static class TypeHelper
{
    /// <summary>
    /// Extention to any type which returns a boolean value indicating the type is:
    /// int, double, decimal, long, short, byte, ulong, ushort, float, sbyte, uint
    /// </summary>
    /// <param name="myType">Any type</param>
    /// <returns>Returns true is the type is in the list. Returns false if it doesn't</returns>
    public static bool IsNumeric(this Type myType)
    {
        var x = Nullable.GetUnderlyingType(myType);
        if (x == typeof(int) || x == typeof(double) || x == typeof(decimal) ||
            x == typeof(long) || x == typeof(short) || x == typeof(byte) ||
            x == typeof(ulong) || x == typeof(ushort) || x == typeof(float) ||
            x == typeof(sbyte) || x == typeof(uint))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Extention to any type which returns a boolean value indicating the type is:
    /// DateTime, DateOnly, TimeOnly, DateTimeOffset
    /// </summary>
    /// <param name="myType">Any type</param>
    /// <returns>Returns true is the type is in the list. Returns false if it doesn't</returns>
    public static bool IsDate(this Type myType)
    {
        var x = Nullable.GetUnderlyingType(myType);
        if (x == typeof(DateTime) || x == typeof(DateOnly) || x == typeof(TimeOnly) ||
            x == typeof(DateTimeOffset))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Extention to any type which returns a boolean value indicating the type is:
    /// bool
    /// </summary>
    /// <param name="myType">Any type</param>
    /// <returns>Returns true is the type is bool. Returns false if it doesn't</returns>
    public static bool IsBoolean(this Type myType)
    {
        var x = Nullable.GetUnderlyingType(myType);
        if (x == typeof(bool))
        {
            return true;
        }
        return false;
    }


}