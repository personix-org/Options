using Shouldly;
using Xunit;

namespace Personix.Options.Tests;

public class IOptionTests
{
    private class ConcreteOption : IOption
    {
        public static string SectionName => "MyCustomSection";
        public string Value { get; set; } = string.Empty;
    }

    private class AnotherOption : IOption
    {
        public static string SectionName => "AnotherSection";
        public int Number { get; set; }
    }

    [Fact]
    public void IOption_ShouldAllowStaticAbstractMember()
    {
        // Assert
        ConcreteOption.SectionName.ShouldBe("MyCustomSection");
    }

    [Fact]
    public void IOption_DifferentImplementations_ShouldHaveDifferentSectionNames()
    {
        // Assert
        ConcreteOption.SectionName.ShouldNotBe(AnotherOption.SectionName);
        ConcreteOption.SectionName.ShouldBe("MyCustomSection");
        AnotherOption.SectionName.ShouldBe("AnotherSection");
    }

    [Fact]
    public void IOption_SectionName_ShouldBeAccessibleWithoutInstance()
    {
        // Act & Assert - This compiles, proving static abstract member works
        var sectionName = ConcreteOption.SectionName;
        sectionName.ShouldNotBeNullOrEmpty();
    }

    private class TestConnectionStringsOption : ConnectionStringsOption
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void ConnectionStringsOption_ShouldImplementIOption()
    {
        // Assert
        typeof(TestConnectionStringsOption).GetInterfaces().ShouldContain(typeof(IOption));
    }

    [Fact]
    public void ConnectionStringsOption_ShouldHavePredefinedSectionName()
    {
        // Assert
        TestConnectionStringsOption.SectionName.ShouldBe("ConnectionStrings");
    }
}
