using Saritasa.Controllers;
using Saritasa;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using Amazon.S3;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Saritasa.Tests;
public class Test
{
    private readonly UserController _userController;
    private readonly UploadController _uploadController;
    private readonly AmazonS3Client _s3Client;
    private readonly UserDataContext _context;
    //Test register function
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
    //Check login function
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
    //Check logout function
    [Fact]
    public void TestLogout(){
        TestLogin();
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
    //Test upload function with userID=1
    [Fact]
    public async void TestUpload(){
        TestLogin();
        string filePath="/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName="Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        var result = await _uploadController.UploadFileToS3(formFile, 1);
        var okResult=result as OkObjectResult;
        string fileUploadName=okResult.Value.ToString();
        var s3Object=await _s3Client.GetObjectAsync("saritasahung",fileUploadName);
        Assert.NotNull(s3Object);
    }
    //Check download function
    [Fact]
    public async void TestDownload(){
        TestLogin();
        string filePath="/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName="Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        var result = await _uploadController.UploadFileToS3(formFile, 1);
        var okResult=result as OkObjectResult;
        string fileUploadName=okResult.Value.ToString();
        var s3Object=await _s3Client.GetObjectAsync("saritasahung",fileUploadName);
        var downloadResult=await _uploadController.DownloadFileFromS3(fileUploadName);
        var okDownloadResult=downloadResult as FileStreamResult;
        Assert.NotNull(okDownloadResult);
    }
    //Test delete function
    [Fact]
    public async void TestDelete(){
        TestLogin();
        string filePath="/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName="Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        var result = await _uploadController.UploadFileToS3(formFile, 1);
        var okResult=result as OkObjectResult;
        string fileUploadName=okResult.Value.ToString();
        var s3Object=await _s3Client.GetObjectAsync("saritasahung",fileUploadName);
        var deleteResult=await _uploadController.DeleteFileInS3(fileUploadName, 1);
        var okDeleteResult=deleteResult as OkResult;
        Assert.NotNull(okDeleteResult);
    }   
    //Test get list of file function
    [Fact]
    public void TestGetListFile(){
        TestLogin();
        TestUpload();
        var result=_uploadController.Get(1);
        var okResult=result as OkObjectResult;
        Assert.NotNull(okResult);
    }
    //Test download file that will be deleted after download
    [Fact]
    public async void TestDownloadAndDelete(){
        TestLogin();
        string filePath="/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName="Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        var result = await _uploadController.UploadFileToS3(formFile, 1, true);
        var okResult=result as OkObjectResult;
        string fileUploadName=okResult.Value.ToString();
        var s3Object=await _s3Client.GetObjectAsync("saritasahung",fileUploadName);
        var downloadResult=await _uploadController.DownloadFileFromS3(fileUploadName);
        bool check=false;
        try{
            await _s3Client.GetObjectAsync("saritasahung",fileUploadName);
        }
        catch{
            check=true;
        }
        Assert.True(check);
    }
    //Test upload text
    [Fact]
    public async void TestStringUpload(){
        TestLogin();
        string testString="This is a test string";
        var result=await _uploadController.UploadTextToS3(testString, 1);
        var okResult=result as OkObjectResult;
        bool check=true;
        try{
            await _s3Client.GetObjectAsync("saritasahung",okResult.Value.ToString());
        }
        catch{
            check=false;
        }
        Assert.True(check);
    }
    //Test text download
    [Fact]
    public async void TestStringDownload(){
        TestLogin();
        string testString="This is a test string";
        var result=await _uploadController.UploadTextToS3(testString, 1,true);
        var okResult=result as OkObjectResult;
        var downloadResult=await _uploadController.DownloadFileFromS3(okResult.Value.ToString());
        var okDownloadResult=downloadResult as OkObjectResult;
        Assert.Equal(testString, okDownloadResult.Value.ToString());
    }
    //Text text that will be deleted after download
    [Fact]
    public async void TestStringDownloadThenDelete(){
        TestLogin();
        string testString="This is a test string";
        var result=await _uploadController.UploadTextToS3(testString, 1,true);
        var okResult=result as OkObjectResult;
        var downloadResult=await _uploadController.DownloadFileFromS3(okResult.Value.ToString());
        var okDownloadResult=downloadResult as OkObjectResult;
        bool check=false;
        try{
            await _s3Client.GetObjectAsync("saritasahung",okResult.Value.ToString());
        }
        catch{
            check=true;
        }
        Assert.True(check);
    }
    //Constructor
    public Test()
    {
        //SQL Server connection string
        var connectionString = "Server=localhost;Database=Saritasa;User Id=sa;Password=Hung_9at1;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<Saritasa.UserDataContext>().UseSqlServer(connectionString).Options;
        var context = new Saritasa.UserDataContext(options);
        _userController = new UserController(context);
        var s3Client=new AmazonS3Client("AKIA2VTR55HP5BKPAXML","Q/PNgmTlsbazLv3EDBDsWfUllhY4+3mpGFY7MV2r",Amazon.RegionEndpoint.USEast1);
        _uploadController=new UploadController(context, s3Client);
        _s3Client=s3Client;
        _context=context;
        //deleteDatabse();
    }
    async void deleteDatabse(){
        await _uploadController.DeleteDatabase();
    }
}