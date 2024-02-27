using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using PdfConversion.Models;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;

namespace PdfConversion;

public class ExcelToPdf(ILogger<ExcelToPdf> logger)
{
    private readonly ILogger<ExcelToPdf> _logger = logger;

    [Function("ExcelToPdf")]
    [OpenApiOperation()]
    [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(MultiPartFormDataModel), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(byte[]))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/text", bodyType: typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/text", bodyType: typeof(string))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger ExcelToPdf function processed a request.");

            // check for excel file is exists or not
            if (req.Form == null || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("Excel file is missing. Please add and try again!");
            }

            // get the excel file
            var textFile = req.Form.Files[0];
            using var excelStream = new MemoryStream();
            await textFile.CopyToAsync(excelStream);
            excelStream.Position = 0;

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2013;

            var workbook = application.Workbooks.Open(excelStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);
            var renderer = new XlsIORenderer();
            var pdf = renderer.ConvertToPDF(workbook);

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
