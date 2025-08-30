namespace Chapi.ApiSpecs.Infrastructure.Configuration;

public class ApiSpecsOptions
{
    public const string SectionName = "ApiSpecs";

    // Database related settings
    public string? Schema { get; set; } = null;

    // OpenAPI reader settings
    public int HttpClientTimeoutSeconds { get; set; } = 30;
    public string? HttpClientUserAgent { get; set; } = "chapi-openapi-reader/1.0";
}
