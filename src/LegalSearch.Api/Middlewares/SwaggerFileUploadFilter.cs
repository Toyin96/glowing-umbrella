using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace LegalSearch.Api.Middlewares
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                var paramType = context.ApiDescription.ParameterDescriptions
                    .FirstOrDefault(p => p.Name == parameter.Name)?.Type;

                if (paramType == typeof(Microsoft.AspNetCore.Http.IFormFile))
                {
                    parameter.Description = "Upload a file.";
                    parameter.Required = true;
                    parameter.Schema = new OpenApiSchema { Type = "string", Format = "binary" };
                }
            }
        }
    }

}
