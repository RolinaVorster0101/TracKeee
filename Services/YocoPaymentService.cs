using System.Text;
using System.Text.Json;

namespace TracKeee.Services
{
    public class YocoPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<YocoPaymentService> _logger;

        public YocoPaymentService(IHttpClientFactory httpClientFactory, ILogger<YocoPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<YocoCheckoutResponse?> CreateCheckout(string secretKey, decimal amount, string invoiceNumber, string successUrl, string cancelUrl, string failureUrl)
        {
            var amountInCents = (int)(amount * 100);

            var requestBody = new
            {
                amount = amountInCents,
                currency = "ZAR",
                successUrl = successUrl,
                cancelUrl = cancelUrl,
                failureUrl = failureUrl,
                metadata = new { invoiceNumber = invoiceNumber }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("https://payments.yoco.com/api/checkouts", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<YocoCheckoutResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }
                else
                {
                    _logger.LogError($"Yoco checkout failed: {response.StatusCode} - {responseBody}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Yoco checkout");
                return null;
            }
        }
    }

    public class YocoCheckoutResponse
    {
        public string? Id { get; set; }
        public string? RedirectUrl { get; set; }
    }
}