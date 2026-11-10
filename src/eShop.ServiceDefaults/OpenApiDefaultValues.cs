using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace eShop.ServiceDefaults;

internal sealed class OpenApiDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        // remove any assumed media types not present in the api description
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            if (operation.Responses is null || !operation.Responses.TryGetValue(responseKey, out var response) || response?.Content is null)
            {
                continue;
            }

            foreach (var contentType in response.Content.Keys)
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
        {
            return;
        }

        // fix-up parameters with additional information from the api explorer that might
        // not have otherwise been used. this will most often happen for api version parameters
        // which are dynamically added, have no endpoint signature info, nor any xml comments.
        foreach (var parameter in operation.Parameters)
        {
            // OpenAPI 2 exposes these members as read-only on the interfaces.
            if (parameter is not OpenApiParameter openApiParameter)
            {
                continue;
            }

            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            openApiParameter.Description ??= description.ModelMetadata?.Description;

            if (openApiParameter.Schema is OpenApiSchema schema &&
                schema.Default == null &&
                description.DefaultValue != null &&
                description.DefaultValue is not DBNull &&
                description.ModelMetadata is ModelMetadata modelMetadata)
            {
                var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
                schema.Default = JsonNode.Parse(json);
            }

            openApiParameter.Required |= description.IsRequired;
        }
    }
}
