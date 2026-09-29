using System.IO.Compression;
using System.Text;
using OnlineQuizApp.ViewModels;

namespace OnlineQuizApp.Services
{
    // A minimal, dependency-free .xlsx writer. An .xlsx file is just a zip archive of a small
    // set of XML parts (the OOXML SpreadsheetML format) - this hand-builds exactly the parts
    // needed for one simple worksheet, using only System.IO.Compression (already part of .NET),
    // so no extra NuGet package/build dependency is needed for this one export feature.
    public static class SimpleXlsxWriter
    {
        // Builds a single-sheet workbook for the Test Event results export:
        // S.No, Roll Number, Section, Test Name, Marks Scored.
        public static byte[] BuildResultsWorkbook(List<TestEventResultRow> rows)
        {
            var headers = new[] { "S.No", "Roll Number", "Section", "Test Name", "Marks Scored" };

            var sheetRows = new List<string>();
            sheetRows.Add(BuildRow(1, headers.Select(h => ((object)h, true)).ToArray()));

            int serial = 1;
            int rowNum = 2;
            foreach (var row in rows)
            {
                var marks = row.Attempted ? $"{row.Score}/{row.TotalQuestions}" : "Not Attempted";
                sheetRows.Add(BuildRow(rowNum, new (object, bool)[]
                {
                    (serial, false),
                    (row.RollNumber ?? "-", true),
                    (row.SectionName ?? "-", true),
                    (row.Language, true),
                    (marks, true)
                }));
                serial++;
                rowNum++;
            }

            var sheetXml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<cols>" +
                "<col min=\"1\" max=\"1\" width=\"8\" customWidth=\"1\"/>" +
                "<col min=\"2\" max=\"2\" width=\"16\" customWidth=\"1\"/>" +
                "<col min=\"3\" max=\"3\" width=\"14\" customWidth=\"1\"/>" +
                "<col min=\"4\" max=\"4\" width=\"20\" customWidth=\"1\"/>" +
                "<col min=\"5\" max=\"5\" width=\"14\" customWidth=\"1\"/>" +
                "</cols>" +
                "<sheetData>" + string.Concat(sheetRows) + "</sheetData>" +
                "</worksheet>";

            return BuildPackage(sheetXml);
        }

        private static string BuildRow(int rowNum, (object value, bool isText)[] cells)
        {
            var sb = new StringBuilder();
            sb.Append($"<row r=\"{rowNum}\">");
            for (int i = 0; i < cells.Length; i++)
            {
                var colLetter = ColumnLetter(i + 1);
                var cellRef = $"{colLetter}{rowNum}";
                var (value, isText) = cells[i];

                if (isText)
                {
                    var escaped = EscapeXml(value?.ToString() ?? "");
                    sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{escaped}</t></is></c>");
                }
                else
                {
                    sb.Append($"<c r=\"{cellRef}\"><v>{value}</v></c>");
                }
            }
            sb.Append("</row>");
            return sb.ToString();
        }

        private static string ColumnLetter(int oneBasedIndex)
        {
            var letters = "";
            while (oneBasedIndex > 0)
            {
                int rem = (oneBasedIndex - 1) % 26;
                letters = (char)('A' + rem) + letters;
                oneBasedIndex = (oneBasedIndex - 1) / 26;
            }
            return letters;
        }

        private static string EscapeXml(string value) =>
            value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");

        private static byte[] BuildPackage(string sheetXml)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(archive, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");

                WriteEntry(archive, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                WriteEntry(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"Results\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "</workbook>");

                WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");

                WriteEntry(archive, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<fonts count=\"2\">" +
                    "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                    "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                    "</fonts>" +
                    "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                    "</styleSheet>");

                WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetXml);
            }

            return ms.ToArray();
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
        }
    }
}
