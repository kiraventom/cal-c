using System;
using System.Diagnostics;
using System.Globalization;

namespace cal_c
{
    internal static class Program
    {
        // TODO: Fix zeros after decimal point
        public static event EventHandler<char> KeyPressed;

        private static void Main()
        {
            Console.CursorVisible = false;
            var calcEngine = new CalcEngine();

            cycle:
            Console.Clear();
            Console.WriteLine("C to clear, Enter to calculate, Esc to close");

            var firstSign = calcEngine.FirstSign == 1 ? string.Empty : "-";
            var firstNumber = FormatNumber(calcEngine.First, calcEngine.FirstHasDecimalPoint);

            var operation = GetOperationString(calcEngine.Operation);

            var secondSign = calcEngine.SecondSign == 1 ? string.Empty : "-";
            var secondNumber = FormatNumber(calcEngine.Second, calcEngine.SecondHasDecimalPoint);

            var result = FormatResult(calcEngine.Result);

            Console.WriteLine($"{firstSign}{firstNumber} {operation} {secondSign}{secondNumber}{result}");

            var cki = Console.ReadKey(true);
            var c = cki.Key switch
            {
                ConsoleKey.Backspace => 'e',
                ConsoleKey.Enter => '=',
                ConsoleKey.Escape => '\0',
                _ => cki.KeyChar
            };

            if (c == '\0')
                return;

            Debug.Assert(KeyPressed != null, nameof(KeyPressed) + " != null");
            KeyPressed.Invoke(null, c);
            goto cycle;
        }

        private static string FormatResult(Result result)
        {
            string resultString;
            if (result is null)
                resultString = string.Empty;
            else
            {
                resultString = " = ";
                if (result.IsValid)
                    resultString += result.Value.ToString("G");
                else
                    resultString += result.Message;
            }

            return resultString;
        }

        private static string FormatNumber(double? number, bool hasDecimal)
        {
            string numberString;
            if (number is null)
            {
                numberString = string.Empty;
            }
            else
            {
                numberString = number.Value.ToString("G");
                if (hasDecimal && number.Value % 1 == 0) // has point, actually integer
                {
                    numberString += CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                }
            }

            return numberString;
        }

        private static string GetOperationString(CalcEngine.MathOperation mo)
        {
            return mo switch
            {
                CalcEngine.MathOperation.None => string.Empty,
                CalcEngine.MathOperation.Add => "+",
                CalcEngine.MathOperation.Substract => "-",
                CalcEngine.MathOperation.Multiply => "*",
                CalcEngine.MathOperation.Divide => "/",
                _ => throw new ArgumentOutOfRangeException(nameof(mo), mo, null)
            };
        }
    }
}