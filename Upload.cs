using System.ComponentModel.DataAnnotations;

namespace Saritasa;
public class Upload{
    [Required]
    public UploadType Type { get; set; }
    [Required]
    public string FilePath { get; set; }
    [Key]
    public int? Id { get; set; }=null;
    [Required]
    public bool DeleteAfterDownload { get; set; }
}
public class File: Upload{

}
public class Text: Upload{

}
public enum UploadType{
    File,
    Text
}