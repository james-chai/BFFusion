using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace BFFusion.Shared.Extensions;

public static class StringExtensions
{
    public static string ToKebabCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(
            input,
            "([a-z0-9])([A-Z])|([A-Z])([A-Z][a-z])",
            "$1$3-$2$4")
            .ToLowerInvariant();
    }
}

