namespace RootBlocks.Serialization.Tests.Converters;

public class DomainEventConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new DomainEventConverter< TestDomainEventWithoutAttribute >() }
    };

    [ Fact ]
    public void ReadJson_ValidJson_ReturnsDomainEvent()
    {
        var id = Guid.Parse( "1b15c0ad-278a-494b-9b5c-41aa57a66482" );
        var causationId = Guid.Parse( "14de3512-003d-46b3-915f-bb42f9a7f2da" );
        var correlationId = Guid.Parse( "2029b400-38b5-484e-bd8e-57bd8e6a5e77" );
        var occurredOn = new DateTime( 1981, 4, 9, 16, 30, 0, DateTimeKind.Utc );
        var typeName = "TestDomainEventWithAttribute";
        var assemblyQualifiedTypeName = "RootBlocks.Serialization.Tests.TestDomainEventWithAttribute, RootBlocks.Serialization.Tests, Version\u003d1.0.0.0, Culture\u003dneutral, PublicKeyToken\u003dnull";
        var serializedEvent = $$"""
                                {
                                  "Id": "{{id}}",
                                  "CausationId": "{{causationId}}",
                                  "CorrelationId": "{{correlationId}}",
                                  "OccurredOn": "{{occurredOn:O}}",
                                  "TypeName": "{{typeName}}",
                                  "AssemblyQualifiedTypeName": "{{assemblyQualifiedTypeName}}",
                                  "Primitive": "Primitive",
                                  "Complex": {
                                    "Name": "Complex",
                                    "Value": {
                                      "Primitive": "Primitive"
                                    }
                                  }
                                }
                                """;

        var eventWithAttribute = ( TestDomainEventWithAttribute )JsonSerializer.Deserialize(
            serializedEvent,
            typeof( TestDomainEventWithAttribute )
        )!;

        Assert.NotNull( eventWithAttribute );
        Assert.Equal( id, eventWithAttribute.Id );
        Assert.Equal( causationId, eventWithAttribute.CausationId );
        Assert.Equal( correlationId, eventWithAttribute.CorrelationId );
        Assert.Equal( occurredOn, eventWithAttribute.OccurredOn );
        Assert.Equal( typeName, eventWithAttribute.TypeName );
        Assert.Equal( assemblyQualifiedTypeName, eventWithAttribute.AssemblyQualifiedTypeName );
        Assert.Equal( "Primitive", eventWithAttribute.Primitive );
        Assert.NotNull( eventWithAttribute.Complex );
        Assert.Equal( "Complex", eventWithAttribute.Complex.Name );
        Assert.NotNull( eventWithAttribute.Complex.Value );
        Assert.Equal( "Primitive", eventWithAttribute.Complex.Value.Primitive );
    }

    [ Fact ]
    public void ReadJson_ValidJson_ReturnsDomainEvent2()
    {
        var id = Guid.Parse( "1b15c0ad-278a-494b-9b5c-41aa57a66482" );
        var causationId = Guid.Parse( "14de3512-003d-46b3-915f-bb42f9a7f2da" );
        var correlationId = Guid.Parse( "2029b400-38b5-484e-bd8e-57bd8e6a5e77" );
        var occurredOn = new DateTime( 1981, 4, 9, 16, 30, 0, DateTimeKind.Utc );
        var typeName = "TestDomainEventWithoutAttribute";
        var assemblyQualifiedTypeName = "RootBlocks.Serialization.Tests.TestDomainEventWithoutAttribute, RootBlocks.Serialization.Tests, Version\u003d1.0.0.0, Culture\u003dneutral, PublicKeyToken\u003dnull";
        var serializedEvent = $$"""
                                {
                                  "Id": "{{id}}",
                                  "CausationId": "{{causationId}}",
                                  "CorrelationId": "{{correlationId}}",
                                  "OccurredOn": "{{occurredOn:O}}",
                                  "TypeName": "{{typeName}}",
                                  "AssemblyQualifiedTypeName": "{{assemblyQualifiedTypeName}}",
                                  "Primitive": "Primitive",
                                  "Complex": {
                                    "Name": "Complex",
                                    "Value": {
                                      "Primitive": "Primitive"
                                    }
                                  }
                                }
                                """;
        var eventWithoutAttribute = ( TestDomainEventWithoutAttribute )JsonSerializer.Deserialize(
            serializedEvent,
            typeof( TestDomainEventWithoutAttribute ),
            _options
        )!;

        Assert.NotNull( eventWithoutAttribute );
        Assert.IsType< TestDomainEventWithoutAttribute >( eventWithoutAttribute );
        Assert.NotNull( eventWithoutAttribute );
        Assert.Equal( id, eventWithoutAttribute.Id );
        Assert.Equal( causationId, eventWithoutAttribute.CausationId );
        Assert.Equal( correlationId, eventWithoutAttribute.CorrelationId );
        Assert.Equal( occurredOn, eventWithoutAttribute.OccurredOn );
        Assert.Equal( typeName, eventWithoutAttribute.TypeName );
        Assert.Equal( assemblyQualifiedTypeName, eventWithoutAttribute.AssemblyQualifiedTypeName );
        Assert.Equal( "Primitive", eventWithoutAttribute.Primitive );
        Assert.NotNull( eventWithoutAttribute.Complex );
        Assert.Equal( "Complex", eventWithoutAttribute.Complex.Name );
        Assert.NotNull( eventWithoutAttribute.Complex.Value );
        Assert.Equal( "Primitive", eventWithoutAttribute.Complex.Value.Primitive );
    }

    [ Fact ]
    public void ReadJson_ValidJson_ReturnsDomainEvent3()
    {
        var id = Guid.Parse( "1b15c0ad-278a-494b-9b5c-41aa57a66482" );
        var causationId = Guid.Parse( "14de3512-003d-46b3-915f-bb42f9a7f2da" );
        var correlationId = Guid.Parse( "2029b400-38b5-484e-bd8e-57bd8e6a5e77" );
        var occurredOn = new DateTime( 1981, 4, 9, 16, 30, 0, DateTimeKind.Utc );
        var typeName = "TestDomainEventWithoutAttribute";
        var assemblyQualifiedTypeName = "RootBlocks.Serialization.Tests.TestDomainEventWithoutAttribute, RootBlocks.Serialization.Tests, Version\u003d1.0.0.0, Culture\u003dneutral, PublicKeyToken\u003dnull";
        var serializedEvent = $$"""
                                {
                                  "Id": "{{id}}",
                                  "CausationId": "{{causationId}}",
                                  "CorrelationId": "{{correlationId}}",
                                  "OccurredOn": "{{occurredOn:O}}",
                                  "TypeName": "{{typeName}}",
                                  "AssemblyQualifiedTypeName": "{{assemblyQualifiedTypeName}}",
                                  "Primitive": "Primitive",
                                  "Complex": {
                                    "Name": "Complex",
                                    "Value": {
                                      "Primitive": "Primitive"
                                    }
                                  }
                                }
                                """;
        var eventWithoutAttribute = ( TestDomainEventWithoutAttribute )JsonSerializer.Deserialize(
            serializedEvent,
            typeof( TestDomainEventWithoutAttribute )
        )!;

        Assert.NotNull( eventWithoutAttribute );
        Assert.IsType< TestDomainEventWithoutAttribute >( eventWithoutAttribute );
        Assert.NotNull( eventWithoutAttribute );
        Assert.NotEqual( id, eventWithoutAttribute.Id );
        Assert.Null( eventWithoutAttribute.CausationId );
        Assert.Equal( Guid.Empty, eventWithoutAttribute.CorrelationId );
        Assert.NotEqual( occurredOn, eventWithoutAttribute.OccurredOn );
        Assert.Equal( typeName, eventWithoutAttribute.TypeName );
        Assert.Equal( assemblyQualifiedTypeName, eventWithoutAttribute.AssemblyQualifiedTypeName );
        Assert.Equal( "Primitive", eventWithoutAttribute.Primitive );
        Assert.NotNull( eventWithoutAttribute.Complex );
        Assert.Equal( "Complex", eventWithoutAttribute.Complex.Name );
        Assert.NotNull( eventWithoutAttribute.Complex.Value );
        Assert.Equal( "Primitive", eventWithoutAttribute.Complex.Value.Primitive );
    }

    [ Fact ]
    public void WriteJson_ValidDomainEvent_WritesDomainEvent()
    {
        var causationIdString = "14de3512-003d-46b3-915f-bb42f9a7f2da";
        var correlationIdString = "2029b400-38b5-484e-bd8e-57bd8e6a5e77";
        var causationId = Guid.Parse( causationIdString );
        var correlationId = Guid.Parse( correlationIdString );
        var eventWithAttribute = new TestDomainEventWithAttribute(
            new Complex
            {
                Name = "Complex",
                Value = new Value
                {
                    Primitive = "Primitive"
                }
            }
        );
        eventWithAttribute.SetCorrelationIds( causationId, correlationId );
        var serializedEventWithAttribute = JsonSerializer.Serialize( eventWithAttribute );
        using var jsonDocument = JsonDocument.Parse( serializedEventWithAttribute );
        var rootElement = jsonDocument.RootElement;

        Assert.NotNull( serializedEventWithAttribute );
        Assert.Equal( eventWithAttribute.Id, rootElement.GetProperty( "Id" ).GetGuid() );
        Assert.Equal( causationIdString, rootElement.GetProperty( "CausationId" ).GetString() );
        Assert.Equal( correlationIdString, rootElement.GetProperty( "CorrelationId" ).GetString() );
        Assert.Equal( eventWithAttribute.OccurredOn, rootElement.GetProperty( "OccurredOn" ).GetDateTime() );
        Assert.Equal( eventWithAttribute.TypeName, rootElement.GetProperty( "TypeName" ).GetString() );
        Assert.Equal( eventWithAttribute.AssemblyQualifiedTypeName, rootElement.GetProperty( "AssemblyQualifiedTypeName" ).GetString() );
        Assert.Equal( eventWithAttribute.Primitive, rootElement.GetProperty( "Primitive" ).GetString() );
        Assert.Equal( "Complex", rootElement.GetProperty( "Complex" ).GetProperty( "Name" ).GetString() );
        Assert.Equal( "Primitive", rootElement.GetProperty( "Complex" ).GetProperty( "Value" ).GetProperty( "Primitive" ).GetString() );

        var eventWithoutAttribute = new TestDomainEventWithoutAttribute
        {
            Complex = new Complex
            {
                Name = "Complex",
                Value = new Value
                {
                    Primitive = "Primitive"
                }
            }
        };
        eventWithoutAttribute.SetCorrelationIds( causationId, correlationId );
        var serializedEventWithoutAttribute = JsonSerializer.Serialize( eventWithoutAttribute );
        using var jsonDocument2 = JsonDocument.Parse( serializedEventWithoutAttribute );
        var rootElement2 = jsonDocument2.RootElement;

        Assert.NotNull( serializedEventWithoutAttribute );
        Assert.Equal( eventWithoutAttribute.Id, rootElement2.GetProperty( "Id" ).GetGuid() );
        Assert.Equal( causationIdString, rootElement2.GetProperty( "CausationId" ).GetString() );
        Assert.Equal( correlationIdString, rootElement2.GetProperty( "CorrelationId" ).GetString() );
        Assert.Equal( eventWithoutAttribute.OccurredOn, rootElement2.GetProperty( "OccurredOn" ).GetDateTime() );
        Assert.Equal( eventWithoutAttribute.TypeName, rootElement2.GetProperty( "TypeName" ).GetString() );
        Assert.Equal( eventWithoutAttribute.AssemblyQualifiedTypeName, rootElement2.GetProperty( "AssemblyQualifiedTypeName" ).GetString() );
        Assert.Equal( eventWithoutAttribute.Primitive, rootElement2.GetProperty( "Primitive" ).GetString() );
        Assert.Equal( "Complex", rootElement2.GetProperty( "Complex" ).GetProperty( "Name" ).GetString() );
        Assert.Equal( "Primitive", rootElement2.GetProperty( "Complex" ).GetProperty( "Value" ).GetProperty( "Primitive" ).GetString() );
    }
}
