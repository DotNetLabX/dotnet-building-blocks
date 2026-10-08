using Mapster;

namespace Blocks.Mapster;

public static class Extensions
{
    /// <summary>
    /// For destination records, maps the source object through the destination's first declared constructor.
    /// </summary>
    public static void MapToConstructor<TSource, TDestination>(this TypeAdapterSetter<TSource, TDestination> typeAdapterSetter)
    {
        typeAdapterSetter.MapToConstructor(typeof(TDestination).GetConstructors().First());
    }

    public static TDestination AdaptWith<TDestination>(this object source, Action<TDestination> afterMapping)
    {
        var destination = source.Adapt<TDestination>();

        afterMapping?.Invoke(destination);

        return destination;
    }

    public static object? AdaptTo(this object source, Type destinationType)
    {
        return source.Adapt(source.GetType(), destinationType);
    }
}
