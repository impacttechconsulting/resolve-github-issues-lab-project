using System.Security.Cryptography;
using System.Text;
using ContosoShopEasy.Models;

namespace ContosoShopEasy.Security
{
    /// <summary>
    /// Provides secure card data handling including tokenization, masking, and card type detection.
    /// This class ensures PCI-DSS compliance by never storing full card numbers or CVV codes.
    /// </summary>
    public static class CardTokenizer
    {
        /// <summary>
        /// Generates a secure token for a card number. The token cannot be reversed to obtain the original card number.
        /// In a production environment, this would integrate with a payment processor's tokenization service.
        /// </summary>
        /// <param name="cardNumber">The full card number to tokenize</param>
        /// <returns>A secure token representing the card</returns>
        public static string GenerateToken(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return string.Empty;

            // Normalize card number (remove spaces and dashes)
            string normalizedCard = NormalizeCardNumber(cardNumber);

            // Generate a cryptographically secure token
            // In production, this would call a payment processor's tokenization API
            using (var sha256 = SHA256.Create())
            {
                // Add a unique salt for each token generation
                string saltedInput = $"{normalizedCard}_{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}";
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
                string hash = Convert.ToBase64String(hashBytes);
                
                // Format as a token with prefix for identification
                return $"tok_{hash.Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 24)}";
            }
        }

        /// <summary>
        /// Extracts the last 4 digits of a card number for display purposes.
        /// </summary>
        /// <param name="cardNumber">The full card number</param>
        /// <returns>Last 4 digits of the card number</returns>
        public static string GetLastFourDigits(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return string.Empty;

            string normalizedCard = NormalizeCardNumber(cardNumber);
            
            if (normalizedCard.Length < 4)
                return normalizedCard;

            return normalizedCard.Substring(normalizedCard.Length - 4);
        }

        /// <summary>
        /// Returns a masked version of the card number showing only the last 4 digits.
        /// </summary>
        /// <param name="cardNumber">The full card number</param>
        /// <returns>Masked card number (e.g., "**** **** **** 1234")</returns>
        public static string MaskCardNumber(string cardNumber)
        {
            string lastFour = GetLastFourDigits(cardNumber);
            
            if (string.IsNullOrEmpty(lastFour))
                return "****";

            return $"**** **** **** {lastFour}";
        }

        /// <summary>
        /// Detects the card type based on the card number prefix (BIN/IIN).
        /// </summary>
        /// <param name="cardNumber">The card number to analyze</param>
        /// <returns>The detected CardType</returns>
        public static CardType DetectCardType(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return CardType.Unknown;

            string normalizedCard = NormalizeCardNumber(cardNumber);

            if (normalizedCard.Length < 2)
                return CardType.Unknown;

            // American Express: starts with 34 or 37
            if (normalizedCard.StartsWith("34") || normalizedCard.StartsWith("37"))
                return CardType.AmericanExpress;

            // Visa: starts with 4
            if (normalizedCard.StartsWith("4"))
                return CardType.Visa;

            // Mastercard: starts with 51-55 or 2221-2720
            if (normalizedCard.Length >= 2)
            {
                int firstTwo = int.Parse(normalizedCard.Substring(0, 2));
                if (firstTwo >= 51 && firstTwo <= 55)
                    return CardType.Mastercard;

                if (normalizedCard.Length >= 4)
                {
                    int firstFour = int.Parse(normalizedCard.Substring(0, 4));
                    if (firstFour >= 2221 && firstFour <= 2720)
                        return CardType.Mastercard;
                }
            }

            // Discover: starts with 6011, 622126-622925, 644-649, or 65
            if (normalizedCard.StartsWith("6011") || normalizedCard.StartsWith("65"))
                return CardType.Discover;

            if (normalizedCard.Length >= 3)
            {
                int firstThree = int.Parse(normalizedCard.Substring(0, 3));
                if (firstThree >= 644 && firstThree <= 649)
                    return CardType.Discover;
            }

            if (normalizedCard.Length >= 6)
            {
                int firstSix = int.Parse(normalizedCard.Substring(0, 6));
                if (firstSix >= 622126 && firstSix <= 622925)
                    return CardType.Discover;
            }

            // Diners Club: starts with 300-305, 36, 38, or 39
            if (normalizedCard.StartsWith("36") || normalizedCard.StartsWith("38") || normalizedCard.StartsWith("39"))
                return CardType.DinersClub;

            if (normalizedCard.Length >= 3)
            {
                int firstThree = int.Parse(normalizedCard.Substring(0, 3));
                if (firstThree >= 300 && firstThree <= 305)
                    return CardType.DinersClub;
            }

            // JCB: starts with 3528-3589
            if (normalizedCard.Length >= 4)
            {
                int firstFour = int.Parse(normalizedCard.Substring(0, 4));
                if (firstFour >= 3528 && firstFour <= 3589)
                    return CardType.JCB;
            }

            return CardType.Unknown;
        }

        /// <summary>
        /// Normalizes a card number by removing spaces, dashes, and other non-digit characters.
        /// </summary>
        /// <param name="cardNumber">The card number to normalize</param>
        /// <returns>Normalized card number containing only digits</returns>
        public static string NormalizeCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return string.Empty;

            return new string(cardNumber.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Validates that a card number has a valid format (length and Luhn check).
        /// </summary>
        /// <param name="cardNumber">The card number to validate</param>
        /// <returns>True if the card number format is valid</returns>
        public static bool ValidateCardFormat(string cardNumber)
        {
            string normalizedCard = NormalizeCardNumber(cardNumber);

            // Check length (13-19 digits)
            if (normalizedCard.Length < 13 || normalizedCard.Length > 19)
                return false;

            // Luhn algorithm check
            return PassesLuhnCheck(normalizedCard);
        }

        /// <summary>
        /// Implements the Luhn algorithm to validate card numbers.
        /// </summary>
        private static bool PassesLuhnCheck(string cardNumber)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }
}
