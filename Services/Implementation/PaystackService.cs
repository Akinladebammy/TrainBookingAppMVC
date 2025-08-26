using Microsoft.Extensions.Configuration;
using PayStack.Net;
using System.Threading.Tasks;

namespace TrainBookingAppMVC.Services
{
    public class PaystackService
    {
        private readonly PayStackApi _paystackApi;
        private readonly string _publicKey;

        public PaystackService(IConfiguration configuration)
        {
            var secretKey = configuration["Paystack:SecretKey"];
            _publicKey = configuration["Paystack:PublicKey"];
            _paystackApi = new PayStackApi(secretKey);
            Console.WriteLine($"PaystackService initialized. SecretKey: {secretKey?.Substring(0, 10)}..., PublicKey: {_publicKey}");
        }

        public async Task<(bool success, string reference, string authorizationUrl)> InitializePaymentAsync(string email, decimal amount, string callbackUrl)
        {
            try
            {
                Console.WriteLine($"Initializing payment: Email={email}, Amount={amount}, CallbackUrl={callbackUrl}");
                var request = new TransactionInitializeRequest
                {
                    Email = email,
                    AmountInKobo = (int)(amount * 100), // Paystack expects amount in kobo (NGN * 100)
                    CallbackUrl = callbackUrl
                };

                var response = await Task.Run(() => _paystackApi.Transactions.Initialize(request));
                Console.WriteLine($"Paystack Initialize response: Status={response.Status}, Message={response.Message}, AuthorizationUrl={response.Data?.AuthorizationUrl}, Reference={response.Data?.Reference}");

                if (response.Status)
                {
                    return (true, response.Data.Reference, response.Data.AuthorizationUrl);
                }
                return (false, null, $"Payment initialization failed: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Paystack Initialize exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"Payment initialization failed: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> VerifyPaymentAsync(string reference)
        {
            try
            {
                Console.WriteLine($"Verifying payment: Reference={reference}");
                var response = await Task.Run(() => _paystackApi.Transactions.Verify(reference));
                Console.WriteLine($"Paystack Verify response: Status={response.Status}, Message={response.Message}, PaymentStatus={response.Data?.Status}");

                if (response.Status && response.Data.Status == "success")
                {
                    return (true, "Payment verified successfully.");
                }
                return (false, $"Payment verification failed: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Paystack Verify exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Verification failed: {ex.Message}");
            }
        }

        public string GetPublicKey()
        {
            return _publicKey;
        }
    }
}