using Microsoft.AspNetCore.Mvc;
namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly UserDataContext _context;
    public static HashSet<int?> LoggedUser = new HashSet<int?>();
    public UserController(UserDataContext context)
    {
        _context = context;
    }
    [HttpPost("login")]
    public IActionResult Login([FromForm] RegularUser user)
    {
        if(user==null)
            return BadRequest("User is null");
        //Encoding password
        byte[] b=System.Text.ASCIIEncoding.ASCII.GetBytes(user.Password);
        user.Password=Convert.ToBase64String(b);
        var dbUser = _context.RegularUsers.FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
        if (dbUser == null)
            return BadRequest("Wrong username or password");
        LoggedUser.Add(dbUser.Id);
        return Ok(dbUser.Id);
    }
    [HttpPost("register")]
    public IActionResult Register([FromForm] RegularUser user, bool testMode=false)
    {
        if(testMode) _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        var dbUser = _context.RegularUsers.FirstOrDefault(u => u.Email == user.Email);
        if (dbUser != null)
            return BadRequest("User with this email already exists");
        //Encoding password
        byte[] b=System.Text.ASCIIEncoding.ASCII.GetBytes(user.Password);
        user.Password=Convert.ToBase64String(b);
        _context.RegularUsers.Add(user);
        _context.SaveChanges();
        return Ok();
    }
    [HttpPost("logout")]
    public IActionResult Logout([FromForm] RegularUser user)
    {
        LoggedUser.Remove(user.Id);
        return Ok();
    }
}