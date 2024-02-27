using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace TextToPdf;

public class TextToPdf(ILogger<TextToPdf> logger)
{
    private readonly ILogger<TextToPdf> _logger = logger;

    [Function("TextToPdf")]
    [OpenApiOperation()]
    [OpenApiParameter(name: "Name", Required = true, In = ParameterLocation.Query, Type = typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/text", bodyType: typeof(string))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var name = req.Query["name"];

        return new OkObjectResult($"Hello {name}, Welcome to Azure TextToPdf Functions!");
    }
}
