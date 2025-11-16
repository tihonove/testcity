using System.Data.Common;
using System.Reflection;

namespace TestCity.Clickhouse;

public static class DataReaderExtensions
{
    private class PropertyMapping
    {
        public PropertyInfo Property { get; init; } = null!;
        public string ColumnName { get; init; } = null!;
        public Type PropertyType { get; init; } = null!;
        public bool IsString { get; init; }
        public int ColumnIndex { get; set; } = -1;
    }

    public static async Task<T?> ReadSingleAsync<T>(this DbDataReader reader, CancellationToken ct = default) where T : class, new()
    {
        if (!await reader.ReadAsync(ct))
            return null;

        var mapper = CreateMapper<T>(reader);
        return mapper(reader);
    }

    public static async Task<List<T>> ReadAllAsync<T>(this DbDataReader reader, CancellationToken ct = default) where T : class, new()
    {
        var results = new List<T>();
        var mapper = CreateMapper<T>(reader);

        while (await reader.ReadAsync(ct))
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    private static Func<DbDataReader, T> CreateMapper<T>(DbDataReader reader) where T : class, new()
    {
        var columnMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnMapping[reader.GetName(i)] = i;
        }

        var propertyMappings = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p =>
            {
                var c = p.GetCustomAttribute<ColumnBindingAttribute>()?.ColumnName ?? p.Name;
                return new PropertyMapping
                {
                    Property = p,
                    ColumnName = c,
                    PropertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType,
                    IsString = p.PropertyType == typeof(string),
                    ColumnIndex = columnMapping.TryGetValue(c, out var index) ? index : -1
                };
            })
            .Where(pm => pm.ColumnIndex >= 0)
            .ToArray();
        return r => MapToObject<T>(r, propertyMappings);
    }

    private static T MapToObject<T>(DbDataReader reader, PropertyMapping[] propertyMappings) where T : class, new()
    {
        var result = new T();

        foreach (var mapping in propertyMappings)
        {
            var value = reader.IsDBNull(mapping.ColumnIndex) ? null : reader.GetValue(mapping.ColumnIndex);

            if (value != null)
            {
                if (mapping.IsString)
                {
                    mapping.Property.SetValue(result, value.ToString());
                }
                else if (value.GetType() == mapping.PropertyType || mapping.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    mapping.Property.SetValue(result, value);
                }
                else
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, mapping.PropertyType);
                        mapping.Property.SetValue(result, convertedValue);
                    }
                    catch
                    {
                        throw new InvalidCastException($"Cannot convert value of column '{mapping.ColumnName}' from type '{value.GetType()}' to '{mapping.PropertyType}'");
                    }
                }
            }
            else if (mapping.IsString)
            {
                mapping.Property.SetValue(result, string.Empty);
            }
        }

        return result;
    }
}
