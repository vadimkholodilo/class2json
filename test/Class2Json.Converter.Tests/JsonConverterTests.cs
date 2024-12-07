namespace Class2Json.Converter.Tests;

public class JsonConverterTests
{
    [Fact]
    public void ConvertClass_ShouldReturnEmptyString_IfSourceCodeIsEmptyString()
    {
        var sourceCode = "";
        var expectedJson = "";

        var json = JsonConverter.ConvertClass(sourceCode);

        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void ConvertClass_ShouldReturnExpectedJson_IfSourceCodeIsValid()
    {
        var sourceCode = @"
            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        ";
        var expectedJson = "{\"firstName\":\"\",\"lastName\":\"\"}";

        var json = JsonConverter.ConvertClass(sourceCode);

        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void ConvertClass_ShouldHandlePrimitiveTypes()
    {
        var sourceCode = @"
            public class Sample
            {
                public int Age { get; set; }
                public double Height { get; set; }
                public bool IsActive { get; set; }
                public string Name { get; set; }
            }
        ";
        var expectedJson = "{\"age\":0,\"height\":0,\"isActive\":false,\"name\":\"\"}";

        var json = JsonConverter.ConvertClass(sourceCode);

        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void ConvertClass_ShouldHandleNestedClasses()
    {
        var sourceCode = @"
            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public Address Address { get; set; }
            }
        ";
        var expectedJson = "{\"firstName\":\"\",\"lastName\":\"\",\"address\":{\"street\":\"\",\"city\":\"\"}}";

        var json = JsonConverter.ConvertClass(sourceCode);

        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void ConvertClass_ShouldHandleClassWithBaseClass()
    {
        var sourceCode = @"
            public class BaseClass
            {
                public int Id { get; set; }
            }

            public class DerivedClass : BaseClass
            {
                public string Name { get; set; }
            }
        ";
        var expectedJson = "{\"id\":0,\"name\":\"\"}";

        var json = JsonConverter.ConvertClass(sourceCode);

        Assert.Equal(expectedJson, json);
    }
}