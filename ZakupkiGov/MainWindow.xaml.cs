using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZakupkiGov.Enums;
using ZakupkiGov.Model;

namespace ZakupkiGov
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller _tasksController;

        public MainWindow()
        {
            InitializeComponent();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (File.Exists("data.dat"))
            {
                var data = File.ReadAllLines("data.dat");

                if (data.Length == 2)
                {
                    ExcelFilePathTextBox.Text = data[0];
                    DirectoryTextBox.Text = data[1];
                }
            }


            //var web = new ZakupkiGovWeb();

            //var zakupka = web.ParseMainInfo("0322300003821000042");

            //zakupka.Directory = @"D:\Projects\ZakupkiGov\ZakupkiGov\bin\Debug\Закупки\" + zakupka.Number;

            //if (!Directory.Exists(zakupka.Directory))
            //{
            //    Directory.CreateDirectory(zakupka.Directory);
            //}

            //zakupka.Files.AddRange(web.ParseFiles(zakupka.Number, zakupka.Directory));

            //ExcelController.AddZakupki("Заявки.xlsm", zakupka);

            //var line = "7,5.32,chocolate_teacake,red_potato_pepper,mega_monster_cheeseburger,apple_juice";

            //var data = line.Split(',');
            //var restaurantId = int.Parse(data[0].Trim());
            //var menu = new Dictionary<string, decimal>();

            //menu.Add(data.Skip(2).Select(s => s.Trim()).Aggregate((a, b) => a.Trim() + "," + b.Trim()), decimal.Parse(data[1].Trim(), CultureInfo.InvariantCulture));
        }

        private  void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tasksController == null || _tasksController.GetState() == ProcessStates.NONE)
            {
                if (string.IsNullOrEmpty(ExcelFilePathTextBox.Text) || !File.Exists(ExcelFilePathTextBox.Text))
                {
                    MessageBox.Show("Укажите файл екселя");
                }
                else if (string.IsNullOrEmpty(DirectoryTextBox.Text) || !Directory.Exists(DirectoryTextBox.Text))
                {
                    MessageBox.Show("Укажите директорию номеров");
                }
                else if (FileIsLocked(ExcelFilePathTextBox.Text))
                {
                    MessageBox.Show("Файл exel открыт. Закройте перед началом загрузки");
                }
                else if (string.IsNullOrEmpty(NumbersTextBox.Text))
                {
                    MessageBox.Show("Нет номеров");
                }
                else
                {
                    var lines = NumbersTextBox.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    _tasksController = new Controller(1, 1000, 5000, DirectoryTextBox.Text, ExcelFilePathTextBox.Text);

                    _tasksController.AddItems(lines.ToList());

                    _tasksController.OnMessage += Controller_OnMessage;

                    DataContext = _tasksController;

                    _tasksController.ExecuteAsync();
                }
            }
            else if (_tasksController.GetState() == ProcessStates.WORKING)
            {
                _tasksController.Stop();
            }
        }

        public bool FileIsLocked(string strFullFileName)
        {
            bool blnReturn = false;
            System.IO.FileStream fs;
            try
            {
                fs = System.IO.File.Open(strFullFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                fs.Close();
            }
            catch (System.IO.IOException ex)
            {
                blnReturn = true;
            }
            return blnReturn;
        }

        private void Controller_OnMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (message.Contains("Номер уже существует"))
                {
                    LogTextBox.AppendText(string.Format("[{0}] - ", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")));

                    var textRange = new TextRange(LogTextBox.Document.ContentEnd, LogTextBox.Document.ContentEnd);

                    textRange.Text = message + Environment.NewLine;
                    textRange.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 255, 182, 188)));
                }
                else
                if (message.Contains("Загружено"))
                {
                    LogTextBox.AppendText(string.Format("[{0}] - ", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")));

                    var textRange = new TextRange(LogTextBox.Document.ContentEnd, LogTextBox.Document.ContentEnd);

                    textRange.Text = message + Environment.NewLine;
                    textRange.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 0, 255, 0)));
                }
                else
                {
                    LogTextBox.AppendText(string.Format("[{0}] - {1}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), message + Environment.NewLine));
                }
            });
        }

        private void OpenExcelFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                ExcelFilePathTextBox.Text = openFileDialog.FileName;
                SaveConfig();
            }
        }

        private void OpenDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    DirectoryTextBox.Text = dialog.SelectedPath;
                    SaveConfig();
                }
            }
        }
        private void SaveConfig()
        {
            var s = ExcelFilePathTextBox.Text + Environment.NewLine + DirectoryTextBox.Text;

            File.WriteAllText("data.dat", s);
        }
    }
}
