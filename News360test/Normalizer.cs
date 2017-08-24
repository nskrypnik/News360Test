using System;
using System.Collections.Generic;
using System.Linq;

/**
 * This module contains main Normalizer class which uses primitives to parse
 * analyze and transform equation string
 * 
 * Main magic is happening inside of Normalize function which receives equation
 * sting then split it into two parts accordingly equal sign. Then those two parts
 * are parsed using Expression and Multiplier classes. Those two classes recursively
 * go through equation string tokenize it and parse. At the end we have only one
 * expression (after converting right part through Multiplier as we have to multiply
 * right expression to -1) which contains a list of summands.
 * 
 * Then we analyze list of summands to get amount of variables used in equation and
 * the max power in it in Analyze function.
 * 
 * Using those parameters we index each summand in Index function so each summand has
 * it's own unique index and this index may be used for ranking summands.
 * 
 * Then we simply sort summands by indexes and return result
 */

namespace News360test
{
	public class EquationNormalizer
	{
		public Tuple<List<char>, int> Analyze(List<Summand> summands)
		{
			int maxPower = 0;
			List<char> variables = new List<char>();
			foreach (Summand sum in summands)
			{
				foreach (char key in sum.variables.Keys)
				{
					int power = sum.variables[key];
					if (power > maxPower)
					{
						maxPower = power;
					}
					if (!variables.Contains(key))
					{
						variables.Add(key);
					}
				}
			}
			variables.Sort();
			return new Tuple<List<char>, int>(variables, maxPower);
		}

		/**
         * For a sake of optimization we generate magic numbers into lookup
         * table to not calculate it every time we need it
         */
		public List<long> CreateMagicNumbers(int variablesCount, int maxPower)
		{
			long magicNumber;
			List<long> magicNumbersLookup = new List<long>();
			for (int power = 0; power < maxPower + 1; power++)
			{
				magicNumber = 0;
				for (int number = 1; number < variablesCount + 1; number++)
				{
					magicNumber += (long)Math.Pow(number, power);
				}
				magicNumbersLookup.Add(magicNumber);
			}
			return magicNumbersLookup;
		}

		public Dictionary<long, Summand> Index(List<Summand> summands)
		{
			Dictionary<long, Summand> indexedSummands = new Dictionary<long, Summand>();
            // before indexing we have to check how many variables do we have and what is
            // the maximum power of variables in quation
			Tuple<List<char>, int> analysis = this.Analyze(summands);
			List<char> variables = analysis.Item1;
			int maxPower = analysis.Item2;

            // for the sake of optimization calculate here magic numbers - the offset of the
            // group represented by max power in summand and sum of powers of variables in summand.
            // As highier max power or sum of powers - the higher summand should be ranked
			List<long> magicNumbersLookup = this.CreateMagicNumbers(variables.Count, maxPower);
			variables.Reverse();
			foreach (Summand sum in summands)
			{
                // Each summand can calculate it's index if variables and magic numbers
                // a provided
				long sumIndex = sum.CalculateIndex(variables, magicNumbersLookup);
				if (indexedSummands.Keys.Contains(sumIndex))
				{
					// Such summand is already presented in indexed list, just
					// update coefficient accordingly
					indexedSummands[sumIndex].coeff += sum.coeff;
                    // here actually we add all same summands together as we may simply
                    // find that they are same as they have same index
				}
				else
				{
					indexedSummands.Add(sumIndex, sum);
				}
			}
			return indexedSummands;
		}

		public List<Summand> Sort(List<Summand> summands)
		{
            // This function sort summands by it's rank

            // But first we have to index all summands
			Dictionary<long, Summand> indexedSummands = this.Index(summands);
			List<Summand> sortedSummands = new List<Summand>();
			List<long> keys = indexedSummands.Keys.ToList();
			keys.Sort();
			keys.Reverse();
			foreach (long key in keys)
			{
				sortedSummands.Add(indexedSummands[key]);
			}
			return sortedSummands;
		}

		public string GenerateEquationString(List<Summand> sortedSummands)
		{
			string output = String.Empty;
			foreach (Summand sum in sortedSummands)
			{
				if (sum.coeff < 0)
				{
					if (output.Length == 0)
					{
						output += "-";
					}
					else
					{
						output += " - ";
					}
				}
				else
				{
					if (sum.coeff > 0)
					{
						if (output.Length != 0)
						{
							output += " + ";
						}

					}
				}

				if (Math.Abs(sum.coeff) > 0.00000000000000001)
				{
					List<char> keys = sum.variables.Keys.ToList();
					keys.Sort();
					if (Math.Abs(sum.coeff) - 1 > 0.00000000000000001 || keys.Count == 0)
					{
						output += Math.Abs(sum.coeff).ToString();
					}
					foreach (char key in keys)
					{
						int power = sum.variables[key];
						if (power == 1)
						{
							output += key;
						}
						else
						{
							output += key.ToString() + "^" + power.ToString();
						}
					}
				}
			}
			if (output.Length == 0)
			{
				return "0 = 0";
			}
			output += " = 0";
			return output;
		}

        /**
         *  Main magic is happening here
         */
		public string Normalize(string equation)
		{
			string[] parts = equation.Split(new char[] { '=' });
			Expression parsedEquation;
			Multiplier rightPart;

			if (parts.Length != 2)
			{
				return "Equation is incorrect, it should have two parts and equal sign";
			}
			string leftPartRaw = parts[0]; // get left part
			string rightPartRaw = "(" + parts[1] + ")"; // and right part
			int rightOffset = equation.IndexOf('=') + 1;

			try
			{
                // parse left part as an Expression
				parsedEquation = new Expression(new RawExpression(0, leftPartRaw));
			}
			catch (ParseException e)
			{
                // parse right part as Multiplier as we want eventually multiply it to -1
				return this.HandleException(equation, e);
			}

			try
			{
				rightPart = new Multiplier(new RawMultiplier(rightOffset, rightPartRaw, -1));
			}
			catch (ParseException e)
			{
				return this.HandleException(equation, e);
			}

            // If I wrote it in Python I would very likely use PyParser package which make life
            // a way easier for parsing expressions like this, but I decided to go hardcore
            // and write it on C# for which breaf search didn't get result of analogues of
            // PyParse in .NET world. So I simply wrote my own parsers

            // then join left part expression and converted multiplier to expression
			parsedEquation.Join(rightPart.ConvertToExpression());

            // do sorting
			List<Summand> sortedSummands = this.Sort(parsedEquation.summands);
			return this.GenerateEquationString(sortedSummands);
		}

		private string HandleException(string equation, ParseException e)
		{
			string output = "Here's a problem with your equation:\n";
			output += equation + "\n" + new String(' ', e.index) + "^\n";
			output += e.Message;

			return output;
		}
	}
}
