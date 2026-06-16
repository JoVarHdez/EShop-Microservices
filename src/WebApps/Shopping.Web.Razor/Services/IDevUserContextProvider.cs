using Shopping.Web.Razor.Models;

namespace Shopping.Web.Razor.Services;

public interface IDevUserContextProvider
{
    DevUserContext GetCurrent();
}