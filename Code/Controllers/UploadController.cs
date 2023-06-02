using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Web;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UploadController: ControllerBase{
    private readonly UserDataContext _context;
    private readonly IAmazonS3 _s3Client;
    public UploadController(UserDataContext context, IAmazonS3 s3Client){
        _context=context;
        _s3Client=s3Client;
    }
    [HttpGet]
    public string GetIpAddress(){
        IPHostEntry ipHostInfo=Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress=ipHostInfo.AddressList[0];
        int port=HttpContext.Connection.LocalPort;
        return ipAddress+":"+port;
    }
    //Upload file to file system (local machine)
    [HttpPost(template: "uploadFile")]
    public IActionResult Post(IFormFile file, int id, bool deleteAfterDownload=false){
        if(file.Length==0) return BadRequest("File is empty");
        if(!UserController.LoggedUser.Contains(id)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==id);
        if(user==null) return BadRequest("User does not exist");
        _context.Entry(user).Collection(u => u.Uploads).Load();
        var originalFileName = Path.GetFileName(file.FileName);
        var uniqueFileName=Path.GetRandomFileName()+ "."+file.FileName.Split('.')[1];
        var uniqueFilePath=Path.Combine("/mnt/e/Coding/Saritasa/Uploads",uniqueFileName);
        if(!Directory.Exists("/mnt/e/Coding/Saritasa/Uploads")){
            Directory.CreateDirectory("/mnt/e/Coding/Saritasa/Uploads");
        }
        using (var stream=System.IO.File.Create(uniqueFilePath)){
            file.CopyTo(stream);
        }
        Saritasa.Upload upload=new Saritasa.Upload{
            FilePath=uniqueFilePath,
            DeleteAfterDownload=deleteAfterDownload,
            Type=Saritasa.UploadType.File
        };
        user.Uploads.Add(upload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        return Ok("https://"+GetIpAddress()+"/upload/downloadFile/"+HttpUtility.UrlEncode(str: uniqueFilePath));
    }
    //Upload text to file system (local machine)
    [HttpPost(template: "uploadText")]
    public IActionResult Post([FromForm] string content, int userId, bool deleteAfterDownload=false){
        if(content==null) return BadRequest("Content is empty");
        if(!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==userId);
        if(user==null) return BadRequest("User does not exist");
        _context.Entry(user).Collection(u => u.Uploads).Load();
        if(user==null) return BadRequest("User does not exist");
        var uniqueFileName=Path.GetRandomFileName()+ ".txt";
        var uniqueFilePath=Path.Combine("/mnt/e/Coding/Saritasa/Uploads",uniqueFileName);
        if(!Directory.Exists("/mnt/e/Coding/Saritasa/Uploads")){
            Directory.CreateDirectory("/mnt/e/Coding/Saritasa/Uploads");
        }
        using (var stream=System.IO.File.Create(uniqueFilePath)){
            stream.Write(Encoding.ASCII.GetBytes(content));
        }
        Saritasa.Upload upload=new Saritasa.Upload{
            FilePath=uniqueFilePath,
            DeleteAfterDownload=deleteAfterDownload,
            Type=Saritasa.UploadType.Text
        };
        user.Uploads.Add(upload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        return Ok("https://"+GetIpAddress()+"/upload/downloadFile/"+HttpUtility.UrlEncode(str: uniqueFilePath));
    }
    //Download file from file system (local machine)
    [HttpGet("downloadFile/{filePath}")]
    public IActionResult DownloadFile(string filePath){
        filePath=HttpUtility.UrlDecode(filePath);
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileBytes=System.IO.File.ReadAllBytes(filePath);
        var fileName=Path.GetFileName(filePath);
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==filePath);
        if(fileObject==null) return BadRequest("File does not exist in database");
        if(fileObject.DeleteAfterDownload){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(fileObject);
            var userObject=_context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(fileObject));
            userObject.Uploads.Remove(fileObject);
            _context.SaveChanges();
        }
        return File(fileBytes, "application/force-download", fileName);
    }
    //Download text from file system (local machine)
    [HttpGet("downloadText/{filePath}")]
    public IActionResult DownloadText(string filePath){
        filePath=HttpUtility.UrlDecode(filePath);
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileBytes=System.IO.File.ReadAllBytes(filePath);
        var fileName=Path.GetFileName(filePath);
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==filePath);
        if(fileObject==null) return BadRequest("File does not exist in database");
        if(fileObject.DeleteAfterDownload){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(fileObject);
            _context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(fileObject)).Uploads.Remove(fileObject);
            _context.SaveChanges();
        }
        return File(fileBytes, "application/force-download", fileName);
    }
    //Get list of upload objects (from all sources)
    [HttpGet("getUploads")]
    public IActionResult Get(int id){
        if(!UserController.LoggedUser.Contains(id)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==id);
        _context.Entry(user).Collection(u => u.Uploads).Load();
        if(user==null) return BadRequest("User does not exist");
        return Ok(user.Uploads);
    }
    //Delete all uploads from file system (local machine)
    [HttpDelete("deleteAll")]
    public IActionResult DeleteAll(){
        var uploads=_context.Uploads.ToList();
        foreach(var upload in uploads){
            System.IO.File.Delete(upload.FilePath);
            _context.Uploads.Remove(upload);
            _context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(upload)).Uploads.Remove(upload);
        }
        _context.SaveChanges();
        return Ok();
    }
    //Delete all uploads and data from all database
    [HttpPost("deleteDatabase")]
    public async Task<IActionResult> DeleteDatabase(){
        _context.Database.EnsureDeleted();
        var isS3Exists=await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, "saritasainterview");
        if(isS3Exists)
            await Amazon.S3.Util.AmazonS3Util.DeleteS3BucketWithObjectsAsync(_s3Client, "saritasainterview");
        return Ok();
    }
    //Delete file from file system (local machine)
    [HttpPost("deleteFile")]
    public IActionResult DeleteFile([FromForm] string filePath, int userId){
        filePath=filePath.Split('/')[filePath.Split('/').Length-1];
        filePath=HttpUtility.UrlDecode(filePath);
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==filePath);
        if(fileObject==null) return BadRequest("File does not exist in database");
        var user=_context.RegularUsers.Include(u => u.Uploads).FirstOrDefault(u => u.Id==userId);
        if(user==null) return BadRequest("User does not exist");
        if(user.Uploads.Contains(fileObject)){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(fileObject);
            user.Uploads.Remove(fileObject);
            _context.SaveChanges();
            return Ok();
        }
        else return BadRequest("File does not belong to this user");
    }
    //Upload file to S3 bucket
    [HttpPost("uploadFileToS3")]
    public async Task<IActionResult> UploadFileToS3(IFormFile file, int userId, bool deleteAfterDownload=false){
        var bucketExists=await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, "saritasainterview");
        if(!bucketExists) await _s3Client.PutBucketAsync("saritasainterview");
        if(file.Length==0) return BadRequest("File is empty");
        if(!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==userId);
        if(user==null) return BadRequest("User does not exist");
        _context.Entry(user).Collection(u => u.Uploads).Load();
        var originalFileName = Path.GetFileName(file.FileName);
        var uniqueFileName=Path.GetRandomFileName()+ "."+file.FileName.Split('.')[1];
        Upload newUpload=new Upload(){
            FilePath=uniqueFileName,
            DeleteAfterDownload=deleteAfterDownload,
            Type=UploadType.File
        };
        user.Uploads.Add(newUpload);
        _context.Uploads.Add(newUpload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        var request=new PutObjectRequest(){
            BucketName="saritasainterview",
            Key=uniqueFileName,
            InputStream=file.OpenReadStream(),
            ContentType=file.ContentType
        };
        await _s3Client.PutObjectAsync(request);
        return Ok("https://"+GetIpAddress()+"/upload/downloadFromS3/"+HttpUtility.UrlEncode(str: uniqueFileName));
    }
    //Upload text to S3 bucket
    [HttpPost("uploadTextToS3")]
    public async Task<IActionResult> UploadTextToS3([FromForm] string content, int userId, bool deleteAfterDownload=false){
        var bucketExists=await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, "saritasainterview");
        if(!bucketExists) await _s3Client.PutBucketAsync("saritasainterview");
        if(content==null) return BadRequest("Content is empty");
        if(!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==userId);
        if(user==null) return BadRequest("User does not exist");
        _context.Entry(user).Collection(u => u.Uploads).Load();
        var uniqueFileName=Path.GetRandomFileName()+ ".txt";
        Upload newUpload=new Upload(){
            FilePath=uniqueFileName,
            DeleteAfterDownload=deleteAfterDownload,
            Type=UploadType.Text
        };
        user.Uploads.Add(newUpload);
        _context.Uploads.Add(newUpload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        var request=new PutObjectRequest(){
            BucketName="saritasainterview",
            Key=uniqueFileName,
            InputStream=new MemoryStream(Encoding.ASCII.GetBytes(content)),
            ContentType="text/plain"
        };
        await _s3Client.PutObjectAsync(request);
        return Ok("https://"+GetIpAddress()+"/upload/downloadFromS3/"+HttpUtility.UrlEncode(str: uniqueFileName));
    }
    //Download file from S3 bucket
    [HttpGet("downloadFromS3/{fileName}")]
    public async Task<IActionResult> DownloadFileFromS3(string fileName){
        if(fileName==null) return BadRequest("File path is empty");
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==fileName);
        if(fileObject==null) return BadRequest("File does not exist in database");
        var s3Object=await _s3Client.GetObjectAsync("saritasainterview", fileName);
        if(fileObject.DeleteAfterDownload){
            await _s3Client.DeleteObjectAsync("saritasainterview", fileName);
            _context.Uploads.Remove(fileObject);
            _context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(fileObject)).Uploads.Remove(fileObject);
            _context.SaveChanges();
        }
        return File(s3Object.ResponseStream, s3Object.Headers.ContentType, fileName);
    }
    //Get list of upload objects (from all sources)
    [HttpDelete("deleteFromS3")]
    public async Task<IActionResult> DeleteFileInS3(string fileName, int userID){
        if(fileName==null) return BadRequest("File path is empty");
        if(!UserController.LoggedUser.Contains(userID)) return BadRequest("User is not logged in");
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==fileName);
        if(fileObject==null) return BadRequest("File does not exist in database");
        var user=_context.RegularUsers.Include(u => u.Uploads).FirstOrDefault(u => u.Id==userID);
        if(user==null) return BadRequest("User does not exist");
        if(user.Uploads.Contains(fileObject)){
            await _s3Client.DeleteObjectAsync("saritasainterview", fileName);
            _context.Uploads.Remove(fileObject);
            user.Uploads.Remove(fileObject);
            _context.SaveChanges();
            return Ok();
        }
        else return BadRequest("File does not belong to this user");
    }
}