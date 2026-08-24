using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xceed.Words.NET;
using Xceed.Document.NET;

class Program
{
    static void Main(string[] args)
    {
        string mdPath = @"d:\repos\SDK-E-INVOICING-SYSTEM\docs\client_pitch_and_demo_guide.md";
        string docxPath = @"d:\repos\SDK-E-INVOICING-SYSTEM\docs\client_pitch_and_demo_guide.docx";

        if (!File.Exists(mdPath))
        {
            Console.WriteLine($"Markdown file not found: {mdPath}");
            return;
        }

        Console.WriteLine($"Reading markdown from: {mdPath}");
        var lines = File.ReadAllLines(mdPath);

        Console.WriteLine($"Generating DOCX to: {docxPath}");
        using (DocX document = DocX.Create(docxPath))
        {
            // Set margins (72 points = 1 inch)
            document.MarginLeft = 72f;
            document.MarginRight = 72f;
            document.MarginTop = 72f;
            document.MarginBottom = 72f;

            List<string[]> tableRows = new List<string[]>();
            bool inTable = false;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].TrimEnd();

                // Handle tables
                if (line.TrimStart().StartsWith("|"))
                {
                    inTable = true;
                    // Parse cells
                    string[] cells = ParseTableCells(line);
                    // Skip if it is just a separator row (like | :--- | ---: |)
                    if (IsSeparatorRow(cells))
                    {
                        continue;
                    }
                    tableRows.Add(cells);
                    continue;
                }
                else
                {
                    // If we were in a table and it ended, write it
                    if (inTable && tableRows.Count > 0)
                    {
                        InsertTable(document, tableRows);
                        tableRows.Clear();
                        inTable = false;
                    }
                }

                // Empty line
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Headers
                if (line.StartsWith("# "))
                {
                    var p = document.InsertParagraph();
                    p.SpacingBefore(24);
                    p.SpacingAfter(12);
                    p.KeepWithNext = true;
                    var text = line.Substring(2).Trim();
                    var run = p.Append(text).Bold().FontSize(22).Font(new Font("Georgia"));
                    run.Color(System.Drawing.Color.FromArgb(26, 54, 93)); // Deep navy
                    continue;
                }
                else if (line.StartsWith("## "))
                {
                    var p = document.InsertParagraph();
                    p.SpacingBefore(18);
                    p.SpacingAfter(10);
                    p.KeepWithNext = true;
                    var text = line.Substring(3).Trim();
                    var run = p.Append(text).Bold().FontSize(15).Font(new Font("Arial"));
                    run.Color(System.Drawing.Color.FromArgb(44, 122, 123)); // Teal
                    continue;
                }
                else if (line.StartsWith("### "))
                {
                    var p = document.InsertParagraph();
                    p.SpacingBefore(14);
                    p.SpacingAfter(8);
                    p.KeepWithNext = true;
                    var text = line.Substring(4).Trim();
                    var run = p.Append(text).Bold().FontSize(12).Font(new Font("Arial"));
                    run.Color(System.Drawing.Color.FromArgb(74, 85, 104)); // Slate gray
                    continue;
                }

                // Horizontal rule
                if (line.Trim() == "---")
                {
                    var p = document.InsertParagraph();
                    p.SpacingBefore(15);
                    p.SpacingAfter(15);
                    p.BorderBottom = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, System.Drawing.Color.LightGray);
                    continue;
                }

