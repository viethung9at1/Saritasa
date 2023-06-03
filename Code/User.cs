using System.ComponentModel.DataAnnotations;

namespace Saritasa;
public class User{
    UserType Type { get; set; }
}
public class RegularUser : User{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Key]
    public int? Id { get; set; }=null;
    public List<File> Files { get; set; } =new List<File>();
    public List<Text> Texts { get; set; } =new List<Text>();
}
public class AnonymousUser : User{

}
enum UserType{
    Anonymous,
    Regular
}