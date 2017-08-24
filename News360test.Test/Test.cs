using NUnit.Framework;
using System;
using System.Collections.Generic;
using News360test;
using System.Linq;

namespace News360test.Test
{
    [TestFixture()]
    public class RawExpressionParserTest
    {
        [Test()]
        public void TestIterate()
        {
            // things are going right
            RawExpression rawExpression1 = new RawExpression(0, "-4 + 6 -9xy");
            RawExpressionParser parser = new RawExpressionParser(rawExpression1);

            List<RawMultiplier> multipliers = parser.Iterator().ToList();
            Assert.AreEqual(multipliers.Count, 3);

            Assert.AreEqual(multipliers[0].rawString, "4 ");
            Assert.AreEqual(multipliers[1].rawString, "6 ");
            Assert.AreEqual(multipliers[2].rawString, "9xy");

            Assert.AreEqual(multipliers[0].coeff, -1);
            Assert.AreEqual(multipliers[1].coeff, 1);
            Assert.AreEqual(multipliers[2].coeff, -1);

            Assert.AreEqual(multipliers[0].offset, 1);
            Assert.AreEqual(multipliers[1].offset, 5);
            Assert.AreEqual(multipliers[2].offset, 8);

            parser = new RawExpressionParser(new RawExpression(0, "- +"));
            // things are going wrong
            try
            {
                multipliers = parser.Iterator().ToList();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 2);
                Assert.AreEqual(e.Message, "Summand symbols are expected here, not operand");
            }

            parser = new RawExpressionParser(new RawExpression(0, " +78z"));
            try
            {
                multipliers = parser.Iterator().ToList();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 1);
                Assert.AreEqual(e.Message, "'+' sign is not expected to be here");
            }

