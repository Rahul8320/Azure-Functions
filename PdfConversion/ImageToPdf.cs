using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using PdfConversion.Models;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace PdfConversion;

public class ImageToPdf(ILogger<ImageToPdf> logger)
{
    private readonly ILogger<ImageToPdf> _logger = logger;

    [Function("ImageToPdf")]
    [OpenApiOperation()]
    [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(MultiPartFormDataModel), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(byte[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/text", bodyType: typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/text", bodyType: typeof(string))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger ImageToPdf function processed a request.");

            // check for image file is exists or not
            if (req.Form == null || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("Image file is missing. Please add and try again!");
            }

            // get the image file
            var imageFile = req.Form.Files[0];
            using var imageStream = new MemoryStream();
            await imageFile.CopyToAsync(imageStream);
            imageStream.Position = 0;

            using var pdfDocument = new PdfDocument();
            PdfPage page = pdfDocument.Pages.Add();
            SizeF pageSize = page.GetClientSize();

            using var image = new PdfBitmap(imageStream);
            page.Graphics.DrawImage(image, new RectangleF(0, 0, pageSize.Width, pageSize.Height));

            using var outputStream = new MemoryStream();
            pdfDocument.Save(outputStream);
            pdfDocument.Close();
            outputStream.Position = 0;

            string contentType = "application/pdf";
            string filename = "document.pdf";

            req.HttpContext.Response.Headers.Append("Content-Disposition", $"attachment;{filename}");

            return new FileContentResult(outputStream.ToArray(), contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred!");
            return new ContentResult()
            {
                Content = "An error occurred: " + ex.Message,
                ContentType = "application/text",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
