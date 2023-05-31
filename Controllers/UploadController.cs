using System.Text;
using Microsoft.AspNetCore.Mvc;
namespace Saritasa.Controllers;
[ApiController]
[Route("[controller]")]
public class UploadController: ControllerBase{
    private readonly UserDataContext _context;
    public UploadController(UserDataContext context){
        _context=context;
    }
    [HttpPost(template: "uploadFile")]
    public IActionResult Post(IFormFile file, int id){
        if(file.Length==0) return BadRequest("File is empty");
        if(id==null) return BadRequest("User is not logged in");
        var user=_context.RegularUsers.FirstOrDefault(u => u.Id==id);
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
            DeleteAfterDownload=false,
            Type=Saritasa.UploadType.File
        };
        user.Uploads.Add(upload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        return Ok(uniqueFilePath);
    }
    [HttpPost(template: "uploadText")]
    public IActionResult Post([FromForm] string content, int userId){
        if(content==null) return BadRequest("Content is empty");
        if(userId==null) return BadRequest("User is not logged in");
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
            DeleteAfterDownload=false,
            Type=Saritasa.UploadType.Text
        };
        user.Uploads.Add(upload);
        _context.RegularUsers.Update(user);
        _context.SaveChanges();
        return Ok(uniqueFilePath);
    }
}