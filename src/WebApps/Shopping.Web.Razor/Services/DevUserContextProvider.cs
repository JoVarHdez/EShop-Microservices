using Microsoft.Extensions.Options;
using Shopping.Web.Razor.Models;

namespace Shopping.Web.Razor.Services;

public class DevUserContextProvider(IOptions<DevUserContext> options) : IDevUserContextProvider
{
    public DevUserContext GetCurrent() => options.Value;
}