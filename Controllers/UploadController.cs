using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UploadController: ControllerBase{
    private readonly UserDataContext _context;
    public UploadController(UserDataContext context){
        _context=context;
    }
    [HttpPost(template: "uploadFile")]
    public IActionResult Post(IFormFile file, int id, bool deleteAfterDownload=false){
        if(file.Length==0) return BadRequest("File is empty");
        if(id==null) return BadRequest("User is not logged in");
        if(!UserController.LoggedUser.Contains(id)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.Include(u => u.Uploads).FirstOrDefault(u => u.Id==id);
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
        return Ok(uniqueFilePath);
    }
    [HttpPost(template: "uploadText")]
    public IActionResult Post([FromForm] string content, int userId, bool deleteAfterDownload=false){
        if(content==null) return BadRequest("Content is empty");
        if(userId==null) return BadRequest("User is not logged in");
        if(!UserController.LoggedUser.Contains(userId)) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==userId);
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
        return Ok(uniqueFilePath);
    }
    [HttpPost("downloadFile")]
    public IActionResult Download([FromForm] string filePath){
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileBytes=System.IO.File.ReadAllBytes(filePath);
        var fileName=Path.GetFileName(filePath);
        if(_context.Uploads.FirstOrDefault(u => u.FilePath==filePath).DeleteAfterDownload){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(_context.Uploads.FirstOrDefault(u => u.FilePath==filePath));
            _context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(_context.Uploads.FirstOrDefault(u => u.FilePath==filePath))).Uploads.Remove(_context.Uploads.FirstOrDefault(u => u.FilePath==filePath));
            _context.SaveChanges();
        }
        return File(fileBytes, "application/force-download", fileName);
    }
    [HttpPost("downloadText")]
    public IActionResult DownloadText([FromForm] string filePath){
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileBytes=System.IO.File.ReadAllBytes(filePath);
        var fileName=Path.GetFileName(filePath);
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==filePath);
        if(fileObject.DeleteAfterDownload){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(fileObject);
            _context.RegularUsers.FirstOrDefault(u => u.Uploads.Contains(fileObject)).Uploads.Remove(fileObject);
            _context.SaveChanges();
        }
        return File(fileBytes, "application/force-download", fileName);
    }
    [HttpPost("getUploads")]
    public IActionResult Get(int id){
        if(id==null) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.Include(u=> u.Uploads).FirstOrDefault(u => u.Id==id);
        return Ok(user.Uploads);
    }
    [HttpPost("deleteAll")]
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
    [HttpPost("deleteDatabase")]
    public IActionResult DeleteDatabase(){
        _context.Database.EnsureDeleted();
        return Ok();
    }
    [HttpPost("deleteFile")]
    public IActionResult DeleteFile([FromForm] string filePath, int userId){
        if(filePath==null) return BadRequest("File path is empty");
        if(!System.IO.File.Exists(filePath)) return BadRequest("File does not exist");
        var fileObject=_context.Uploads.FirstOrDefault(u => u.FilePath==filePath);
        var user=_context.RegularUsers.Include(u => u.Uploads).FirstOrDefault(u => u.Id==userId);
        if(user.Uploads.Contains(fileObject)){
            System.IO.File.Delete(filePath);
            _context.Uploads.Remove(fileObject);
            user.Uploads.Remove(fileObject);
            _context.SaveChanges();
            return Ok();
        }
        else return BadRequest("File does not belong to this user");
    }
}