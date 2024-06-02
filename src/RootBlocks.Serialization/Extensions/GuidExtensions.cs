namespace RootBlocks.Serialization.Extensions;

internal static class GuidExtensions
{
    internal static T ToIdentity< T >( this Guid guid )
        where T : Identity
    {
        var identity = Activator.CreateInstance< T >();
        var setter = ReflectionHelper.GetSetterForProperty< T, Guid >( x => x.Value );
        setter.Invoke( identity, guid );
        return identity;
    }
}
