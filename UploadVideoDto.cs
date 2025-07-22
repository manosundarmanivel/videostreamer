
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TrainingVideoAPI.Models
{
    public class UploadVideoDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }
    }
}
