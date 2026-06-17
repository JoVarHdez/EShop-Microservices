using System.ComponentModel.DataAnnotations;
using Shopping.Web.Razor.Models;

namespace Shopping.Web.Razor.Tests.Unit.Configuration;

public class DevUserContextValidationTests
{
    [Fact]
    public void DevUserContext_WithRequiredValues_IsValid()
    {
        var model = new DevUserContext
        {
            UserName = "swn",
            CustomerId = Guid.NewGuid()
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void DevUserContext_WithoutUserName_IsInvalid()
    {
        var model = new DevUserContext
        {
            CustomerId = Guid.NewGuid()
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(DevUserContext.UserName)));
    }
}
