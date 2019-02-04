using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace SitefinityWebApp.Mvc.Models
{
    public class EmailWidgetModel
    {
        [Required]
        [RegularExpression(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", ErrorMessage = "Error: Invalid email address.")]
        public string Email { get; set; }
    }
}