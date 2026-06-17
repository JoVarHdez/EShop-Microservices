using System.ComponentModel.DataAnnotations;
using Shopping.Web.Razor.Models;

namespace Shopping.Web.Razor.Tests.Unit.Configuration;

public class ApiSettingsValidationTests
{
    [Fact]
    public void ApiSettings_WithGatewayAddress_IsValid()
    {
        var model = new ApiSettings { GatewayAddress = "https://localhost:6064" };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void ApiSettings_WithoutGatewayAddress_IsInvalid()
    {
        var model = new ApiSettings();

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(ApiSettings.GatewayAddress)));
    }
}
