using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WebLibrary.Web;

namespace ZakupkiGov.Model
{
    public class ZakupkiGovWeb : Social
    {
        private static Regex DateRegex = new Regex(@"(?<day>[0-9]+)\.(?<month>[0-9]+)\.(?<year>[0-9]+)", RegexOptions.Compiled);
        private static Regex TimeRegex = new Regex("(?<hour>[0-9]+):(?<minute>[0-9]+)", RegexOptions.Compiled);
        private static Regex RegionRegex = new Regex(@"МСК((?<zone>(\+|\-)[0-9]+)){0,1}", RegexOptions.Compiled);

        public Zakupka ParseMainInfo(string number)
        {
            var url = "https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=" + number;

            DoOperation(() => UrlOperation(Operation.GET, url), "", 3);

            if (HtmlPageText != null)
            {
                var zakupka = new Zakupka(number);
                var doc = new HtmlAgilityPack.HtmlDocument();

                doc.LoadHtml(HtmlPageText);

                // var table = doc.DocumentNode.SelectSingleNode(".//div[contains(@id, 'purchaseObject') or @id='medTable']/table");

                var table = doc.DocumentNode.SelectSingleNode(".//div/h2[text()='Информация об объекте закупки']/..//table");


                var tableData = GetTableData(table);

                var codePosition = tableData[0].IndexOf("Код позиции");

                var info = doc.DocumentNode.SelectSingleNode(".//tr[@class='tableBlock__row']/td[" + (codePosition + 2) + "]/text()");

                if (codePosition >= 0)
                {
                    //var info = tableData[1][codePosition + 1];

                    zakupka.Info = HttpUtility.HtmlDecode(info.InnerText.Replace(Environment.NewLine, " ").Replace("  ", "").Replace("&nbsp;", " ").Trim());//tableData[1][codePosition + 1].Replace(Environment.NewLine, " ").Replace("  ", "").Replace("&nbsp;", " ").Trim();

                    var spanName = doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Наименование электронной площадки')]/span[@class='section__info']");
                    var startMaxNode = doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Начальная (максимальная) цена')]/span[@class='section__info']");
                    var placeNode = doc.DocumentNode.SelectSingleNode(".//tr[contains(@class,'displayNone')]/td[@class='alignRight']") ??
                                    doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Организация, осуществляющая размещение')]/span[@class='section__info']");
                    var endDateNode = doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Дата и время окончания срока подачи заявок')]/span[@class='section__info']");
                    var auctionDateNode = doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Дата проведения аукциона')]/span[@class='section__info']");
                    var auctionTimeNode = doc.DocumentNode.SelectSingleNode(".//section[contains(., 'Время проведения аукциона')]/span[@class='section__info']");

                    zakupka.NameSite = HttpUtility.HtmlDecode(spanName.InnerText.Trim());
                    zakupka.StartMaxPrice = HttpUtility.HtmlDecode(startMaxNode.InnerText.Trim());
                    zakupka.Place = HttpUtility.HtmlDecode(placeNode.InnerText.Trim());

                    var dates = CreateMSKDate(endDateNode.InnerText.Trim(), auctionDateNode != null ? auctionDateNode.InnerText.Trim() : "", auctionTimeNode != null ? auctionTimeNode.InnerText.Trim() : "");

                    zakupka.EndDate = dates.Item1.HasValue ? dates.Item1.Value.ToString("dd.MM.yyyy HH:mm") : "";
                    zakupka.AuctionDate = dates.Item2.HasValue ? dates.Item2.Value.ToString("dd.MM.yyyy HH:mm") : "";

                    return zakupka;
                }
            }

            return null;
        }

        public List<ZakupkaFile> ParseFiles(string number, string directory)
        {
            var url = "https://zakupki.gov.ru/epz/order/notice/ea44/view/documents.html?regNumber=" + number;
            var files = new List<ZakupkaFile>();

            DoOperation(() => UrlOperation(Operation.GET, url), "", 3);

            if (HtmlPageText != null)
            {
                var doc = new HtmlAgilityPack.HtmlDocument();

                doc.LoadHtml(HtmlPageText);

                var attachedBlocks = doc.DocumentNode.SelectNodes(".//div[text()='Прикрепленные файлы']");

                if (attachedBlocks != null)
                {
                    foreach (var attachedBlock in attachedBlocks)
                    {
                        var fileNodes = attachedBlock.ParentNode.SelectNodes(".//span[@class='section__value']/a");

                        foreach (var fileNode in fileNodes)
                        {
                            var path = directory + "\\" + fileNode.Attributes["title"].Value;

                            try
                            {
                                using (var content = GetImageFromUrl(fileNode.Attributes["href"].Value))
                                {
                                    using (Stream file = File.Create(path))
                                    {
                                        content.CopyTo(file);
                                    }
                                }

                                var zakupkaFile = new ZakupkaFile() { Title = fileNode.Attributes["title"].Value };

                                files.Add(zakupkaFile);
                            }
                            catch { }
                        }
                    }
                }
            }

            return files;
        }

        public static List<List<string>> GetTableData(HtmlAgilityPack.HtmlNode table)
        {
            var tableData = new List<List<string>>();
            var rows = table.SelectNodes(".//tr");

            foreach (var row in rows)
            {
                var rowData = new List<string>();

                foreach (var cell in row.SelectNodes("th|td"))
                {
                    rowData.Add(cell.InnerText.Trim());
                }

                tableData.Add(rowData);
            }

            return tableData;
        }

        private static Tuple<DateTime?, DateTime?> CreateMSKDate(string dateStr, string auctionDateStr, string auctioTimeStr)
        {
            var date = (DateTime?)null;
            var auctiondate = (DateTime?)null;
            var dateMatch = DateRegex.Match(dateStr);
            var timeMatch = TimeRegex.Match(dateStr);
            var timezoneMatch = RegionRegex.Match(dateStr);
            var timeZone = 0;

            if (dateMatch.Success)
            {
                if (timezoneMatch.Groups["zone"].Success)
                {
                    timeZone = -Convert.ToInt32(timezoneMatch.Groups["zone"].Value);
                }

                date = new DateTime(Convert.ToInt32(dateMatch.Groups["year"].Value), Convert.ToInt32(dateMatch.Groups["month"].Value), Convert.ToInt32(dateMatch.Groups["day"].Value));

                date = date.Value.AddHours(Convert.ToInt32(timeMatch.Groups["hour"].Value)).AddMinutes(Convert.ToInt32(timeMatch.Groups["minute"].Value)).AddHours(timeZone);

                var auctionDate = DateRegex.Match(auctionDateStr);
                var auctionTime = TimeRegex.Match(auctioTimeStr);

                if (auctionDate.Success)
                {
                    auctiondate = new DateTime(Convert.ToInt32(auctionDate.Groups["year"].Value), Convert.ToInt32(auctionDate.Groups["month"].Value), Convert.ToInt32(auctionDate.Groups["day"].Value), Convert.ToInt32(auctionTime.Groups["hour"].Value), Convert.ToInt32(auctionTime.Groups["minute"].Value), 0).AddHours(timeZone);
                }
            }

            return new Tuple<DateTime?, DateTime?>(date, auctiondate);
        }
    }
}
