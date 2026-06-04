using System.ComponentModel.DataAnnotations;

namespace Shopping.Web.Razor.Models;

public class ApiSettings
{
    [Required]
    public string GatewayAddress { get; set; } = default!;
}
