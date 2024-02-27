using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using PdfConversion.Models;
using Syncfusion.Pdf;
using Syncfusion.Presentation;
using Syncfusion.PresentationRenderer;

namespace PdfConversion;

public class PowerPointToPdf(ILogger<PowerPointToPdf> logger)
{
    private readonly ILogger<PowerPointToPdf> _logger = logger;

    [Function("PowerPointToPdf")]
    [OpenApiOperation()]
    [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(MultiPartFormDataModel), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(byte[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/text", bodyType: typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/text", bodyType: typeof(string))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger PowerPointToPdf function processed a request.");

            // check for powerPoint file is exists or not
            if (req.Form == null || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("PowerPoint file is missing. Please add and try again!");
            }

            // get the powerPoint file
            var powerPointFile = req.Form.Files[0];
            using var powerPointStream = new MemoryStream();
            await powerPointFile.CopyToAsync(powerPointStream);
            powerPointStream.Position = 0;

            using IPresentation presentation = Presentation.Open(powerPointStream);
            var settings = new PresentationToPdfConverterSettings()
            {
                ShowHiddenSlides = true,
            };
            PdfDocument pdfDocument = PresentationToPdfConverter.Convert(presentation, settings);

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
