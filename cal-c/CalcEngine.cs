using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace cal_c
{
    class CalcEngine
    {
        private const char DecimalPoint = '.';

        public CalcEngine()
        {
            _firstNumberBuilder = new StringBuilder();
            _secondNumberBuilder = new StringBuilder();
            _currentState = State.WaitingFirst;
            Operation = MathOperation.None;
            Program.KeyPressed += OnKeyPressed;
        }
        
        public double? First { get; private set; }
        public bool FirstHasDecimalPoint { get; private set; }
        public int FirstSign { get; private set; } = 1;
        public MathOperation Operation { get; private set; }
        public double? Second { get; private set; }
        public bool SecondHasDecimalPoint { get; private set; }
        public int SecondSign { get; private set; } = 1;
        public Result Result { get; private set; }
        
        private State _currentState;
        private readonly StringBuilder _firstNumberBuilder;
        private readonly StringBuilder _secondNumberBuilder;

        private void OnKeyPressed(object sender, char c)
        {
            var inputType = ProcessInput(c);
            Action action = inputType switch
            {
                InputType.Digit => () => ProcessDigit(c),
                InputType.Operation => () => ProcessOperation(c),
                InputType.Sign => ProcessSign,
                InputType.DecimalPoint => ProcessDecimalPoint,
                InputType.Erase => ProcessEraseCommand,
                InputType.Clear => ProcessClearCommand,
                InputType.Calculate => ProcessCalculateCommand,
                InputType.Incorrect or InputType.None => () => { },
                _ => throw new ArgumentOutOfRangeException(nameof(inputType), inputType, "Unknown input type")
            };
            
            action.Invoke();
        }

        private InputType ProcessInput(char c)
        {
            if (int.TryParse(c.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                return InputType.Digit;

            return c switch
            {
                ',' or '.' => InputType.DecimalPoint,
                'e' => InputType.Erase,
                'c' => InputType.Clear,
                '=' => InputType.Calculate,
                '+' or '*' or '/' => InputType.Operation,
                '-' => _currentState switch
                {
                    State.WaitingFirst or State.WaitingSecond => InputType.Sign,
                    State.EnteringFirst => InputType.Operation,
                    _ => InputType.Incorrect
                },
                _ => InputType.Incorrect
            };
        }

        private void ProcessDigit(char c)
        {
            if (_currentState is State.ShowingResult)
                return;

            if (_currentState is State.WaitingFirst)
                _currentState = State.EnteringFirst;

            if (_currentState is State.WaitingSecond)
                _currentState = State.EnteringSecond;

            if (_currentState is State.EnteringFirst)
                _firstNumberBuilder.Append(c);

            if (_currentState is State.EnteringSecond)
                _secondNumberBuilder.Append(c);

            UpdateNumber();
        }

        private void ProcessOperation(char c)
        {
            if (_currentState is not State.EnteringFirst)
                return;

            Operation = ParseOperation(c);
            _currentState = State.WaitingSecond;
        }

        private void ProcessSign()
        {
            if (_currentState is State.WaitingFirst)
                FirstSign = -1;

            if (_currentState is State.WaitingSecond)
                SecondSign = -1;
        }

        private void ProcessDecimalPoint()
        {
            if (_currentState is State.EnteringFirst && !FirstHasDecimalPoint)
            {
                _firstNumberBuilder.Append(DecimalPoint);
                FirstHasDecimalPoint = true;
            }

            if (_currentState is State.EnteringSecond && !SecondHasDecimalPoint)
            {
                _secondNumberBuilder.Append(DecimalPoint);
                SecondHasDecimalPoint = true;
            }
        }

        private void ProcessEraseCommand()
        {
            switch (_currentState)
            {
                case State.ShowingResult:
                    return;

                case State.WaitingFirst:
                    if (FirstSign is -1)
                        FirstSign = 1;
                    return;

                case State.WaitingSecond when SecondSign is -1:
                    SecondSign = 1;
                    return;

                case State.WaitingSecond:
                    Operation = MathOperation.None;
                    _currentState = State.EnteringFirst;
                    return;

                case State.EnteringFirst:
                    if (_firstNumberBuilder[^1] == DecimalPoint)
                        FirstHasDecimalPoint = false;
                    
                    _firstNumberBuilder.Remove(_firstNumberBuilder.Length - 1, 1);
                    if (_firstNumberBuilder.Length == 0)
                        _currentState = State.WaitingFirst;
                    break;

                case State.EnteringSecond:
                    if (_secondNumberBuilder[^1] == DecimalPoint)
                        SecondHasDecimalPoint = false;
                    _secondNumberBuilder.Remove(_secondNumberBuilder.Length - 1, 1);
                    if (_secondNumberBuilder.Length == 0)
                        _currentState = State.WaitingSecond;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_currentState), _currentState, "Unknown state");
            }

            UpdateNumber();
        }

        private void ProcessClearCommand()
        {
            First = null;
            Second = null;
            Result = null;
            Operation = MathOperation.None;
            _firstNumberBuilder.Clear();
            _secondNumberBuilder.Clear();
            FirstHasDecimalPoint = false;
            SecondHasDecimalPoint = false;
            FirstSign = 1;
            SecondSign = 1;
            _currentState = State.WaitingFirst;
        }

        private void ProcessCalculateCommand()
        {
            if (_currentState is not State.EnteringSecond)
                return;

            Debug.Assert(First != null, nameof(First) + " != null");
            Debug.Assert(Second != null, nameof(Second) + " != null");

            Result = Calculate(First.Value * FirstSign, Second.Value * SecondSign, Operation);
            _currentState = State.ShowingResult;
        }

        private static Result Calculate(double first, double second, MathOperation operation)
        {
            return operation switch
            {
                MathOperation.Add => new Result {Value = first + second},
                MathOperation.Substract => new Result {Value = first - second},
                MathOperation.Multiply => new Result {Value = first * second},
                MathOperation.Divide => second != 0
                    ? new Result {Value = first / second}
                    : new Result {IsValid = false, Message = Result.DivisionByZero},
                MathOperation.None => throw new InvalidOperationException(),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation")
            };
        }

        private void UpdateNumber()
        {
            switch (_currentState)
            {
                case State.EnteringFirst:
                    First = double.Parse(_firstNumberBuilder.ToString(), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture);
                    break;

                case State.EnteringSecond:
                    Second = double.Parse(_secondNumberBuilder.ToString(), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture);
                    break;

                case State.WaitingFirst:
                    First = null;
                    break;

                case State.WaitingSecond:
                    Second = null;
                    break;

                default:
                    return;
            }
        }

        private static MathOperation ParseOperation(char c)
        {
            return c switch
            {
                '+' => MathOperation.Add,
                '-' => MathOperation.Substract,
                '*' => MathOperation.Multiply,
                '/' => MathOperation.Divide,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, "Cannot parse as math operation")

            };
        }

        public enum MathOperation
        {
            None,
            Add,
            Substract,
            Multiply,
            Divide
        }

        private enum State
        {
            WaitingFirst,
            EnteringFirst,
            WaitingSecond,
            EnteringSecond,
            ShowingResult
        }

        private enum InputType
        {
            None,
            Digit,
            Operation,
            Sign,
            DecimalPoint,
            Erase,
            Clear,
            Calculate,
            Incorrect
        }
    }
}