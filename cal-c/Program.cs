using System;
using System.Diagnostics;
using cal_c.Enums;
using cal_c.Exception;

namespace cal_c
{
    internal static class Program
    {
        public static event EventHandler<char> KeyPressed;

        private static void Main()
        {
            Console.CursorVisible = false;
            var calcEngine = new CalcEngine();
            var display = string.Empty;

            Console.WriteLine("Cal-C");
            Console.WriteLine("Controls: C - clear, Backspace - erase, Enter - calculate, Esc - close");

            cycle:
            var firstSign = calcEngine.FirstSign == 1 ? string.Empty : "-";
            var firstNumber = calcEngine.First;
            var operation = GetOperationString(calcEngine.Operation);
            var secondSign = calcEngine.SecondSign == 1 ? string.Empty : "-";
            var secondNumber = calcEngine.Second;
            var result = FormatResult(calcEngine.Result);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', display.Length)); // clear previous display

            display = $"{firstSign}{firstNumber} {operation} {secondSign}{secondNumber}{result}";
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(display);

            var cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Escape)
                return;
            
            var c = cki.Key switch
            {
                ConsoleKey.Backspace => 'e',
                ConsoleKey.Enter => '=',
                _ => cki.KeyChar
            };

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
                resultString += result.IsValid ? result.Value.ToString("G") : result.Message;
            }

            return resultString;
        }

        private static string GetOperationString(MathOperation mathOperation)
        {
            return mathOperation switch
            {
                MathOperation.None => string.Empty,
                MathOperation.Add => "+",
                MathOperation.Substract => "-",
                MathOperation.Multiply => "*",
                MathOperation.Divide => "/",
                _ => throw ExceptionFactory.CreateEnumException(mathOperation, nameof(mathOperation))
            };
        }
    }
}