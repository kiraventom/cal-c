using System;

namespace cal_c.Exception
{
    internal static class ExceptionFactory
    {
        internal static ArgumentOutOfRangeException CreateEnumException<T>(T paramValue, string paramName)
        {
            if (!paramValue.GetType().IsEnum)
                throw new ArgumentException($"{paramValue.GetType()} is not enum", nameof(paramValue));
                
            return new ArgumentOutOfRangeException(paramName, paramValue, "Unexpected enum value");
        }
    }
}