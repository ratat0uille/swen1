using System.ComponentModel.DataAnnotations;

namespace MTCG.Models
{
    public class UserModel
    {
        [Required]
        [MinLength(3)]
        public string Name { get; set; }
    }
}
