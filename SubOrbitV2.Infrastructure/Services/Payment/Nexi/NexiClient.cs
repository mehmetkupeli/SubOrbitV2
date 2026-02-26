using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Common.Models.Payment;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SubOrbitV2.Infrastructure.Services.Payment.Nexi;

public class NexiClient : INexiClient
{
    private readonly HttpClient _httpClient;
    private readonly IProjectContext _projectContextService;
    private readonly ILogger<NexiClient> _logger;
    private readonly NexiSettings _nexiSettings;
    private readonly IEncryptionService _encryptionService;

    public NexiClient(
        HttpClient httpClient,
        IProjectContext projectContextService,
        ILogger<NexiClient> logger,
        IOptions<NexiSettings> nexiSettings,
        IEncryptionService encryptionService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _projectContextService = projectContextService ?? throw new ArgumentNullException(nameof(projectContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));

        _nexiSettings = nexiSettings?.Value ?? throw new ArgumentNullException(nameof(nexiSettings));
        _httpClient.BaseAddress = new Uri(_nexiSettings.BaseUrl);
    }

    public async Task<NexiPaymentResponse?> InitializePaymentAsync(NexiOrderItemDto orderItem)
    {
        PrepareHttpClient();

        var webhookUrl = $"{_nexiSettings.PublicApiUrl}/nexi?projectId={_projectContextService.ProjectId}&subId={orderItem.SubscriptionReference}";

        var requestBody = new CreatePaymentRequest(
            new NexiCheckout("HostedPaymentPage", orderItem.ReturnUrl, orderItem.TermsUrl, true, true),
            new NexiOrder(
                new[]
                {
                    new NexiOrderItem(
                        orderItem.NexiReference,
                        orderItem.Name,
                        orderItem.Quantity,
                        orderItem.Unit,
                        orderItem.UnitPrice,
                        orderItem.TaxRate,
                        orderItem.TaxAmount,
                        orderItem.GrossTotalAmount,
                        orderItem.NetTotalAmount
                    )
                },
                orderItem.GrossTotalAmount,
                orderItem.Currency,
                orderItem.SubscriptionReference
            ),
            new NexiNotification(new[]
            {
                new NexiWebhook("payment.checkout.completed", webhookUrl, "webhook-guvenlik-tokeni")
            }),
            new NexiSubscriptionRequestData(
                Interval: 0,
                EndDate: GetSubscriptionEndDate())
        );

        return await PostAsync<CreatePaymentRequest, NexiPaymentResponse>("payments", requestBody, nameof(InitializePaymentAsync));
    }

    public async Task<NexiPaymentDetailsResponse?> GetPaymentDetailsAsync(string paymentId)
    {
        PrepareHttpClient();
        return await GetAsync<NexiPaymentDetailsResponse>($"payments/{paymentId}", nameof(GetPaymentDetailsAsync));
    }

    /// <summary>
    /// Toplu ödeme işlemini başlatır.
    /// </summary>
    public async Task<string?> BulkChargeSubscriptionsAsync(IEnumerable<BulkSubscriptionItem> items, Guid projectId)
    {
        PrepareHttpClient();

        var requestBody = new BulkChargeRequest(
            ExternalBulkChargeId: Guid.NewGuid().ToString(),
            Notifications: null,
            Subscriptions: items
        );

        var result = await PostAsync<BulkChargeRequest, BulkChargeResponse>("subscriptions/charges", requestBody, nameof(BulkChargeSubscriptionsAsync));
        return result?.BulkId;
    }

    /// <summary>
    /// Toplu işlemin sonucunu sorgular.
    /// </summary>
    public async Task<BulkStatusResponse?> RetrieveBulkChargeStatusAsync(string bulkId, int pageNumber = 1)
    {
        PrepareHttpClient();
        return await GetAsync<BulkStatusResponse>($"subscriptions/charges/{bulkId}?pageNumber={pageNumber}", nameof(RetrieveBulkChargeStatusAsync));
    }

