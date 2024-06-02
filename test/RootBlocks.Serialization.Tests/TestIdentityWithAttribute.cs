namespace RootBlocks.Serialization.Tests;

[ JsonConverter( typeof( IdentityJsonConverter< TestIdentityWithAttribute > ) ) ]
public class TestIdentityWithAttribute : Identity;
