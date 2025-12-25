using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.Models
{
    public class AIModel
    {
        [Key]
        public int ModelID { get; set; }

        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public string ModelVersion { get; set; }
        public string ModelFilePath { get; set; }
        public double Accuracy { get; set; }
        public DateTime DateTrained { get; set; }
    }
}
