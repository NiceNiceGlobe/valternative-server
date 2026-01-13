using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Globalization;
using ValternativeServer.Models.DTOs.Recruiters;

namespace ValternativeServer.Services
{
    public class RecruiterCsvService
    {
        public List<ParsedRecruiterRiderDto> Parse(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.Trim().Replace(" ", "")
            });

            var records = new List<ParsedRecruiterRiderDto>();

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var fullName = csv.GetField("FullName");
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                records.Add(new ParsedRecruiterRiderDto
                {
                    FullName = fullName,
                    Email = csv.GetField("Email"),
                    PhoneNumber = csv.GetField("PhoneNumber"),
                    City = csv.GetField("City"),
                    Nationality = csv.GetField("Nationality")
                });
            }

            return records;
        }

        public List<ParsedRecruiterRiderDto> ParseExcel(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.First();

            var records = new List<ParsedRecruiterRiderDto>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int row = 2; row <= lastRow; row++)
            {
                var fullName = worksheet.Cell(row, 1).GetString();
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                records.Add(new ParsedRecruiterRiderDto
                {
                    FullName = fullName,
                    Email = worksheet.Cell(row, 2).GetString(),
                    PhoneNumber = worksheet.Cell(row, 3).GetString(),
                    City = worksheet.Cell(row, 4).GetString(),
                    Nationality = worksheet.Cell(row, 5).GetString()
                });
            }

            return records;
        }
    }
}