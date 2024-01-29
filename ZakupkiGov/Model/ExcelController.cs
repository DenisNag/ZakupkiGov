using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ZakupkiGov.Model
{
    public static class ExcelController
    {
        public static void AddZakupki(string filePath, ref List<string> errorsList, params Zakupka[] zakupki)
        {
            using (var file = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = file.Workbook.Worksheets["Закупки"];
                var lastRow = worksheet.Dimension != null ? worksheet.Dimension.Rows + 1 : 1;
                var listNumbers = new HashSet<string>();
                var listAlreadyExists = new List<string>();

                for (int i = 2; i < 9999; i++)
                {
                    if (string.IsNullOrEmpty(worksheet.Cells["A" + i].Text))
                    {
                        lastRow = i;

                        break;
                    }

                    listNumbers.Add(worksheet.Cells["B" + i].Text);

                    //if (worksheet.Cells["D" + i].Hyperlink != null)
                    //{
                    //    Console.WriteLine(worksheet.Cells["D" + i].Hyperlink.ToString());
                    //}
                }

                foreach (var zakupka in zakupki)
                {
                    if (!listNumbers.Contains(zakupka.Number))
                    {
                        worksheet.Cells["A" + lastRow].Value = zakupka.Info;
                        worksheet.Cells["B" + lastRow].Value = zakupka.Number;
                        worksheet.Cells["B" + lastRow].Hyperlink = new Uri("https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=" + zakupka.Number);
                        worksheet.Cells["C" + lastRow].Value = zakupka.NameSite;
                        worksheet.Cells["E" + lastRow].Value = zakupka.StartMaxPrice;
                        worksheet.Cells["L" + lastRow].Value = zakupka.EndDate;
                        worksheet.Cells["M" + lastRow].Value = zakupka.AuctionDate;
                        worksheet.Cells["N" + lastRow].Value = zakupka.Place;
                        worksheet.Cells["P" + lastRow].Value = "Подать";

                        //worksheet.Cells["D" + lastRow].Formula = $"=HYPERLINK(\"{zakupka.Directory.Replace("\\", "\\\\")}\",\"Тыц\")";
                        worksheet.Cells["D" + lastRow].Hyperlink = new ExcelHyperLink(zakupka.Number, UriKind.RelativeOrAbsolute) { Display = "Тыц" };// $" =HYPERLINK(\"{zakupka.Number}\",\"Тыц\")";

                        lastRow++;
                    }
                    else
                    {
                        errorsList.Add(zakupka.Number);
                    }
                }

                file.Save();
            }
        }
    }
}
