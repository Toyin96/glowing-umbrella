using LegalSearch.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.AspNetCore.Routing;

namespace LegalSearch.Test.Filters
{
    public class RequestValidationFilterTests
    {
        [Fact]
        public void RequestValidationFilter_PassInvalidActionExecutingContext()
        {
            var modelStateDictionary = new ModelStateDictionary(2);
            modelStateDictionary.AddModelError("name", "invalid model");

            var mockActionContext = new ActionContext(
                Mock.Of<HttpContext>(),
                Mock.Of<RouteData>(),
                Mock.Of<ActionDescriptor>(),
                modelStateDictionary
            );

            var mockActionExecutingContext = new ActionExecutingContext(mockActionContext, new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                Mock.Of<Controller>());

            var mockActionExecutedContext = new ActionExecutedContext(mockActionContext, new List<IFilterMetadata>(), Mock.Of<Controller>());

            var validationActionFilter = new RequestValidationFilter();

            validationActionFilter.OnActionExecuting(mockActionExecutingContext);
            validationActionFilter.OnActionExecuted(mockActionExecutedContext);

            Assert.IsType<BadRequestObjectResult>(mockActionExecutingContext.Result);
        }
    }
}
