
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CurrencyConverterAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyConverterController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CurrencyConverterController> _logger;

        public CurrencyConverterController(IConfiguration configuration, ILogger<CurrencyConverterController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("convert")]
        public ActionResult ConvertCurrency([FromQuery] string sourceCurrency, [FromQuery] string targetCurrency, [FromQuery] decimal amount)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(sourceCurrency) || string.IsNullOrEmpty(targetCurrency) || amount <= 0)
                {
                    _logger.LogError("Invalid input parameters.");
                    return BadRequest("Invalid input parameters.");
                }

                // Retrieve exchange rate from configuration or fallback to default values
                decimal exchangeRate = GetExchangeRate(sourceCurrency, targetCurrency);

                decimal convertedAmount = amount * exchangeRate;

                // Log successful conversion
                _logger.LogInformation($"Conversion from {sourceCurrency} to {targetCurrency} successful. Amount: {amount}, Exchange Rate: {exchangeRate}, Converted Amount: {convertedAmount}");

                // Framing the Json response object
                var responseObject = new
                {
                    exchangeRate,
                    convertedAmount
                };

                // Return the response as JSON
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError($"Error during currency conversion: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private decimal GetExchangeRate(string sourceCurrency, string targetCurrency)
        {
            // Get exchange rate from configuration, fallback to default values if not found
            string exchangeRateKey = $"{sourceCurrency}_TO_{targetCurrency}";
            decimal exchangeRate = _configuration.GetValue<decimal>(exchangeRateKey, GetDefaultExchangeRate(exchangeRateKey));

            // Log the exchange rate
            _logger.LogInformation($"Exchange Rate for {sourceCurrency} to {targetCurrency}: {exchangeRate}");

            return exchangeRate;
        }

        private Dictionary<string, decimal> ReadExchangeRatesFromFile()
        {
            var filePath = "exchangeRates.json";
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string jsonContent = System.IO.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, decimal>>(jsonContent);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Error reading exchange rates from file: {ex.Message}");
                return null;
            }
            
        }



        private decimal GetDefaultExchangeRate(string exchangeRateKey)
        {
            var defaultExchangeRates = ReadExchangeRatesFromFile();

            if (defaultExchangeRates != null && defaultExchangeRates.TryGetValue(exchangeRateKey, out var rate))
            {
                return rate;
            }
            _logger.LogError($"Exchange rate for {exchangeRateKey} not found in the file.");

            return 0.0m; // Default to 1.0 if not found
        }

    }
}