                // Numbered lists (e.g. 1. **FBR Downtime:** ...)
                var numMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.*)$");
                if (numMatch.Success)
                {
                    var indentSpaces = numMatch.Groups[1].Value.Length;
                    var number = numMatch.Groups[2].Value;
                    var content = numMatch.Groups[3].Value;

                    var p = document.InsertParagraph();
                    p.SpacingAfter(4);
                    p.IndentationBefore = (indentSpaces + 2) * 10f; // basic indentation
                    
                    p.Append($"{number}. ").Bold();
                    AppendFormattedText(p, content);
                    continue;
                }

                // Bullet lists (e.g. * **Real-time Statistics:** ...)
                var bulletMatch = Regex.Match(line, @"^(\s*)([\*\-])\s+(.*)$");
                if (bulletMatch.Success)
                {
                    var indentSpaces = bulletMatch.Groups[1].Value.Length;
                    var content = bulletMatch.Groups[3].Value;

                    var p = document.InsertParagraph();
                    p.SpacingAfter(4);
                    p.IndentationBefore = (indentSpaces + 2) * 10f;

                    string bulletSymbol = indentSpaces > 2 ? "▪ " : "• ";
                    p.Append(bulletSymbol);
                    AppendFormattedText(p, content);
                    continue;
                }

                // Normal Paragraph
                {
                    var p = document.InsertParagraph();
                    p.SpacingAfter(8);
                    p.LineSpacing = 1.15f;
                    AppendFormattedText(p, line.Trim());
                }
            }

            // If file ended while in table
            if (inTable && tableRows.Count > 0)
            {
                InsertTable(document, tableRows);
            }

            document.Save();
        }

        Console.WriteLine("Word file generated successfully!");
    }

    static string[] ParseTableCells(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);

        string[] parts = trimmed.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Trim();
        }
        return parts;
    }

    static bool IsSeparatorRow(string[] cells)
    {
        if (cells.Length == 0) return false;
        foreach (var cell in cells)
        {
            string t = cell.Replace("-", "").Replace(":", "").Trim();
            if (t.Length > 0)
            {
                return false;
            }
        }
        return true;
    }

    static void InsertTable(DocX document, List<string[]> tableRows)
    {
        if (tableRows.Count == 0) return;

        int colCount = 0;
        foreach (var row in tableRows)
        {
            if (row.Length > colCount) colCount = row.Length;
        }

        int rowCount = tableRows.Count;
        var t = document.AddTable(rowCount, colCount);

        t.Design = TableDesign.TableGrid;
        t.Alignment = Alignment.center;

        for (int r = 0; r < rowCount; r++)
        {
            var row = tableRows[r];
            var docRow = t.Rows[r];
            docRow.Height = 24;

            for (int col = 0; col < colCount; col++)
            {
                var cell = docRow.Cells[col];
                cell.MarginTop = 6;
                cell.MarginBottom = 6;
                cell.MarginLeft = 8;
                cell.MarginRight = 8;

                var p = cell.Paragraphs[0];
                p.Alignment = Alignment.left;

                if (col < row.Length)
                {
                    string cellValue = row[col];
                    if (r == 0)
                    {
                        cell.FillColor = System.Drawing.Color.FromArgb(44, 122, 123); // Teal
                        var run = p.Append(cellValue).Bold();
                        run.Color(System.Drawing.Color.White);
                        p.Alignment = Alignment.center;
                    }
                    else
                    {
                        if (r % 2 == 0)
                        {
                            cell.FillColor = System.Drawing.Color.FromArgb(247, 250, 252);
                        }
                        AppendFormattedText(p, cellValue);
                    }
                }
            }
        }

        document.InsertTable(t);
        document.InsertParagraph().SpacingAfter(10);
    }

    static void AppendFormattedText(Paragraph p, string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            // Bold
            if (i + 1 < text.Length && text[i] == '*' && text[i+1] == '*')
            {
                int next = text.IndexOf("**", i + 2);
                if (next != -1)
                {
                    string boldText = text.Substring(i + 2, next - (i + 2));
                    p.Append(boldText).Bold();
                    i = next + 2;
                    continue;
                }
            }
            // Italic
            if (text[i] == '*' && (i == 0 || text[i-1] != '*'))
            {
                int next = text.IndexOf('*', i + 1);
                if (next != -1 && (next + 1 == text.Length || text[next+1] != '*'))
                {
                    string italicText = text.Substring(i + 1, next - (i + 1));
                    p.Append(italicText).Italic();
                    i = next + 1;
                    continue;
                }
            }
            // Inline Code
            if (text[i] == '`')
            {
                int next = text.IndexOf('`', i + 1);
                if (next != -1)
                {
                    string codeText = text.Substring(i + 1, next - (i + 1));
                    var run = p.Append(codeText);
                    run.Font(new Font("Consolas"));
                    run.Color(System.Drawing.Color.FromArgb(199, 37, 78));
                    run.FontSize(9.5);
                    i = next + 1;
                    continue;
                }
            }

            int nextSpecial = text.IndexOfAny(new char[] { '*', '`' }, i);
            if (nextSpecial == -1)
            {
                p.Append(text.Substring(i));
                break;
            }
            else
            {
                p.Append(text.Substring(i, nextSpecial - i));
                i = nextSpecial;
            }
        }
    }
}
