using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LegalSearch.Api.Middlewares
{

    /// <summary>
    /// Custom Swagger filter to enhance file upload descriptions in Swagger documentation.
    /// </summary>
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        /// <summary>
        /// Modifies the Swagger documentation for file upload operations.
        /// </summary>
        /// <param name="operation">The Swagger operation being processed.</param>
        /// <param name="context">The context for the Swagger operation.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                // Check if the parameter is of type IFormFile
                var paramType = context.ApiDescription.ParameterDescriptions
                    .FirstOrDefault(p => p.Name == parameter.Name)?.Type;

                if (paramType == typeof(IFormFile))
                {
                    // Update parameter description and properties for file upload
                    parameter.Description = "Upload a file.";
                    parameter.Required = true;
                    parameter.Schema = new OpenApiSchema { Type = "string", Format = "binary" };
                }
            }
        }
    }


}
