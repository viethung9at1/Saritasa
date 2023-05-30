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
}
public class AnonymousUser : User{

}
enum UserType{
    Anonymous,
    Regular
}