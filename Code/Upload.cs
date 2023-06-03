using System.ComponentModel.DataAnnotations;

namespace Saritasa;
//Upload: with file and text object, to control the file/text operations
public class Upload{
    //File or text
    [Required]
    public UploadType Type { get; set; }
    [Required]
    //File path
    public string FilePath { get; set; }
    //File ID
    [Key]
    public int? Id { get; set; }=null;
    [Required]
    public bool DeleteAfterDownload { get; set; }
}
public class File: Upload{

}
public class Text: Upload{
    public string Content {get; set; }
}
public enum UploadType{
    File,
    Text
}