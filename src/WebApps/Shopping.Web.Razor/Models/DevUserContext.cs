using System.ComponentModel.DataAnnotations;

namespace Shopping.Web.Razor.Models;

public class DevUserContext
{
    [Required]
    public string UserName { get; set; } = default!;

    [Required]
    public Guid CustomerId { get; set; }
}