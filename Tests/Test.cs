using Saritasa.Controllers;
using Saritasa;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
namespace Saritasa.Tests;
public class Test
{
    private readonly UserController _userController;
    [Fact]
    public void TestRegister()
    {
        RegularUser user = new RegularUser()
        {
            Email = "viethung.9at1@gmail.com",
            Password = "hung9at1"
        };
        var result = _userController.Register(user);
        var okResult = result as OkResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }
    [Fact]
    public void TestLogin()
    {
        RegularUser user = new RegularUser()
        {
            Email = "viethung.9at1@gmail.com",
            Password = "hung9at1"
        };
        var result = _userController.Login(user);
        var okResult = result as OkResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }
    [Fact]
    public void TestLogout(){
        RegularUser user = new RegularUser()
        {
            Email = "viethung.9at1@gmail.com",
            Password = "hung9at1"
        };
        var result = _userController.Logout(user);
        var okResult = result as OkResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }
    public Test()
    {
        var connectionString = "Server=localhost;Database=Saritasa;User Id=sa;Password=Hung_9at1;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<Saritasa.UserDataContext>().UseSqlServer(connectionString).Options;
        var context = new Saritasa.UserDataContext(options);
        _userController = new UserController(context);
    }
}