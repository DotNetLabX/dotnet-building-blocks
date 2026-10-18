using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Blocks.EntityFrameworkCore;

public static class BuilderExtensions
{
    public static PropertyBuilder<TEnum> HasEnumConversion<TEnum>(this PropertyBuilder<TEnum> builder, int maxLength = 64)
        where TEnum : Enum
    {
        return builder
        .HasConversion(
            e => e.ToString(),
            value => (TEnum)Enum.Parse(typeof(TEnum), value))
        .HasMaxLength(maxLength);
    }

    public static PropertyBuilder<TEnum> HasEnumAsStringConversion<TEnum>(this PropertyBuilder<TEnum> builder, int maxLength = 64)
        where TEnum : Enum
    {
        return builder.HasConversion<string>().HasMaxLength(maxLength);
    }

    public static PropertyBuilder<TEnum> HasEnumToStringConversion<TEnum>(this PropertyBuilder<TEnum> builder, int maxLength = 64)
        where TEnum : struct, Enum
    {
        return builder
            .HasConversion(new EnumToStringConverter<TEnum>())
            .HasMaxLength(maxLength);
    }

    public static PropertyBuilder<IReadOnlyList<string>> HasCsvListConversion(this PropertyBuilder<IReadOnlyList<string>> builder)
    {
        Func<IReadOnlyList<string>, string> serializeFunc = v => v != null && v.Any() ? string.Join(',', v) : string.Empty;
        Func<string, IReadOnlyList<string>> deserializeFunc = v =>
            !string.IsNullOrEmpty(v) ? v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();

        var csvListConverter = new ValueConverter<IReadOnlyList<string>, string>(
            v => serializeFunc(v),
            v => deserializeFunc(v)
        );

        return builder.HasConversion(csvListConverter);
    }

    public static ValueConverter<IReadOnlyList<T>, string> BuildJsonReadOnlyListConvertor<T>()
        => BuildJsonReadOnlyListConvertor<T>(JsonSerializerOptions.Default);

    public static ValueConverter<IReadOnlyList<T>, string> BuildJsonReadOnlyListConvertor<T>(JsonSerializerOptions options)
        => new(
            v => Serialize(v, options),
            v => DeserializeList<IReadOnlyList<T>>(v, options));

    public static ValueConverter<TCollection, string> BuildJsonListConvertor<TCollection>()
        => BuildJsonListConvertor<TCollection>(JsonSerializerOptions.Default);

    public static ValueConverter<TCollection, string> BuildJsonListConvertor<TCollection>(JsonSerializerOptions options)
        => new(
            v => Serialize(v, options),
            v => DeserializeList<TCollection>(v, options));

    public static ValueComparer<T> BuildJsonValueComparer<T>()
        => BuildJsonValueComparer<T>(JsonSerializerOptions.Default);

    public static ValueComparer<T> BuildJsonValueComparer<T>(JsonSerializerOptions options)
        => new(
            (left, right) => Serialize(left, options) == Serialize(right, options),
            v => Serialize(v, options).GetHashCode(),
            v => Deserialize<T>(Serialize(v, options), options));

    public static PropertyBuilder<IReadOnlyList<T>> HasJsonReadOnlyListConversion<T>(this PropertyBuilder<IReadOnlyList<T>> builder)
        => builder.HasJsonReadOnlyListConversion(JsonSerializerOptions.Default);

    public static PropertyBuilder<IReadOnlyList<T>> HasJsonReadOnlyListConversion<T>(this PropertyBuilder<IReadOnlyList<T>> builder, JsonSerializerOptions options)
        => builder.HasConversion(BuildJsonReadOnlyListConvertor<T>(options), BuildJsonValueComparer<IReadOnlyList<T>>(options));

    public static PropertyBuilder<T> HasJsonCollectionConversion<T>(this PropertyBuilder<T> builder)
        => builder.HasJsonCollectionConversion(JsonSerializerOptions.Default);

    public static PropertyBuilder<T> HasJsonCollectionConversion<T>(this PropertyBuilder<T> builder, JsonSerializerOptions options)
        => builder.HasConversion(BuildJsonListConvertor<T>(options), BuildJsonValueComparer<T>(options));

    public static ComplexTypePropertyBuilder<T> HasJsonListConversion<T>(this ComplexTypePropertyBuilder<T> builder)
        => builder.HasJsonListConversion(JsonSerializerOptions.Default);

    public static ComplexTypePropertyBuilder<T> HasJsonListConversion<T>(this ComplexTypePropertyBuilder<T> builder, JsonSerializerOptions options)
        => builder.HasConversion(BuildJsonListConvertor<T>(options), BuildJsonValueComparer<T>(options));

    public static PropertyBuilder<TCollection> HasJsonListConversion<TCollection, T>(this PropertyBuilder<TCollection> builder)
        where TCollection : IList<T>
        => builder.HasJsonListConversion<TCollection, T>(JsonSerializerOptions.Default);

    public static PropertyBuilder<TCollection> HasJsonListConversion<TCollection, T>(this PropertyBuilder<TCollection> builder, JsonSerializerOptions options)
        where TCollection : IList<T>
        => builder.HasConversion(BuildJsonListConvertor<TCollection>(options), BuildJsonValueComparer<TCollection>(options));

    private static string Serialize<T>(T value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(value, options);

    private static T Deserialize<T>(string json, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<T>(json, options)!;

    private static T DeserializeList<T>(string? json, JsonSerializerOptions options)
        => Deserialize<T>(json ?? "[]", options);

    public static PropertyBuilder<TProperty> HasColumnNameSameAsProperty<TProperty>(this PropertyBuilder<TProperty> builder)
        => builder.HasColumnName(builder.Metadata.PropertyInfo?.Name);
    
    public static ComplexTypePropertyBuilder<TProperty> HasColumnNameSameAsProperty<TProperty>(this ComplexTypePropertyBuilder<TProperty> builder)
        => builder.HasColumnName(builder.Metadata.PropertyInfo?.Name);
}
