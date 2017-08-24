using System;
using System.Collections.Generic;
using System.Linq;

/**
 * This file contains all primitives we use to represent an equasion.
 * Most normalized form of equation is Expression class which contains a list
 * of summands. Expression means the sum of all summands it contains.
 * 
 * Expression doesn't perform add operation so it may contains similar summands
 * e.g. x^2 and 4x^2. The job of Expression - to parse and store summands.
 * 
 * Multiplier class represents any multiplying like (x + 1)(x + 2). It contains
 * the list of parsed Expressions, e.g. in our example there're x + 1 and x + 2.
 * 
 * Multyplier class may be converted to Expression with ConvertToExpression function.
 * It means that it multilies all underlying Expressions to get one resul Expression
 * at the end. E.g. in our example we'll get resulting Expression containing
 * x^2, 3x and 2 summands.
 * 
 * So while parsing we simply break equation on sub Expressions and multiply them
 * till we get the final Expression.
 * 
 * The Summand class represent summands and contain data about summand coefficient
 * variables and their powers. Also Summund class provides multiplication operand
 * to multiply two summands and get resulting summand at the end
 */

namespace News360test
{

    public class RawMultiplier
    {
        public int offset;
        public string rawString;
        public int coeff;

        public RawMultiplier(int offset, string rawString, int coeff = 1)
        {
            this.offset = offset;
            this.rawString = rawString;
            this.coeff = coeff;
        }
    }


    public class RawExpression
    {
        public int offset;
        public string rawString;

        public RawExpression(int offset, string rawString)
        {
            this.offset = offset;
            this.rawString = rawString;
        }
    }


    public class Expression 
    {
        private RawExpression rawExpression;
        public List<Summand> summands = new List<Summand>(); // array of summands in expression

        public Expression()
        {}

        public Expression(List<Summand> summands)
        {
            this.summands = summands;
        }

        public Expression(RawExpression rawExpresion)
        {
            this.rawExpression = rawExpresion;
            this.Parse(rawExpression);
        }


        public void Parse(RawExpression rawExpression)
        {
            RawExpressionParser parser = new RawExpressionParser(rawExpression);
            IEnumerable<RawMultiplier> rawMultipliers = parser.Iterator();
            foreach (RawMultiplier rawMultiplier in rawMultipliers)
            {
                Multiplier multiplier = new Multiplier(rawMultiplier);
                Expression expression = multiplier.ConvertToExpression();
                this.Join(expression);
            }
        }

        public void Join(Expression expression)
        {
            this.summands.AddRange(expression.summands);
        }

        /**
         * More like add operation, simply adds all summands from one expression
         * to another
         */
        public Expression Clone()
        {
            List<Summand> clonedSummands = new List<Summand>();
            foreach (Summand summand in this.summands)
            {
                clonedSummands.Add(summand.Clone());
            }
            return new Expression(clonedSummands);
        }
    }

    public class Multiplier
    {
        
        public List<Expression> expressions; // array of expressions in multiplier
        private RawMultiplier rawMultiplier;
        private int coeff = 1;

        public Multiplier()
        {}

        public Multiplier(RawMultiplier rawMultiplier)
        {
            this.rawMultiplier = rawMultiplier;
            this.coeff = rawMultiplier.coeff;
            this.Parse(rawMultiplier); 
        }

        public void Parse(RawMultiplier rawMultiplier)
        {
            RawMultiplierParser parser = new RawMultiplierParser(rawMultiplier);
            this.expressions = parser.Parse();
        }

        public Expression Multiply()
        {
            if (this.expressions.Count == 0)
            {
                // return just empty expression if multiplier doesn't contain expressions
                return new Expression();
            }
            // reduce would be very in use here, but I didn't find any standard reduce
            // function for C# so gonna implement this pattern by my own
            Expression accum = null;
            foreach (Expression current in this.expressions)
            {
                if (accum == null)
                {
                    accum = current;
                }
                else
                {
                    accum = this.MultiplyExpressions(accum, current);
                }
            }
            return accum;
        }

        /**
         * This implements multiplying of two expressions, more like if we
         * multiply two expressions in brackets e.g.
         * 
         *  (x + 5)(y + 6)
         */
        public Expression MultiplyExpressions(Expression left, Expression right)
        {
            List<Summand> newSummands = new List<Summand>();
            foreach (Summand leftSummand in left.summands)
            {
                foreach (Summand rightSummand in right.summands)
                {
                    newSummands.Add(leftSummand * rightSummand);
                }
            }
            return new Expression(newSummands);
        }

        public Expression ConvertToExpression()
        {
            Expression expression = this.Multiply();
            foreach (Summand sum in expression.summands)
            {
                sum.coeff *= this.coeff;
            }

            return expression;
        }
    }


    public class Summand
    {
        public double coeff;
        public Dictionary<char, int> variables;

        public Summand(double coeff, Dictionary<char, int> variables)
        {
            this.coeff = coeff;
            this.variables = variables;
        }

        public Summand Clone()
        {
            return new Summand(this.coeff, new Dictionary<char, int>(this.variables));
        }

        static public Summand operator *(Summand first, Summand second)
        {
            Dictionary<char, int> variables = new Dictionary<char, int>();
            double coeff = first.coeff * second.coeff;
            foreach (Summand multipliee in new List<Summand>{first, second })
            {
				foreach (char key in multipliee.variables.Keys)
				{
					if (!variables.Keys.Contains(key))
					{
						variables.Add(key, multipliee.variables[key]);
					}
					else
					{
						variables[key] += multipliee.variables[key];
					}
				}
            }

            return new Summand(coeff, variables);
        }

        /**
         * This is an essential function for sorting algoritm - it takes a list
         * of variables presented in equation and "magic numbers" - special table
         * which is function of maximum power number in summand
         */
        public long CalculateIndex(List<char> variables, List<long> magicNumbersLookup)
        {
            // Variables should be sorted already
            int groupRank = 0;
            int powerRank = 0;
            int power;
            int varIndex = 1;
            int rankInGroup = 0;
            foreach (char variable in variables)
            {
                if (this.variables.Keys.Contains(variable)) {
                    power = this.variables[variable];
                }
                else
                {
                    power = 0;
                }
                rankInGroup += (int)Math.Pow(varIndex, power);
                if (powerRank < power)
                {
                    powerRank = power;
                }
                groupRank += power;
                varIndex++;
            }
            long magicNumber = magicNumbersLookup[powerRank];
            return magicNumber * powerRank * groupRank + rankInGroup;
        }
    }
}