    /// <summary>
    /// Kullanıcının kartını güncellemesi için 0 tutarlı bir oturum açar.
    /// </summary>
    public async Task<NexiPaymentResponse?> CreateCardUpdateSessionAsync(string subscriptionId, string returnUrl, string termsUrl)
    {
        PrepareHttpClient();

        var requestBody = new CreatePaymentRequest(
            new NexiCheckout(
                IntegrationType: "HostedPaymentPage",
                ReturnUrl: returnUrl,
                TermsUrl: termsUrl,
                Charge: false, // Yorum satırındaki kurala uygun olarak düzeltildi.
                MerchantHandlesConsumerData: true
            ),
            new NexiOrder(
                Items: new[]
                {
                    new NexiOrderItem(
                        Reference: "card-update",
                        Name: "Kart Guncelleme",
                        Quantity: 1,
                        Unit: "pcs",
                        UnitPrice: 0,
                        TaxRate: 0,
                        TaxAmount: 0,
                        GrossTotalAmount: 0,
                        NetTotalAmount: 0
                    )
                },
                Amount: 0,
                Currency: "DKK",
                Reference: Guid.NewGuid().ToString()
            ),
            Notifications: null,
            Subscription: new NexiSubscriptionRequestData(
                SubscriptionId: subscriptionId,
                Interval: 0,
                EndDate: GetSubscriptionEndDate()
            )
        );

        return await PostAsync<CreatePaymentRequest, NexiPaymentResponse>("payments", requestBody, nameof(CreateCardUpdateSessionAsync));
    }

    public async Task<(bool Success, string? ChargeId)> ChargeSubscriptionAsync(string nexiSubscriptionId, NexiOrderItem orderItem, string currency, Guid myReference)
    {
        PrepareHttpClient();

        var requestBody = new
        {
            order = new NexiOrder(
                Items: new[] { orderItem },
                Amount: (int)orderItem.GrossTotalAmount, // Kuruş
                Currency: currency,
                Reference: myReference.ToString()
            ),
            notifications = (object?)null
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"subscriptions/{nexiSubscriptionId}/charges", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Nexi Single Charge Error: {Error}", error);
                return (false, null);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (result.TryGetProperty("chargeId", out var chargeIdProp))
            {
                return (true, chargeIdProp.GetString());
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Single Charge Exception");
            return (false, null);
        }
    }

    // --- PRIVATE HELPER METHODS ---

    /// <summary>
    /// Her istek öncesi mevcut proje konfigürasyonunu alır ve Authorization header'ını ayarlar.
    /// </summary>
    private void PrepareHttpClient()
    {
        var config = GetCurrentProjectConfig();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(config.SecretKey);
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest requestBody, string methodName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Nexi Error in {MethodName} ({StatusCode}): {Error}", methodName, response.StatusCode, errorContent);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nexi API Connection Error in {MethodName}", methodName);
            return default;
        }
    }

    private async Task<TResponse?> GetAsync<TResponse>(string url, string methodName)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Nexi Error in {MethodName} ({StatusCode}): {Error}", methodName, response.StatusCode, error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nexi API Connection Error in {MethodName}", methodName);
            return default;
        }
    }

    private BillingConfig GetCurrentProjectConfig()
    {
        var project = _projectContextService.CurrentProject;

        if (project == null)
            throw new UnauthorizedAccessException("Project bağlamı bulunamadı. API Key gönderdiniz mi?");

        if (string.IsNullOrEmpty(project.EncryptedBillingConfig))
            throw new InvalidOperationException($"'{project.Name}' firması için ödeme ayarları girilmemiş.");

        try
        {
            var json = _encryptionService.Decrypt(project.EncryptedBillingConfig);
            var config = JsonSerializer.Deserialize<BillingConfig>(json);

            if (string.IsNullOrEmpty(config?.SecretKey))
                throw new Exception("Config okundu ama SecretKey boş.");

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project config hatası: {ProjectId}", project.Id);
            throw new InvalidOperationException("Ödeme ayarları çözülemedi.");
        }
    }

    private static string GetSubscriptionEndDate() => DateTime.Today.AddYears(10).ToString("yyyy-MM-dd");
}