using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Collections.Generic;

namespace DigitalTwin.Infrastructure.Filters
{
    /// <summary>
    /// Model validation filter for API endpoints
    /// </summary>
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                var errorResponse = new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors
                };

                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }
        }
    }

    /// <summary>
    /// SQL injection prevention filter
    /// </summary>
    public class SqlInjectionProtectionAttribute : ActionFilterAttribute
    {
        private static readonly string[] SqlPatterns = new[]
        {
            "--", ";--", "/\\*/", "\\*/", "@@", 
            "char", "nchar", "varchar", "nvarchar",
            "alter", "begin", "cast", "create", "cursor",
            "declare", "delete", "drop", "exec", "execute",
            "fetch", "insert", "kill", "select", "sys",
            "sysobjects", "syscolumns", "table", "update"
        };

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var suspiciousInputs = new List<string>();

            foreach (var parameter in context.ActionArguments)
            {
                if (parameter.Value == null) continue;

                var stringValue = parameter.Value.ToString();
                if (string.IsNullOrEmpty(stringValue)) continue;

                var lowerValue = stringValue.ToLowerInvariant();

                foreach (var pattern in SqlPatterns)
                {
                    if (lowerValue.Contains(pattern))
                    {
                        suspiciousInputs.Add($"{parameter.Key}: {stringValue}");
                        break;
                    }
                }
            }

            if (suspiciousInputs.Any())
            {
                var errorResponse = new
                {
                    success = false,
                    message = "Invalid input detected",
                    errors = new[] { "Input contains potentially dangerous content" }
                };

                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }
        }
    }

    /// <summary>
    /// XSS prevention filter
    /// </summary>
    public class XssProtectionAttribute : ActionFilterAttribute
    {
        private static readonly string[] XssPatterns = new[]
        {
            "<script", "</script>", "javascript:", "onload=", "onerror=",
            "onclick=", "onmouseover=", "onfocus=", "onblur=", "onchange=",
            "onsubmit=", "eval(", "expression(", "vbscript:", "data:text/html",
            "<iframe", "</iframe>", "<object", "</object>", "<embed", "</embed>"
        };

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var suspiciousInputs = new List<string>();

            foreach (var parameter in context.ActionArguments)
            {
                if (parameter.Value == null) continue;

                var stringValue = parameter.Value.ToString();
                if (string.IsNullOrEmpty(stringValue)) continue;

                var lowerValue = stringValue.ToLowerInvariant();

                foreach (var pattern in XssPatterns)
                {
                    if (lowerValue.Contains(pattern))
                    {
                        suspiciousInputs.Add($"{parameter.Key}: {stringValue}");
                        break;
                    }
                }
            }

            if (suspiciousInputs.Any())
            {
                var errorResponse = new
                {
                    success = false,
                    message = "Invalid input detected",
                    errors = new[] { "Input contains potentially dangerous content" }
                };

                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }
        }
    }
}