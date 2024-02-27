using System.ComponentModel.DataAnnotations;

namespace PdfConversion.Models;

public class MultiPartFormDataModel
{
    [Required]
    public byte[] FileUpload { get; set; } = default!;
}
