using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers.Dto
{
    public class UpdateUserDto
    {
       [FromForm(Name = "id")]
       public long Id { get; set; }
       
       [FromForm(Name = "username")]
       public string UserName { get; set; }
       
       [FromForm(Name = "password")]
       public string Password { get; set; }
       
       [FromForm(Name = "confirm-password")]
       public string ConfirmPassword { get; set; }
    }
}