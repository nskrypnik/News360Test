using System;
using System.Collections.Generic;
using System.Linq;

namespace News360test
{
	public class RawExpressionParser
	{
		enum ParseStates { EscapeSymbols, Start, WaitSymbols, WaitOperand };

		private RawExpression rawExpression;
		private ParseStates state;
		private int index;
		private int blockOffset;
		private int globalIndex;
		private int coeff;
		private int bracketCounter;

		public RawExpressionParser(RawExpression rawExpression)
		{
			this.rawExpression = rawExpression;
			this.reset();
		}

		private void reset()
		{
			this.state = ParseStates.Start;
			this.index = 0;
			this.blockOffset = 0;
			this.globalIndex = rawExpression.offset;
			this.coeff = 1;
			this.bracketCounter = 0;
		}

		public IEnumerable<RawMultiplier> Iterator()
		{
			RawMultiplier output = null;

			foreach (char c in this.rawExpression.rawString)
			{
				switch (this.state)
				{
					case ParseStates.Start:
						this.HandleStart(c);
						break;
					case ParseStates.WaitSymbols:
						this.HandleWaitSymbols(c);
						break;
					case ParseStates.WaitOperand:
						output = this.HandleWaitOperand(c);
						break;
					case ParseStates.EscapeSymbols:
						this.HandleEscapeSymbols(c);
						break;
				}

				if (output != null)
				{
					yield return output;
					output = null;
				}

				this.index++;
				this.globalIndex++;
			}

			yield return this.Finalize();
			this.reset();
		}

		private void HandleStart(char c)
		{
			if (c != ' ') // ignore all whitespaces
			{
				switch (c)
				{
					case '-':
						this.state = ParseStates.WaitSymbols;
						this.coeff = -1;
						break;
					case '+':
						throw new ParseException("'+' sign is not expected to be here", this.globalIndex);
					case '(':
						this.state = ParseStates.EscapeSymbols;
						this.blockOffset = this.index;
						break;
					default:
						// here we hit some symbol of expression
						this.state = ParseStates.WaitOperand;
						this.blockOffset = this.index;
						break;
				}
			}
		}

		private void HandleEscapeSymbols(char c)
		{
			if (c == ')')
			{
				if (this.bracketCounter == 0)
				{
					this.state = ParseStates.WaitOperand;
				}
				else
				{
					this.bracketCounter--;
				}
			}
			else
			{
				if (c == '(')
				{
					this.bracketCounter++;
				}
			}
		}

		private RawMultiplier HandleWaitOperand(char c)
		{
			if (c == '+' || c == '-')
			{
				string rawBlockString = this.rawExpression
											.rawString.Substring(this.blockOffset, index - this.blockOffset);
				RawMultiplier output = new RawMultiplier(this.blockOffset, rawBlockString, this.coeff);
				// define coefficent
				this.coeff = (c == '-') ? -1 : 1;
				this.state = ParseStates.WaitSymbols;

				return output;
			}
			else
			{
				if (c == '(')
				{
					this.state = ParseStates.EscapeSymbols;
				}
			}

			return null;
		}

		private void HandleWaitSymbols(char c)
		{
			switch (c)
			{
				case '+':
				case '-':
					throw new ParseException("Summand symbols are expected here, not operand", this.globalIndex);
				case ' ':
					break;
				case '(':
					this.blockOffset = index;
					this.state = ParseStates.EscapeSymbols;
					break;
				default:
					this.blockOffset = index;
					this.state = ParseStates.WaitOperand;
					break;
			}
		}

		private RawMultiplier Finalize()
		{
			if (this.state == ParseStates.WaitSymbols || this.state == ParseStates.Start)
			{
				throw new ParseException("Symbols are expected after operand", this.globalIndex);
			}

			if (this.state == ParseStates.EscapeSymbols)
			{
				throw new ParseException("Expecting ')' here", this.globalIndex);
			}

			string rawBlockString = this.rawExpression.rawString
										.Substring(this.blockOffset, this.index - this.blockOffset);
			return new RawMultiplier(this.blockOffset, rawBlockString, this.coeff);
		}

	}

	public class RawMultiplierParser
	{
		enum ParseStates { Start, ExtractBlock, ParseDigit, ParsePowerDigit };

		private int index;
		private int globalIndex;
		private string stack;
		private int bracketCount;
		private ParseStates state;
		private RawMultiplier rawMultiplier;
		private List<Expression> expressions;

