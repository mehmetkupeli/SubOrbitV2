using FluentValidation;
using System.Net;
using System.Text.Json;

namespace SubOrbitV2.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Varsayılan hata: 500 Internal Server Error
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var errors = new List<string>();
        var message = "Sunucuda beklenmedik bir hata oluştu.";

        #region Hata Tipine Göre Formatlama
        switch (exception)
        {
            case ValidationException validationException:
                // FluentValidation hatası yakalandığında 400 dönüyoruz
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Validasyon hataları oluştu.";
                errors.AddRange(validationException.Errors.Select(x => x.ErrorMessage));
                break;

            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = "Bu işlem için yetkiniz yok.";
                break;

            default:
                // Diğer tüm hatalarda (Development ortamındaysak detayı ekleyelim)
                if (_env.IsDevelopment())
                {
                    errors.Add(exception.Message);
                    errors.Add(exception.StackTrace ?? string.Empty);
                }
                break;
        }
        #endregion

        context.Response.StatusCode = statusCode;

        // Bizim Result<T> yapımıza uygun anonim bir obje oluşturuyoruz
        // Result record'u generic olduğu için burada aynı JSON yapısını simüle ediyoruz
        var response = new
        {
            IsSuccess = false,
            Data = (object?)null,
            Errors = errors,
            Message = message
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}