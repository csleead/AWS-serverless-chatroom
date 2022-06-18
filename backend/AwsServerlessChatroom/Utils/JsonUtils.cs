using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AwsServerlessChatroom;

namespace AwsServerlessChatroom.Utils;
public static class JsonUtils
{
    public static bool TryGetStringProperty(this JsonElement jsonElement, string propertyName, [NotNullWhen(true)] out string? value)
    {
        if (jsonElement.ValueKind != JsonValueKind.Object
            || !jsonElement.TryGetProperty(propertyName, out var prop)
            || prop.ValueKind != JsonValueKind.String)
        {
            value = null;
            return false;
        }

        value = prop.GetString()!;
        return true;
    }
}