		public RawMultiplierParser(RawMultiplier rawMultiplier)
		{
			this.rawMultiplier = rawMultiplier;
		}

		private void reset()
		{
			this.index = 0;
			this.globalIndex = this.rawMultiplier.offset;
			this.state = ParseStates.Start;
			this.expressions = new List<Expression>();
			this.stack = String.Empty;
			this.bracketCount = 0;
		}

		public List<Expression> Parse()
		{
			this.reset();
			string parsedString = rawMultiplier.rawString;
			foreach (char c in rawMultiplier.rawString)
			{
				this.RunFSM(c);

				this.index++;
				this.globalIndex++;
			}

			this.RunFSM('\n'); // run else one time with end of line to finish

			return this.expressions;
		}

		private void RunFSM(char c)
		{
			switch (state)
			{
				case ParseStates.Start:
					this.HandleStart(c);
					break;
				case ParseStates.ExtractBlock:
					this.HandleExtractBlock(c);
					break;
				case ParseStates.ParseDigit:
					this.HandleParseDigit(c);
					break;
				case ParseStates.ParsePowerDigit:
					this.HandleParsePowerDigit(c);
					break;
			}
		}

		private void addSummand(char c)
		{
			Dictionary<char, int> variable = new Dictionary<char, int>();
			variable.Add(c, 1);
			List<Summand> summands = new List<Summand>();
			summands.Add(new Summand(1, variable));
			this.expressions.Add(new Expression(summands));
		}

		private void HandleStart(char c)
		{
			this.stack = String.Empty;
			switch (c)
			{
				case '\n':
					break;
				case ' ':
					// ignore whitespaces
					break;
				case '(':
					this.state = ParseStates.ExtractBlock;
					break;
				case '^':
					this.state = ParseStates.ParsePowerDigit;
					break;
				default:
					if (Char.IsLetter(c))
					{
						if (Char.IsUpper(c))
						{
							throw new ParseException("Only lower case letters are allowed", this.globalIndex);
						}
						this.addSummand(c);
					}
					else
					{
						if (Char.IsDigit(c))
						{
							this.stack += c;
							this.state = ParseStates.ParseDigit;
						}
						else
						{
							throw new ParseException($"'{c}' symbol is not allowed here", this.globalIndex);
						}
					}
					break;
			}
		}

		private void HandleExtractBlock(char c)
		{
			switch (c)
			{
				case '\n':
					throw new ParseException("')' is expected here", this.globalIndex);
				case '(':
					this.stack += c;
					this.bracketCount++;
					break;
				case ')':
					if (this.bracketCount == 0)
					{
						RawExpression rawExpression = new RawExpression(this.globalIndex - this.stack.Length, this.stack);
						this.expressions.Add(new Expression(rawExpression));
						this.state = ParseStates.Start;
					}
					else
					{
						this.bracketCount--;
						this.stack += c;
					}
					break;
				default:
					this.stack += c;
					break;
			}
		}

		private void HandleParseDigit(char c)
		{
			if (Char.IsDigit(c) || c == '.')
			{
				// check whether we have already one dot in stack string
				if (c == '.' && this.stack.IndexOf('.') != -1)
				{
					throw new ParseException("Illigal symbol here", this.globalIndex);
				}

				this.stack += c;
			}
			else
			{
				double coeff = double.Parse(this.stack);
				List<Summand> summands = new List<Summand>();
				summands.Add(new Summand(coeff, new Dictionary<char, int>()));
				this.expressions.Add(new Expression(summands));
				this.state = ParseStates.Start;
				this.HandleStart(c);
			}
		}

		private void HandleParsePowerDigit(char c)
		{
			if (!Char.IsDigit(c) || c == '\n')
			{
				if (String.IsNullOrEmpty(this.stack))
				{
					throw new ParseException("Only digits are allowed here", this.globalIndex);
				}

				int power = int.Parse(this.stack);
				if (this.expressions.Count == 0)
				{
					throw new ParseException("Power operation should follow some expression", this.globalIndex);
				}
				Expression lastExpression = this.expressions.Last();
				for (int i = 0; i < power - 1; i++)
				{
					this.expressions.Add(lastExpression.Clone());
				}
				this.state = ParseStates.Start;
				this.HandleStart(c);
			}
			else
			{
				this.stack += c;
			}
		}
	}
}
