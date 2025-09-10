using System.Text.Json;

namespace DocHub.API.Extensions;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(this object obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
            return string.Empty;

        try
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static object? FromJson(this string json, Type type, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize(json, type, options ?? DefaultOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
