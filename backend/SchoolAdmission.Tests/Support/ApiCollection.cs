namespace SchoolAdmission.Tests.Support;

// Every test class that uses [Collection(ApiCollection.Name)] shares one ApiFixture.
// Classes in the same collection run one after another, never in parallel.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
