using NUnit.Framework;

namespace GDF.ConfigImport.Tests
{
    /// <summary>
    /// Covers the RFC 4180 subset <see cref="CsvParser"/> supports: quoted cells, embedded commas,
    /// embedded line breaks, escaped quotes, trailing newlines, mixed row widths.
    /// </summary>
    public sealed class CsvParserTests
    {
        [Test]
        public void Parse_PlainTwoByTwo_ReturnsExpectedCells()
        {
            var rows = CsvParser.Parse("a,b\n1,2");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(new[] { "a", "b" }, rows[0].ToArray());
            Assert.AreEqual(new[] { "1", "2" }, rows[1].ToArray());
        }

        [Test]
        public void Parse_QuotedCell_PreservesEmbeddedComma()
        {
            var rows = CsvParser.Parse("name,desc\n\"Pike\",\"Fast, armored\"");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("Fast, armored", rows[1][1]);
        }

        [Test]
        public void Parse_EscapedQuote_IsReturnedAsSingleQuote()
        {
            var rows = CsvParser.Parse("field\n\"She said \"\"hi\"\"\"");
            Assert.AreEqual("She said \"hi\"", rows[1][0]);
        }

        [Test]
        public void Parse_CrlfLineEndings_SplitsRows()
        {
            var rows = CsvParser.Parse("a,b\r\n1,2\r\n3,4\r\n");
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("4", rows[2][1]);
        }

        [Test]
        public void Parse_EmbeddedNewlineInQuotes_StaysOnTheSameRow()
        {
            var rows = CsvParser.Parse("a,b\n\"line1\nline2\",ok");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("line1\nline2", rows[1][0]);
            Assert.AreEqual("ok", rows[1][1]);
        }

        [Test]
        public void Parse_TrailingEmptyLines_AreDiscarded()
        {
            var rows = CsvParser.Parse("a\n1\n\n\n");
            Assert.AreEqual(2, rows.Count);
        }

        [Test]
        public void Parse_ShortRow_PaddedToHeaderWidth()
        {
            var rows = CsvParser.Parse("a,b,c\n1,2");
            Assert.AreEqual(3, rows[1].Count);
            Assert.AreEqual(string.Empty, rows[1][2]);
        }

        [Test]
        public void Parse_LongRow_TruncatedToHeaderWidth()
        {
            var rows = CsvParser.Parse("a,b\n1,2,3,4");
            Assert.AreEqual(2, rows[1].Count);
            Assert.AreEqual("2", rows[1][1]);
        }

        [Test]
        public void Parse_NullOrEmpty_ReturnsEmpty()
        {
            Assert.AreEqual(0, CsvParser.Parse(null).Count);
            Assert.AreEqual(0, CsvParser.Parse(string.Empty).Count);
        }

        [Test]
        public void Parse_FileWithoutTerminatingNewline_StillSplitsLastRow()
        {
            var rows = CsvParser.Parse("a,b\n1,2\n3,4");
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("3", rows[2][0]);
        }
    }
}
