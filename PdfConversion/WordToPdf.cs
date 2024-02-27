using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using PdfConversion.Models;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;

namespace PdfConversion;

public class WordToPdf(ILogger<WordToPdf> logger)
{
    private readonly ILogger<WordToPdf> _logger = logger;

    [Function("WordToPdf")]
    [OpenApiOperation()]
    [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(MultiPartFormDataModel), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(byte[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/text", bodyType: typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/text", bodyType: typeof(string))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger 'WordToPdf' function processed a request.");

            // check for word file is exists or not
            if (req.Form == null || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("Word file is missing. Please add and try again!");
            }

            // get the word file
            var wordFile = req.Form.Files[0];
            using var wordStream = new MemoryStream();
            await wordFile.CopyToAsync(wordStream);
            wordStream.Position = 0;

            // create an word document with docx extensions
            var word = new WordDocument(wordStream, FormatType.Docx);
            using var renderer = new DocIORenderer();
            // create the pdf file
            var pdf = renderer.ConvertToPDF(word);
            // close the word file
            word.Close();

            using var pdfStream = new MemoryStream();
            pdf.Save(pdfStream);
            pdf.Close();
            pdfStream.Position = 0;

            string contentType = "application/pdf";
            string filename = "document.pdf";

            req.HttpContext.Response.Headers.Append("Content-Disposition", $"attachment;{filename}");

            return new FileContentResult(pdfStream.ToArray(), contentType);
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
