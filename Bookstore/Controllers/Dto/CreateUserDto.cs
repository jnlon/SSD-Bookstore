using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers.Dto
{
    public class CreateUserDto
    {
       [FromForm(Name = "username")]
       public string UserName { get; set; }
       
       [FromForm(Name = "password")]
       public string Password { get; set; }
       
       [FromForm(Name = "confirm-password")]
       public string ConfirmPassword { get; set; }
       
       [FromForm(Name = "is-admin")]
       public bool IsAdmin { get; set; }
    }
}