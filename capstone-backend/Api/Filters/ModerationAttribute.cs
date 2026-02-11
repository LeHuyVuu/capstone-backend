using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Concurrent;
using System.Reflection;

namespace capstone_backend.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ModerationAttribute : ActionFilterAttribute
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propsCache = new();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var moderationService = context.HttpContext.RequestServices.GetService<IModerationService>();
            if (moderationService == null) return;

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                    continue;

                if (argument is string strValue)
                {
                    if (!Validate(context, moderationService, strValue, "DirectParam")) 
                        return;
                }
                else
                {
                    var type = argument.GetType();

                    var props = _propsCache.GetOrAdd(type, t =>
                        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.PropertyType == typeof(string) && p.CanRead));

                    foreach (var prop in props)
                    {
                        var value = (string?)prop.GetValue(argument);
                        if (!Validate(context, moderationService, value, prop.Name)) 
                            return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        private bool Validate(ActionExecutingContext context, IModerationService service, string? content, string fieldName)
        {
            if (string.IsNullOrEmpty(content)) 
                return true;

            var (isValid, message) = service.CheckContent(content);
            if (!isValid)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = 400,
                    Error = "Content Moderation Failed",
                    Message = message,
                    Field = fieldName
                });
                return false;
            }
            return true;
        }
    }
}
