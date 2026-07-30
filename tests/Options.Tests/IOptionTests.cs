using FluentAssertions;
using Personix.Options;
using Xunit;

namespace Options.Tests;

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
        ConcreteOption.SectionName.Should().Be("MyCustomSection");
    }

    [Fact]
    public void IOption_DifferentImplementations_ShouldHaveDifferentSectionNames()
    {
        // Assert
        ConcreteOption.SectionName.Should().NotBe(AnotherOption.SectionName);
        ConcreteOption.SectionName.Should().Be("MyCustomSection");
        AnotherOption.SectionName.Should().Be("AnotherSection");
    }

    [Fact]
    public void IOption_SectionName_ShouldBeAccessibleWithoutInstance()
    {
        // Act & Assert - This compiles, proving static abstract member works
        var sectionName = ConcreteOption.SectionName;
        sectionName.Should().NotBeNullOrEmpty();
    }

    private class TestConnectionStringsOption : ConnectionStringsOption
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void ConnectionStringsOption_ShouldImplementIOption()
    {
        // Assert
        typeof(TestConnectionStringsOption).GetInterfaces().Should().Contain(typeof(IOption));
    }

    [Fact]
    public void ConnectionStringsOption_ShouldHavePredefinedSectionName()
    {
        // Assert
        TestConnectionStringsOption.SectionName.Should().Be("ConnectionStrings");
    }
}



