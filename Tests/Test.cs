using Saritasa.Controllers;
using Saritasa;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using Amazon.S3;
using System.IO;
using Microsoft.AspNetCore.Http;
using Amazon.S3.Model;
using System.Text;
namespace Saritasa.Tests;
public class Test
{
    private readonly UserController _userController;
    private readonly UploadController _uploadController;
    private readonly AmazonS3Client _s3Client;
    private readonly UserDataContext _context;
    private bool LoggedIn = false;
    private bool Registered = false;
    //Test register function
    [Fact]
    public void TestRegister()
    {
        if(!LoggedIn && !Registered){
            //Create a new user
            RegularUser user = new RegularUser()
            {
                Email = "viethung.9at1@gmail.com",
                Password = "hung9at1"
            };
            //Check if the user is created
            var result = _userController.Register(user);
            var okResult = result as OkResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Registered = true;
        }
    }
    //Check login function
    [Fact]
    public void TestLogin()
    {
        if(LoggedIn) return;
        TestRegister();
        //Create a user object
        RegularUser user = new RegularUser()
        {
            Email = "viethung.9at1@gmail.com",
            Password = "hung9at1"
        };
        //Check if the user is logged in
        var result = _userController.Login(user);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        var check = UserController.LoggedUser.Contains((int?)okResult.Value);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
        LoggedIn = true;
    }
    //Check logout function
    [Fact]
    public void TestLogout()
    {
        //Login first
        TestLogin();
        //Create a user object
        RegularUser user = new RegularUser()
        {
            Email = "viethung.9at1@gmail.com",
            Password = "hung9at1",
            Id = 1
        };
        //Check if the user is logged out
        var result = _userController.Logout(user);
        var okResult = result as OkResult;
        var check = UserController.LoggedUser.Contains(user.Id);
        Assert.False(check);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }
    //Test upload function with userID=1
    [Fact]
    public async Task<string> TestFileUpload()
    {
        //Login first
        TestLogin();
        //Create a file object
        string filePath = "/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName = "Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        //Check if the file is uploaded
        var result = await _uploadController.UploadFileToS3(formFile, 1, false, true);
        var okResult = result as OkObjectResult;
        string fileUploadName = okResult.Value.ToString();
        var s3Object = await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        var list = await _context.Files.FirstOrDefaultAsync(x => x.FilePath == fileUploadName);
        Assert.NotNull(list);
        Assert.NotNull(s3Object);
        return okResult.Value.ToString();
    }
    //Check download function
    [Fact]
    public async void TestFileDownload()
    {
        //Login first
        TestLogin();
        var result = await TestFileUpload();
        var okResult = result;
        string fileUploadName = okResult;
        var s3Object = await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        var downloadResult = await _uploadController.DownloadFileFromS3(fileUploadName);
        var okDownloadResult = downloadResult as FileStreamResult;
        Assert.NotNull(okDownloadResult);
    }
    //Test delete function
    [Fact]
    public async void TestFileDelete()
    {
        //Login first
        TestLogin();
        //Create a file object
        string filePath = "/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName = "Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        //Check if the file is uploaded
        var result = await _uploadController.UploadFileToS3(formFile, 1, false, true);
        var okResult = result as OkObjectResult;
        string fileUploadName = okResult.Value.ToString();
        //Check if the file is on S3
        var s3Object = await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        //Delete the file
        var deleteResult = await _uploadController.DeleteFileInS3(fileUploadName, 1);
        //Check if the file is deleted in local database
        var list = _context.Files.FirstOrDefault(x => x.FilePath == fileUploadName);
        //Check if the file is deleted on S3
        bool checkOnS3 = false;
        try
        {
            await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        }
        catch
        {
            checkOnS3 = true;
        }
        Assert.True(checkOnS3);
        Assert.Null(list);
        var okDeleteResult = deleteResult as OkResult;
        Assert.NotNull(okDeleteResult);
    }
    //Test get list of file function
    [Fact]
    public async void TestGetListFileText()
    {
        TestLogin();
        List<string> listFileName = new List<string>();
        for(int i=0;i<3;i++){
            Task<string> task = TestFileUpload();
            listFileName.Add(await task);
        }
        for(int i=0;i<3;i++){
            string content="Love you, VN-A873, a Boeing 787-10 Dreamliner";
            var res = _uploadController.UploadText(content, 1, false, true);
            var okRes = res as OkObjectResult;
            listFileName.Add(okRes.Value.ToString());
        }
        //Check if the list is returned
        var result = _uploadController.Get(1);
        var okResult = result as OkObjectResult;
        var listOk = okResult.Value as List<Upload>;
        listOk.Select(x => x.FilePath).ToList().ForEach(x => Assert.Contains(x, listFileName));
    }
    //Test download file that will be deleted after download
    [Fact]
    public async void TestFileDownloadAndDelete()
    {
        //Login first
        TestLogin();
        //Create a file object
        string filePath = "/mnt/e/Coding/Saritasa/Code/Uploads/Image1.jpg";
        string fileName = "Image1.jpg";
        var fileStream = System.IO.File.OpenRead(filePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        //Upload file
        formFile.Headers = new HeaderDictionary();
        formFile.Headers["Content-Type"] = "image/jpeg";
        var result = await _uploadController.UploadFileToS3(formFile, 1, true, true);
        var okResult = result as OkObjectResult;
        string fileUploadName = okResult.Value.ToString();
        var s3Object = await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        //Download file
        var downloadResult = await _uploadController.DownloadFileFromS3(fileUploadName);
        //Check if the file is deleted
        bool check = false;
        var checkInList = await _context.Files.FirstOrDefaultAsync(x => x.FilePath == fileUploadName);
        Assert.Null(checkInList);
        try
        {
            await _s3Client.GetObjectAsync("saritasahung", fileUploadName);
        }
        catch
        {
            check = true;
        }
        Assert.True(check);
    }
    //Test upload text
    [Fact]
    public string TestStringUpload()
    {
        //Login first
        TestLogin();
        string content="Love you, VN-A873, a Boeing 787-10 Dreamliner";
        //Upload text
        var result = _uploadController.UploadText(content, 1, false, true);
        var okResult = result as OkObjectResult;
        var list =  _context.Texts.FirstOrDefault(x => x.FilePath == okResult.Value.ToString());
        Assert.NotNull(list);
        return okResult.Value.ToString();
    }
    //Test text download
    [Fact]
    public void TestStringDownload()
    {
        //Login first
        TestLogin();
        string fileName=TestStringUpload();
        //Download text
        var result = _uploadController.DownloadText(fileName);
        var okResult = result as OkObjectResult;
        string content="Love you, VN-A873, a Boeing 787-10 Dreamliner";
        var downloadContent=_context.Texts.FirstOrDefault(x=>x.FilePath==fileName).Content;
        var finalDownload=Encoding.UTF8.GetString(Convert.FromBase64String(downloadContent));
        Assert.Equal(content,finalDownload);
        Assert.NotNull(okResult);
    }
    //Text text that will be deleted after download
    [Fact]
    public void TestStringDownloadThenDelete()
    {
        //Login first
        TestLogin();
        string content="Love you, VN-A873, a Boeing 787-10 Dreamliner";
        //Upload text
        var result = _uploadController.UploadText(content, 1, true, true);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        string fileName=okResult.Value.ToString();
        //Download text
        var downloadResult = _uploadController.DownloadText(fileName);
        var okDownloadResult = downloadResult as OkObjectResult;
        Assert.NotNull(okDownloadResult);
        //Check if the text is deleted
        var checkInList =  _context.Texts.FirstOrDefault(x => x.FilePath == fileName);
        Assert.Null(checkInList);
    }
    //Constructor
    public Test()
    {
        //SQL Server connection string
        var connectionString = "Server=localhost;Database=Saritasa;User Id=sa;Password=Hung_9at1;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<Saritasa.UserDataContext>().UseSqlServer(connectionString).Options;
        var context = new Saritasa.UserDataContext(options);
        _userController = new UserController(context);
        var s3Client = new AmazonS3Client("AKIA2VTR55HP5BKPAXML", "Q/PNgmTlsbazLv3EDBDsWfUllhY4+3mpGFY7MV2r", Amazon.RegionEndpoint.USEast1);
        _uploadController = new UploadController(context, s3Client);
        _s3Client = s3Client;
        _context = context;
        deleteDatabase();
    }
    async void deleteDatabase()
    {
        await _uploadController.DeleteDatabase();
    }
}