using NetArchTest.Rules;
using Xunit;

namespace CustomerService.ArchitectureTests;
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(typeof(CustomerService.Domain.Shared.Result).Assembly).ShouldNot().HaveDependencyOn("CustomerService.Application").GetResult();
        Assert.True(result.IsSuccessful);
    }
}