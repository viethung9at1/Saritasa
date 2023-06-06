using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Web;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Collections;

namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UploadController : ControllerBase
{
    private readonly UserDataContext _context;
    private readonly IAmazonS3 _s3Client;
    public UploadController(UserDataContext context, IAmazonS3 s3Client)
    {
        _context = context;
        _s3Client = s3Client;
    }
    [HttpGet]
    public string GetIpAddress()
    {
        string url =HttpContext.Request.Host.Value;
        return url;
    }
    //Upload file to file system (local machine)
    [HttpPost(template: "uploadFile")]
    public IActionResult UploadFileToLocal(IFormFile file, int id, bool deleteAfterDownload = false)
    {
        //Check if file is empty
        if (file.Length == 0) return BadRequest("File is empty");
        //Check if user is logged in
        if (!UserController.LoggedUser.Contains(id)) return BadRequest("User is not logged in");
        //Check if user exists
        var user = _context.RegularUsers.FirstOrDefault(u => u.Id == id);
        if (user == null) return BadRequest("User does not exist");
        //Load user uploads
        _context.Entry(user).Collection(u => u.Files).Load();
        //Get and generate file name
        var originalFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = Path.GetRandomFileName() + "." + file.FileName.Split('.')[1];
        var uniqueFilePath = Path.Combine("/mnt/e/Coding/Saritasa/Uploads", uniqueFileName);
        //Create directory if it does not exist
        if (!Directory.Exists("/mnt/e/Coding/Saritasa/Uploads"))
        {
            Directory.CreateDirectory("/mnt/e/Coding/Saritasa/Uploads");
        }
        //Create file
        using (var stream = System.IO.File.Create(uniqueFilePath))
        {
            file.CopyTo(stream);
        }
        //Create file upload object
        Saritasa.File fileUpload = new Saritasa.File
        {
            FilePath = uniqueFilePath,
            DeleteAfterDownload = deleteAfterDownload,
            Type = Saritasa.UploadType.File
        };
        //Add upload object to user
        user.Files.Add(fileUpload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        return Ok("http://" + GetIpAddress() + "/upload/downloadFile/" + HttpUtility.UrlEncode(str: uniqueFilePath));
    }
    //Upload text to file system (local machine)
    [HttpPost(template: "uploadText")]
    public IActionResult UploadText([FromForm] string content, int userId, bool deleteAfterDownload = false, bool testMode = false)
    {
        //Check if content is empty
        if (content == null) return BadRequest("Content is empty");
        //Check if user is logged in
        if (!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        //Check if user exists
        var user = _context.RegularUsers.FirstOrDefault(u => u.Id == userId);
        if (user == null) return BadRequest("User does not exist");
        //Load user text uploads
        _context.Entry(user).Collection(u => u.Texts).Load();
        var uniqueFileName = Path.GetRandomFileName();
        //Encode content to base64
        content = Convert.ToBase64String(Encoding.ASCII.GetBytes(content));
        //Create text upload object
        Saritasa.Text textUpload = new Saritasa.Text
        {
            FilePath = uniqueFileName,
            DeleteAfterDownload = deleteAfterDownload,
            Type = Saritasa.UploadType.Text,
            Content = content
        };
        //Add upload object to user
        user.Texts.Add(textUpload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        if(!testMode) return Ok("http://" + GetIpAddress() + "/upload/downloadText/" + HttpUtility.UrlEncode(str: uniqueFileName));
        else return Ok(uniqueFileName);
    }
    //Download file from file system (local machine)
    [HttpGet("downloadFile/{filePath}")]
    public IActionResult DownloadFile(string filePath)
    {
        filePath = HttpUtility.UrlDecode(filePath);
        if (filePath == null) return BadRequest("File path is empty");
        if (!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = Path.GetFileName(filePath);
        var fileObject = _context.Files.FirstOrDefault(u => u.FilePath == filePath);
        if (fileObject == null) return BadRequest("File does not exist in database");
        if (fileObject.DeleteAfterDownload)
        {
            System.IO.File.Delete(filePath);
            _context.Files.Remove(fileObject);
            var userObject = _context.RegularUsers.FirstOrDefault(u => u.Files.Contains(fileObject));
            userObject.Files.Remove(fileObject);
            _context.SaveChanges();
        }
        return File(fileBytes, "application/force-download", fileName);
    }
    //Download text from file system (local machine)
    [HttpGet("downloadText/{filePath}")]
    public IActionResult DownloadText(string filePath)
    {
        //Check if file path is empty
        if (filePath == null) return BadRequest("File path is empty");
        //Decode from URL
        filePath = HttpUtility.UrlDecode(filePath);
        //Get text object from database
        var textObjectInDatabase = _context.Texts.FirstOrDefault(u => u.FilePath == filePath);
        if (textObjectInDatabase == null) return BadRequest("Text does not exist in database");
        if (textObjectInDatabase.DeleteAfterDownload)
        {
            _context.Texts.Remove(textObjectInDatabase);
            var userObject = _context.RegularUsers.FirstOrDefault(u => u.Texts.Contains(textObjectInDatabase));
            if (userObject == null) return BadRequest("User does not exist");
            userObject.Texts.Remove(textObjectInDatabase);
            _context.SaveChanges();
        }
        //Decoding from base64
        var textBytes = Encoding.UTF8.GetString(Convert.FromBase64String(textObjectInDatabase.Content));
        return Ok(textBytes);
    }
    //Get list of upload objects (from all sources)
    [HttpGet("getUploads")]
    public IActionResult Get(int id)
    {
        //Check if user is logged in
        if (!UserController.LoggedUser.Contains(id)) return BadRequest("User is not logged in");
        var user = _context.RegularUsers.FirstOrDefault(u => u.Id == id);
        //Check if user exists
        if (user == null) return BadRequest("User does not exist");
        //Load user uploads
        _context.Entry(user).Collection(u => u.Files).Load();
        _context.Entry(user).Collection(u => u.Texts).Load();
        ArrayList uploads = new ArrayList();
        user.Texts.ForEach(t => t.Content=Encoding.UTF8.GetString(Convert.FromBase64String(t.Content)));
        uploads.AddRange(user.Files);
        uploads.AddRange(user.Texts);
        return Ok(uploads);
    }
    //Delete all uploads from file system (local machine)
    [HttpDelete("deleteAll")]
    public IActionResult DeleteAll()
    {
        var uploads = _context.Files.ToList();
        foreach (var upload in uploads)
        {
            System.IO.File.Delete(upload.FilePath);
            _context.Files.Remove(upload);
            _context.RegularUsers.FirstOrDefault(u => u.Files.Contains(upload)).Files.Remove(upload);
        }
        var texts = _context.Texts.ToList();
        foreach (var text in texts)
        {
            _context.Texts.Remove(text);
            _context.RegularUsers.FirstOrDefault(u => u.Texts.Contains(text)).Texts.Remove(text);
        }
        _context.SaveChanges();
        return Ok();
    }
    //Delete all uploads and data from all database
    [HttpDelete("deleteDatabase")]
    public async Task<IActionResult> DeleteDatabase()
    {
        _context.Database.EnsureDeleted();
        var isS3Exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, "saritasahung");
        if (isS3Exists)
        {
            ListObjectsRequest listObjectsRequest = new ListObjectsRequest
            {
                BucketName = "saritasahung"
            };
            ListObjectsResponse listObjectsResponse;
            do
            {
                listObjectsResponse = await _s3Client.ListObjectsAsync(listObjectsRequest);
                foreach (var file in listObjectsResponse.S3Objects)
                {
                    await _s3Client.DeleteObjectAsync("saritasahung", file.Key);
                }
                listObjectsRequest.Marker = listObjectsResponse.NextMarker;
            } while (listObjectsResponse.IsTruncated);
        }
        return Ok();
    }
    //Delete file from file system (local machine)
    [HttpPost("deleteFile")]
    public IActionResult DeleteFile([FromForm] string filePath, int userId)
    {
        filePath = filePath.Split('/')[filePath.Split('/').Length - 1];
        filePath = HttpUtility.UrlDecode(filePath);
        if (filePath == null) return BadRequest("File path is empty");
        if (!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileObject = _context.Files.FirstOrDefault(u => u.FilePath == filePath);
        if (fileObject == null) return BadRequest("File does not exist in database");
        var user = _context.RegularUsers.Include(u => u.Files).FirstOrDefault(u => u.Id == userId);
        if (user == null) return BadRequest("User does not exist");
        if (user.Files.Contains(fileObject))
        {
            System.IO.File.Delete(filePath);
            _context.Files.Remove(fileObject);
            user.Files.Remove(fileObject);
            _context.SaveChanges();
            return Ok();
        }
        else return BadRequest("File does not belong to this user");
    }
    //Upload file to S3 bucket
    [HttpPost("uploadFileToS3")]
    public async Task<IActionResult> UploadFileToS3(IFormFile file, int userId, bool deleteAfterDownload = false, bool testMode = false)
    {
        //Check if file is empty
        if (file.Length == 0) return BadRequest("File is empty");
        Task<bool> bucketExistsTask = Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, "saritasahung");
        //Check if user is logged in
        if (!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        //Check if user exists
        Task<RegularUser> userTask = _context.RegularUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (!await bucketExistsTask) await _s3Client.PutBucketAsync("saritasahung");
        var user = await userTask;
        if(user == null) return BadRequest("User does not exist");
        //Get and generate file name
        var originalFileName = Path.GetFileName(file.FileName);
        //Create upload object
        var uniqueFileName = Path.GetRandomFileName() + "." + file.FileName.Split('.')[1];
        //Create request
        var request = new PutObjectRequest()
        {
            BucketName = "saritasahung",
            Key = uniqueFileName,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType
        };
        //Upload file to S3 bucket
        Task uploadTask = _s3Client.PutObjectAsync(request);
        //Load user uploads
        _context.Entry(user).Collection(u => u.Files).Load();
        Saritasa.File newUpload = new Saritasa.File()
        {
            FilePath = uniqueFileName,
            DeleteAfterDownload = deleteAfterDownload,
            Type = UploadType.File,
            OriginalFileName = originalFileName
        };
        //Add upload object to user
        user.Files.Add(newUpload);
        _context.Files.Add(newUpload);
        _context.RegularUsers.Update(user);
        Task saveTask = _context.SaveChangesAsync();
        //Wait async task to complete
        await uploadTask;
        await saveTask;
        if (!testMode) return Ok("http://" + GetIpAddress() + "/upload/downloadFromS3/" + HttpUtility.UrlEncode(str: uniqueFileName));
        else return Ok(uniqueFileName);
    }
    //Download file from S3 bucket
    [HttpGet("downloadFromS3/{fileName}")]
    public async Task<IActionResult> DownloadFileFromS3(string fileName)
    {
        //Check if file name is empty
        if (fileName == null) return BadRequest("File path is empty");
        //Check if file exists in S3 bucket
        Task<GetObjectResponse> s3ObjectTask;
        try
        {
            s3ObjectTask = _s3Client.GetObjectAsync("saritasahung", fileName);
        }
        catch (AmazonS3Exception e)
        {
            return BadRequest("File does not exist in S3 bucket");
        }
        //Check if file exists in database
        var fileObject = _context.Files.FirstOrDefault(u => u.FilePath == fileName);
        if (fileObject == null) return BadRequest("File does not exist in database");
        //Wait for s3ObjectTask to complete
        var s3Object=await s3ObjectTask;
        if (s3Object == null) return BadRequest("File does not exist in S3 bucket");
        //Delete file from S3 bucket if DeleteAfterDownload is true
        if (fileObject.DeleteAfterDownload)
        {
            //Delete file from S3 bucket
            Task deleteTask = _s3Client.DeleteObjectAsync("saritasahung", fileName);
            //Delete file from database
            _context.Files.Remove(fileObject);
            if (_context.RegularUsers.FirstOrDefault(u => u.Files.Contains(fileObject)) != null)
                _context.RegularUsers.FirstOrDefault(u => u.Files.Contains(fileObject)).Files.Remove(fileObject);
            _context.SaveChanges();
            await deleteTask;
        }
        //Read file from S3 bucket
        StreamReader sr = new StreamReader(s3Object.ResponseStream);
        return File(sr.BaseStream, s3Object.Headers.ContentType, fileName);
    }
    //Get list of upload objects (from all sources)
    [HttpDelete("deleteFromS3")]
    public async Task<IActionResult> DeleteFileInS3(string fileName, int userID)
    {
        //Check if file name is empty
        if (fileName == null) return BadRequest("File path is empty");
        //Check if user is logged in
        if (!UserController.LoggedUser.Contains(userID)) return BadRequest("User is not logged in");
        //Check if file exists in database
        var fileObject = _context.Files.FirstOrDefault(u => u.FilePath == fileName);
        if (fileObject == null) return BadRequest("File does not exist in database");
        //Check if user exists
        var user = _context.RegularUsers.Include(u => u.Files).FirstOrDefault(u => u.Id == userID);
        if (user == null) return BadRequest("User does not exist");
        //Delete file from S3 bucket
        if (user.Files.Contains(fileObject))
        {
            //Delete file from S3 bucket
            Task deleteTask= _s3Client.DeleteObjectAsync("saritasahung", fileName);
            _context.Files.Remove(fileObject);
            user.Files.Remove(fileObject);
            _context.SaveChanges();
            await deleteTask;
            return Ok();
        }
        else return BadRequest("File does not belong to this user");
    }
}