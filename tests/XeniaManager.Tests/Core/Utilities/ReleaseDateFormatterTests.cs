using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

[TestFixture]
public class ReleaseDateFormatterTests
{
    [TestCase("2010-05-18", "18th May 2010")]
    [TestCase("2005-11-22", "22nd November 2005")]
    [TestCase("2001-01-01", "1st January 2001")]
    [TestCase("2002-02-02", "2nd February 2002")]
    [TestCase("2003-03-03", "3rd March 2003")]
    [TestCase("2004-04-11", "11th April 2004")]
    [TestCase("2006-06-12", "12th June 2006")]
    [TestCase("2007-07-13", "13th July 2007")]
    [TestCase("2008-08-21", "21st August 2008")]
    [TestCase("2009-09-23", "23rd September 2009")]
    [TestCase("2011-10-31", "31st October 2011")]
    public void Format_ParsesIsoDate_ReturnsOrdinalDate(string input, string expected)
    {
        string? result = ReleaseDateFormatter.Format(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-date")]
    public void Format_UnparseableInput_ReturnsInputUnchanged(string? input)
    {
        string? result = ReleaseDateFormatter.Format(input);

        Assert.That(result, Is.EqualTo(input));
    }
}