            parser = new RawExpressionParser(new RawExpression(0, "78z + uh + - 9"));
            try
            {
                multipliers = parser.Iterator().ToList();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 11);
                Assert.AreEqual(e.Message, "Summand symbols are expected here, not operand");
            }

            parser = new RawExpressionParser(new RawExpression(0, "78z + (dfdg) + 78x - "));
            try
            {
                multipliers = parser.Iterator().ToList();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 21);
                Assert.AreEqual(e.Message, "Symbols are expected after operand");
            }
        }

        [Test()]
        public void TestIterateWithBrackets()
        {
            RawExpression rawExpression1 = new RawExpression(0, "(x + y)(y^2 + 10) - (5)");
			RawExpressionParser parser = new RawExpressionParser(rawExpression1);

			List<RawMultiplier> multipliers = parser.Iterator().ToList();

            Assert.AreEqual(multipliers.Count, 2);
            Assert.AreEqual(multipliers[0].rawString, "(x + y)(y^2 + 10) ");
            Assert.AreEqual(multipliers[1].rawString, "(5)");

            rawExpression1 = new RawExpression(0, "((1 - z) + 4) + (((4))) + ((x))");
			parser = new RawExpressionParser(rawExpression1);

            multipliers = parser.Iterator().ToList();
			Assert.AreEqual(multipliers.Count, 3);
            Assert.AreEqual(multipliers[0].rawString, "((1 - z) + 4) ");
            Assert.AreEqual(multipliers[1].rawString, "(((4))) ");
            Assert.AreEqual(multipliers[2].rawString, "((x))");

		}
    }

    [TestFixture()]
    public class RawMultiplierParserTest
    {
        [Test()]
        public void TestParseSimple()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "xyz"));
            List<Expression> expressions = parser.Parse();
            Assert.AreEqual(expressions.Count, 3);
            Assert.AreEqual(expressions[0].summands.Count, 1);
            Assert.AreEqual(expressions[1].summands.Count, 1);
            Assert.AreEqual(expressions[2].summands.Count, 1);

            Assert.IsTrue(expressions[0].summands[0].variables.ContainsKey('x'));
            Assert.IsFalse(expressions[0].summands[0].variables.ContainsKey('y'));
            Assert.IsFalse(expressions[0].summands[0].variables.ContainsKey('z'));

            Assert.IsFalse(expressions[1].summands[0].variables.ContainsKey('x'));
            Assert.IsTrue(expressions[1].summands[0].variables.ContainsKey('y'));
            Assert.IsFalse(expressions[1].summands[0].variables.ContainsKey('z'));

            Assert.IsFalse(expressions[2].summands[0].variables.ContainsKey('x'));
            Assert.IsFalse(expressions[2].summands[0].variables.ContainsKey('y'));
            Assert.IsTrue(expressions[2].summands[0].variables.ContainsKey('z'));
        }

        [Test()]
        public void TestCaseWithPower()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "x^3y^2"));
            List<Expression> expressions = parser.Parse();

            Assert.AreEqual(expressions.Count, 5);
            Assert.AreEqual(expressions[0].summands[0].variables.Keys.Count, 1);
            Assert.IsTrue(expressions[0].summands[0].variables.ContainsKey('x'));

            Assert.AreEqual(expressions[1].summands[0].variables.Keys.Count, 1);
            Assert.IsTrue(expressions[1].summands[0].variables.ContainsKey('x'));

            Assert.AreEqual(expressions[2].summands[0].variables.Keys.Count, 1);
            Assert.IsTrue(expressions[2].summands[0].variables.ContainsKey('x'));

            Assert.AreEqual(expressions[3].summands[0].variables.Keys.Count, 1);
            Assert.IsTrue(expressions[3].summands[0].variables.ContainsKey('y'));

            Assert.AreEqual(expressions[4].summands[0].variables.Keys.Count, 1);
            Assert.IsTrue(expressions[4].summands[0].variables.ContainsKey('y'));
        }

        [Test()]
        public void TestCaseWithNumbers()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "56^2x^3y^2z17.8"));
            List<Expression> expressions = parser.Parse();
            Assert.AreEqual(expressions.Count, 9);

            Assert.AreEqual(expressions[0].summands[0].coeff, 56);
            Assert.AreEqual(expressions[1].summands[0].coeff, 56);

            Assert.AreEqual(expressions[8].summands[0].coeff, 17.8);
        }

        [Test()]
        public void TestCaseWithSimpleBrackets()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "(xy)(zw)"));
            List<Expression> expressions = parser.Parse();
            Assert.AreEqual(expressions.Count, 2);

            parser = new RawMultiplierParser(new RawMultiplier(0, "(xy)(zw) x^2"));
            expressions = parser.Parse();
            Assert.AreEqual(expressions.Count, 4);
        }

        [Test()]
        public void TestExceptionWhenBracketIsNotClosed()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "(xy)(zw"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 7);
                Assert.AreEqual(e.Message, "')' is expected here");
            }
        }

        [Test()]
        public void TestImproperPowerSign()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(0, "^2zx"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 2);
                Assert.AreEqual(e.Message, "Power operation should follow some expression");
            }
        }

        [Test()]
        public void TestImproperPowerUsage()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(4, "^zx"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 5);
                Assert.AreEqual(e.Message, "Only digits are allowed here");
            }
        }

        [Test()]
        public void TestImproperDotUsage()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(5, "2^2zx45.76.7"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 15);
                Assert.AreEqual(e.Message, "Illigal symbol here");
            }
        }

        [Test()]
        public void TestIllegalSign()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(3, "2^2.76"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 6);
                Assert.AreEqual(e.Message, "'.' symbol is not allowed here");
            }
        }

        [Test()]
        public void TestOnlyLowerCase()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(6, "2^2X76"));
            try
            {
                List<Expression> expressions = parser.Parse();
                Assert.Fail();
            }
            catch (ParseException e)
            {
                Assert.AreEqual(e.index, 9);
                Assert.AreEqual(e.Message, "Only lower case letters are allowed");
            }
        }

        [Test()]
        public void TestOneInBrackets()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(6, "((1 - z) + 4)"));
            List<Expression> expressions = parser.Parse();
		}

        [Test()]
        public void TestDoubleBrackets()
        {
            RawMultiplierParser parser = new RawMultiplierParser(new RawMultiplier(6, "((x + 1)(x + 2))"));
            List<Expression> expressions = parser.Parse();
        }
    }

    [TestFixture()]
    public class SummandTest
    {
        [Test()]
        public void TestClone()
        {
            Summand s = new Summand(10, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } });
            Summand cloned = s.Clone();

            Assert.AreNotSame(s, cloned);
            Assert.AreEqual(cloned.coeff, 10);
            Assert.AreEqual(cloned.variables['x'], 2);
            Assert.AreEqual(cloned.variables['y'], 1);
        }

        [Test()]
        public void TestMultiply()
        {
            Summand s1 = new Summand(5, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } });
            Summand s2 = new Summand(3, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 2 } });

            Summand s3 = s1 * s2;

            Assert.AreEqual(s3.coeff, 15);
            Assert.AreEqual(s3.variables['x'], 3);
            Assert.AreEqual(s3.variables['y'], 1);
            Assert.AreEqual(s3.variables['z'], 2);
        }

        [Test()]
        public void TestCalculateIndex()
        {
            Summand s = new Summand(10, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } });
            List<long> magicNumberLookup = new List<long>() { 2, 3, 5 };
            List<char> variables = new List<char>() { 'y', 'x' };
            Assert.AreEqual(s.CalculateIndex(variables, magicNumberLookup), 35);

			magicNumberLookup = new List<long>() { 3, 6, 14 };
			variables = new List<char>() { 'z', 'y', 'x' };
            Assert.AreEqual(s.CalculateIndex(variables, magicNumberLookup), 96);

            s = new Summand(10, new Dictionary<char, int>());
			magicNumberLookup = new List<long>() { 3, 6, 14 };
			variables = new List<char>() { 'z', 'y', 'x' };
            Assert.AreEqual(s.CalculateIndex(variables, magicNumberLookup), 3);

            s = new Summand(10, new Dictionary<char, int>(){{'x', 3}});
			magicNumberLookup = new List<long>() { 3, 6, 14, 36};
			variables = new List<char>() { 'z', 'y', 'x' };
            Assert.AreEqual(s.CalculateIndex(variables, magicNumberLookup), 353);
        }
    }

    [TestFixture()]
    class MultiplierTest
    {
        [Test()]
        public void TestMultiplyExpressions()
        {
            Summand s1 = new Summand(1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } });
            Summand s2 = new Summand(3, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 2 } });

            Summand s3 = new Summand(2, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } });
            Summand s4 = new Summand(1, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 2 } });

            Expression e1 = new Expression(new List<Summand>() { s1, s2 });
            Expression e2 = new Expression(new List<Summand>() { s3, s4 });

            Multiplier m = new Multiplier();
            m.expressions = new List<Expression>() { e1, e2 };
            Expression res = m.Multiply();
            Assert.AreEqual(res.summands.Count, 4);
            // check coefficients first
            Assert.AreEqual(res.summands[0].coeff, 2);
            Assert.AreEqual(res.summands[1].coeff, 1);
            Assert.AreEqual(res.summands[2].coeff, 6);
            Assert.AreEqual(res.summands[3].coeff, 3);

            Assert.AreEqual(res.summands[0].variables['x'], 4);
            Assert.AreEqual(res.summands[0].variables['y'], 2);

            Assert.AreEqual(res.summands[1].variables['x'], 3);
            Assert.AreEqual(res.summands[1].variables['y'], 1);
            Assert.AreEqual(res.summands[1].variables['z'], 2);

            Assert.AreEqual(res.summands[2].variables['x'], 3);
            Assert.AreEqual(res.summands[2].variables['y'], 1);
            Assert.AreEqual(res.summands[2].variables['z'], 2);

            Assert.AreEqual(res.summands[3].variables['x'], 2);
            Assert.AreEqual(res.summands[3].variables['z'], 4);
        }
    }

    [TestFixture()]
    public class TestExpression
    {
        [Test()]
        public void TestParse()
        {
            Expression e = new Expression(new RawExpression(0, "(x + y)(y^2 - 10) - 5^2 + ((1 - z) + 4) - (1 - x)^2"));
            List<Summand> summs = e.summands;
        }
    }

    [TestFixture()]
    public class TestEquationNormalizer
    {
        [Test()]
        public void TestMagicNumbers()
        {
            EquationNormalizer e = new EquationNormalizer();
            List<long> magicLookup = e.CreateMagicNumbers(3, 3);
            Assert.AreEqual(magicLookup[0], 3);
            Assert.AreEqual(magicLookup[1], 6);
            Assert.AreEqual(magicLookup[2], 14);
            Assert.AreEqual(magicLookup[3], 36);
        }

        [Test()]
        public void TestSortSummands()
        {
            List<Summand> summands = new List<Summand>()
            {
                new Summand(1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
                new Summand(2, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
                new Summand(4, new Dictionary<char, int>() { { 'x', 3 }, { 'y', 1 } }),
                new Summand(10, new Dictionary<char, int>())
            };

            EquationNormalizer e = new EquationNormalizer();
            List<Summand> sortedSummands = e.Sort(summands);

            Assert.AreEqual(sortedSummands[0].coeff, 4);
            Assert.AreEqual(sortedSummands[1].coeff, 1);
            Assert.AreEqual(sortedSummands[2].coeff, 2);
            Assert.AreEqual(sortedSummands[3].coeff, 10);
        }

        [Test()]
        public void TestIndexSummands()
        {
			List<Summand> summands = new List<Summand>()
			{
				new Summand(1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
                new Summand(-2, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
				new Summand(2, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
                new Summand(3, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
                new Summand(3, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
				new Summand(4, new Dictionary<char, int>() { { 'x', 3 }, { 'y', 1 } }),
				new Summand(10, new Dictionary<char, int>()),
                new Summand(-4, new Dictionary<char, int>())
			};

			EquationNormalizer e = new EquationNormalizer();
			List<Summand> sortedSummands = e.Sort(summands);

			Assert.AreEqual(sortedSummands[0].coeff, 4);
			Assert.AreEqual(sortedSummands[1].coeff, -1);
			Assert.AreEqual(sortedSummands[2].coeff, 8);
			Assert.AreEqual(sortedSummands[3].coeff, 6);
        }

        [Test ()]
        public void TestAnalyze()
        {
			List<Summand> summands = new List<Summand>()
			{
				new Summand(1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
				new Summand(-2, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
				new Summand(2, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
				new Summand(3, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
				new Summand(4, new Dictionary<char, int>() { { 'x', 3 }, { 'y', 1 } }),
				new Summand(10, new Dictionary<char, int>()),
				new Summand(-4, new Dictionary<char, int>())
			};

            EquationNormalizer e = new EquationNormalizer();
            Tuple<List<char>, int> a = e.Analyze(summands);
            Assert.AreEqual(a.Item1.Count, 3);
            Assert.AreEqual(a.Item2, 3);
        }

        [Test()]
        public void TestGenerateEquationString()
        {
			List<Summand> summands = new List<Summand>()
			{
                new Summand(4, new Dictionary<char, int>() { { 'x', 3 }, { 'y', 1 } }),
				new Summand(-1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
				new Summand(2, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
				new Summand(-10, new Dictionary<char, int>())
			};

			EquationNormalizer e = new EquationNormalizer();
            string output = e.GenerateEquationString(summands);
            Assert.AreEqual(output, "4x^3y - x^2y + 2xz - 10 = 0");

			summands = new List<Summand>()
			{
				new Summand(4, new Dictionary<char, int>() { { 'x', 3 }, { 'y', 1 } }),
				new Summand(-1, new Dictionary<char, int>() { { 'x', 2 }, { 'y', 1 } }),
				new Summand(2, new Dictionary<char, int>() { { 'x', 1 }, { 'z', 1 } }),
                new Summand(0, new Dictionary<char, int>() { { 'x', 2 }, { 'z', 2 } }),
				new Summand(-10, new Dictionary<char, int>())
			};

			output = e.GenerateEquationString(summands);
			Assert.AreEqual(output, "4x^3y - x^2y + 2xz - 10 = 0");

            output = e.GenerateEquationString(new List<Summand>(){
                new Summand(0, new Dictionary<char, int>() { { 'x', 2 }, { 'z', 2 } })
            });
            Assert.AreEqual(output, "0 = 0");
        }

        [Test()]
        public void TestNormalize()
        {
            EquationNormalizer e = new EquationNormalizer();
            string output = e.Normalize("((x - 2)(y + 3)) = 0");
            Assert.AreEqual(output, "xy + 3x - 2y - 6 = 0");

            output = e.Normalize("(x - 2)(y + 3) = ((x - 5)(x - 1))");
            Assert.AreEqual(output, "-x^2 + xy + 9x - 2y - 11 = 0");

            output = e.Normalize("(x - 2)(y + 3) = xy(x - 5)(x - 1)");
            Assert.AreEqual(output, "-x^3y + 6x^2y - 4xy + 3x - 2y - 6 = 0");

            output = e.Normalize("(x - 2)((x + 5) + 4) = 0");
            Assert.AreEqual(output, "x^2 + 7x - 18 = 0");

			output = e.Normalize("x = 1");
			Assert.AreEqual(output, "x - 1 = 0");

			output = e.Normalize("x^2 + 3.5xy + y = y^2 - xy + y");
			Assert.AreEqual(output, "x^2 - y^2 + 4.5xy = 0");

			output = e.Normalize("x - (0 - (0 - x)) = 0");
			Assert.AreEqual(output, "0 = 0");
		}
    }
}

