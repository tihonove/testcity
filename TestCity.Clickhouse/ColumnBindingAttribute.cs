namespace TestCity.Clickhouse;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnBindingAttribute(string columnName) : Attribute
{
    public string ColumnName { get; } = columnName;
}
