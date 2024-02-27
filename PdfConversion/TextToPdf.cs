using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace PdfConversion;

public class TextToPdf(ILogger<TextToPdf> logger)
{
    private readonly ILogger<TextToPdf> _logger = logger;

    [Function("TextToPdf")]
    [OpenApiOperation()]
    [OpenApiParameter(name: "Text", Required = true, In = ParameterLocation.Query, Type = typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(byte[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/text", bodyType: typeof(string))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger 'TextToPdf' function processed a request.");

        // Get the query parameter
        var text = req.Query["text"];

        // check if text is exists or not
        if (string.IsNullOrEmpty(text))
        {
            return new BadRequestObjectResult("The required query parameter text cannot be null or empty!");
        }

        // Create the pdf document
        using PdfDocument document = new();
        PdfPage page = document.Pages.Add();

        PdfGraphics graphics = page.Graphics;

        graphics.DrawString(text!,
            new PdfStandardFont(PdfFontFamily.Helvetica, 20),
            PdfBrushes.Black,
            new Syncfusion.Drawing.PointF(0, 0));

        using MemoryStream outputStream = new();
        document.Save(outputStream);
        outputStream.Position = 0;
        document.Dispose();

        string contentType = "application/pdf";
        string fileName = "document.pdf";

        req.HttpContext.Response.Headers.Append("Content-Disposition", $"attachment;{fileName}");

        return new FileContentResult(outputStream.ToArray(), contentType);
    }
}
