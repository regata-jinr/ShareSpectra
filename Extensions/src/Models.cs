using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Extensions
{

    [Table("sharedspectra")]
    public class SharedSpectra
    {
        public string fileS     { get; set; }
        public string token     { get; set; }
    }
}
