using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BuildingBlocks.Serialization.Newtonsoft.Json.ContractResolvers;

public class IgnorePropertiesResolver : DefaultContractResolver
{
    private readonly HashSet< string > _ignoreProperties;

    public IgnorePropertiesResolver( IEnumerable< string > propertiesToIgnore )
        => _ignoreProperties = new HashSet< string >( propertiesToIgnore );

    protected override JsonProperty CreateProperty( MemberInfo member, MemberSerialization memberSerialization )
    {
        var property = base.CreateProperty( member, memberSerialization );

        if ( _ignoreProperties.Contains( property.PropertyName ) )
            property.ShouldSerialize = _ => false;

        return property;
    }
}
