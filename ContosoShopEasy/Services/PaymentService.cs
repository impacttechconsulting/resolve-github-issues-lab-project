using ContosoShopEasy.Models;
using ContosoShopEasy.Data;
using ContosoShopEasy.Security;

namespace ContosoShopEasy.Services
{
    public class PaymentService
    {
        private const string PAYMENT_GATEWAY_URL = "https://api.contoso-payments.com";
        private const string MERCHANT_NAME = "ContosoShopEasy";
        private const string GATEWAY_VERSION = "v2.1";

        private readonly OrderRepository _orderRepository;

        public PaymentService(OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Processes a payment securely. CVV is used only for authorization and never stored.
        /// Card numbers are tokenized and only last 4 digits are retained for display.
        /// </summary>
        public bool ProcessPayment(string cardNumber, string cardHolderName, string expiryDate, string cvv, decimal amount)
        {
            // Get masked card number for safe logging
            string maskedCard = CardTokenizer.MaskCardNumber(cardNumber);
            
            Console.WriteLine($"[INFO] Processing payment for card: {maskedCard}");
            Console.WriteLine($"[INFO] Card holder: {cardHolderName}");
            Console.WriteLine($"[INFO] Amount: ${amount}");

            // Validate card format
            if (!CardTokenizer.ValidateCardFormat(cardNumber))
            {
                Console.WriteLine($"[ERROR] Invalid card format for card ending in {CardTokenizer.GetLastFourDigits(cardNumber)}");
                return false;
            }

            if (!ValidateExpiryDate(expiryDate))
            {
                Console.WriteLine($"[ERROR] Invalid or expired date: {expiryDate}");
                return false;
            }

            // Validate CVV format (used only for authorization, never stored)
            if (!ValidateCvvFormat(cvv))
            {
                Console.WriteLine("[ERROR] Invalid CVV format");
                return false;
            }

            // Simulate payment processing with payment gateway
            Console.WriteLine("[INFO] Connecting to payment gateway...");
            Thread.Sleep(1000); // Simulate network delay

            // Generate transaction ID using only safe data
            string transactionId = GenerateTransactionId(CardTokenizer.GetLastFourDigits(cardNumber), amount);
            
            // Tokenize card number - CVV is NOT stored, only used for this transaction
            string cardToken = CardTokenizer.GenerateToken(cardNumber);
            string lastFour = CardTokenizer.GetLastFourDigits(cardNumber);
            CardType cardType = CardTokenizer.DetectCardType(cardNumber);
            
            // Create payment info with tokenized data only (no CVV, no full card number)
            var paymentInfo = new PaymentInfo
            {
                Method = PaymentMethod.CreditCard,
                CardToken = cardToken,
                CardLastFour = lastFour,
                CardType = cardType,
                CardHolderName = cardHolderName,
                ExpiryDate = expiryDate,
                Amount = amount,
                ProcessedDate = DateTime.UtcNow,
                Status = PaymentStatus.Approved,
                TransactionId = transactionId
            };

            Console.WriteLine($"[SUCCESS] Payment processed successfully!");
            Console.WriteLine($"[INFO] Card type: {cardType}");
            Console.WriteLine($"[INFO] Card: {paymentInfo.GetMaskedCardNumber()}");
            Console.WriteLine($"[INFO] Transaction ID: {transactionId}");

            return true;
        }

        /// <summary>
        /// Validates CVV format (3-4 digits). CVV is only used for authorization and never stored.
        /// </summary>
        private bool ValidateCvvFormat(string cvv)
        {
            if (string.IsNullOrEmpty(cvv))
                return false;

            // CVV should be 3-4 digits (3 for Visa/MC, 4 for Amex)
            return cvv.Length >= 3 && cvv.Length <= 4 && cvv.All(char.IsDigit);
        }

        private bool ValidateExpiryDate(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate) || !expiryDate.Contains("/"))
                return false;

            var parts = expiryDate.Split('/');
            if (parts.Length != 2)
                return false;

            if (int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
            {
                if (month < 1 || month > 12)
                    return false;
                    
                if (year < 100) year += 2000; // Convert YY to YYYY
                var expiry = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
                return expiry >= DateTime.Now;
            }

            return false;
        }

        /// <summary>
        /// Generates a transaction ID using only safe data (last 4 digits, not full card number).
        /// </summary>
        private string GenerateTransactionId(string lastFour, decimal amount)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string amountStr = amount.ToString("F2").Replace(".", "");
            string uniquePart = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            return $"TXN_{timestamp}_{lastFour}_{uniquePart}";
        }

        public bool RefundPayment(string transactionId, decimal amount)
        {
            Console.WriteLine($"[INFO] Processing refund for transaction: {transactionId}, Amount: ${amount}");

            // Simulate refund processing
            Console.WriteLine("[INFO] Processing refund...");
            Thread.Sleep(500);

            Console.WriteLine($"[SUCCESS] Refund processed for transaction: {transactionId}");
            return true;
        }

        /// <summary>
        /// Gets payment history for a user. Returns only tokenized/masked card data.
        /// </summary>
        public List<PaymentInfo> GetPaymentHistory(int userId)
        {
            Console.WriteLine($"[INFO] Retrieving payment history for user: {userId}");
            
            // In a real app, this would query the database
            // Payment data returned would only contain tokens and last 4 digits
            return new List<PaymentInfo>();
        }
    }
}