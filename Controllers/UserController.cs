using Microsoft.AspNetCore.Mvc;
namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly UserDataContext _context;
    public static List<User> LoggedUser = new List<User>();
    public UserController(UserDataContext context)
    {
        _context = context;
    }
    [HttpPost("login")]
    public IActionResult Login([FromForm] RegularUser user)
    {
        var dbUser = _context.RegularUsers.FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
        if (dbUser == null)
            return BadRequest("Wrong username or password");
        LoggedUser.Add(dbUser);
        return Ok();
    }
    [HttpPost("register")]
    public IActionResult Register([FromForm] RegularUser user)
    {
        var dbUser = _context.RegularUsers.FirstOrDefault(u => u.Email == user.Email);
        if (dbUser != null)
            return BadRequest("User with this email already exists");
        _context.RegularUsers.Add(user);
        _context.SaveChanges();
        return Ok();
    }
    [HttpPost("logout")]
    public IActionResult Logout([FromForm] RegularUser user)
    {
        LoggedUser.Remove(user);
        return Ok();
    }
}