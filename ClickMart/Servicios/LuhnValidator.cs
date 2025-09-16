namespace ClickMart.Servicios
{
    public static class LuhnValidator
    {
        public static bool IsValid(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.Length < 12) return false; // umbral razonable


            int sum = 0;
            bool dbl = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i] - '0';
                if (dbl)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                dbl = !dbl;
            }
            return sum % 10 == 0;
        }


        public static string Last4(string input)
        {
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? digits[^4..] : digits;
        }
    }
}