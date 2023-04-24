using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Squirrel;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Data;
using System.Web.Configuration;
using System.Reflection;

namespace DatabaseApp
{
    public partial class MainWindow : Window
    {

        UpdateManager manager;
        public string version;

        public List<Data> datas = new List<Data>();
        public string[] propList = new[] { "№ Личного дела", "ФИО", "ИНН", "Статус", "Конкурсный балл",
            "Телефон", "Факультет", "Форма обучения", "Категория", "Образовательная программа", "План",
            "Наличие льгот", "Преим. право зачисления", "Доп. балл / причины начисления",
            "Дата подачи документов", "Адрес", "Дата рождения", "Данные документа удостоверяющего личность",
            "Данные документа о ранее полученном образовании", "Средний балл аттестата",
            "Балл по русскому языку", "Балл по математике", "Профильный предмет", "Балл по проф. предмету",
            "Средний балл диплома бакалавра / специалиста", "Балл по ГИА", "Балл по иностранному",
            "Средний балл диплома специалиста среднего звена", "Иностранный язык, который изучался",
            "Сведения о родителях", "Созданые даные", "Последнее обновление данных", };

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            Data data = new Data
            {
                results = new List<Results>()
            };

            datas.Add(data);

            DebugLog(DebugState.None);
            updateBtn.IsEnabled = false;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            manager = await UpdateManager
                .GitHubUpdateManager(@"https://github.com/paveldrobny/DatabaseAppUpdater");

            string major = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString();
            string minor = Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
            string build = Assembly.GetExecutingAssembly().GetName().Version.Build.ToString();
            version = $"{major}.{minor}.{build}";
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            var updateInfo = await manager.CheckForUpdate();

            if (updateInfo.ReleasesToApply.Count > 0)
            {
                updateBtn.IsEnabled = true;
            }
            else
            {
                updateBtn.IsEnabled = false;
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await manager.UpdateApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            MessageBox.Show("Обновление завершенно!", "ADI PC");
        }

        void DebugLog(DebugState state)
        {
            switch (state)
            {
                case DebugState.None:
                    txtDebug.Text = $"Здесь будут отображаться последние действия.";
                    break;
                case DebugState.Selected:
                    txtDebug.Text = $"Выбрана {_dbList.SelectedIndex + 1} строка (Всего: {_dbList.Items.Count})";
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "(*.json)|*.json";
            openFileDialog.DefaultExt = ".json";

            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                StreamReader reader = new StreamReader(fileInfo.Open(FileMode.Open, FileAccess.Read));

                var result = JsonConvert.DeserializeObject<Data>(reader.ReadToEnd());

                if (_dbList.Items.Count > 0)
                {
                    if (MessageBox.Show("У вас уже есть данные во вкладке 'Список', открытие файла перезапишет их.\n\nВы точно хотите перезаписать данные?",
                     "ПЕРЕЗАПИСЬ ДАННЫХ",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _dbList.SelectedIndex = -1;
                        datas.Clear();
                        datas.Add(result);
                        _dbList.ItemsSource = datas[0].results;
                        _dbList.Items.Refresh();
                    }

                }
                else
                {
                    _dbList.SelectedIndex = -1;
                    datas.Clear();
                    datas.Add(result);

                    _dbList.ItemsSource = datas[0].results;
                    _dbList.Items.Refresh();
                }

                reader.Close();
                return;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "(*.json)|*.json";
            saveFileDialog1.DefaultExt = ".json";

            if (saveFileDialog1.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(saveFileDialog1.OpenFile()))
                {
                    string output = JsonConvert.SerializeObject(datas);
                    string saveFile = output.Substring(1, output.Length - 2);

                    sw.Write(saveFile);

                    sw.Close();
                }
            }
        }

        private void _dbList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DebugLog(DebugState.Selected);

            if (_dbList.SelectedIndex != -1)
            {
                Results result = (Results)_dbList.Items[_dbList.SelectedIndex];

                _dbListPreview.Items.Clear();
                _dbListPreview.Items.Add(new TextBlock { Text = "ПРЕДОСМОТР", FontWeight = FontWeights.Bold, FontSize = 18 });

                var props = result.GetType().GetProperties();

                int i = -1;
                foreach (var res in props)
                {
                    i++;

                    TextBlock textBlock = new TextBlock();
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.Inlines.Add(new Bold(new Run($"{propList[i]}: ")));
                    textBlock.Inlines.Add(new Run(res.GetValue(result).ToString()));
                    _dbListPreview.Items.Add(textBlock);
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Results r = new Results() { name = testNAME.Text, personalID = testCODE.Text };

            datas[0].results.Add(r);
            _dbList.Items.Refresh();
            _dbList.ItemsSource = datas[0].results;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"ADI PC v{version}\n\nРазработана на помощью: WPF & C#\nРазработчик: Дробный Павел", "ADI PC", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public enum DebugState
    {
        None,
        Selected
    }

    public class Data
    {
        public List<Results> results { get; set; }
    }

    public class Results
    {
        public override string ToString()
        {
            return $"№{personalID}, {name}";
        }

        public string getData()
        {
            return
                $"№ ЛИЧНОГО ДЕЛА - {personalID}\n" +
                $"ФИО - {name}\n" +
                $"СТАТУС - {status}\n" +
                $"КОНКУРСНЫЙ БАЛЛ - {score}\n" +
                $"ФАКУЛЬТЕТ - {faculty}\n" +
                $"СПЕЦИАЛЬНОСТЬ - {category}\n" +
                $"ФОРМА ОБУЧЕНИЕ - {formEducation}\n" +
                $"ОБРАЗОВАТЕЛЬНАЯ ПРОГРАММА - {program}\n";
        }

        [JsonProperty("personalID")]
        public string personalID { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("icode")]
        public string icode { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("score")]
        public int score { get; set; }

        [JsonProperty("phone")]
        public string phone { get; set; }

        [JsonProperty("faculty")]
        public string faculty { get; set; }

        [JsonProperty("formEducation")]
        public string formEducation { get; set; }

        [JsonProperty("category")]
        public string category { get; set; }

        [JsonProperty("program")]
        public string program { get; set; }

        [JsonProperty("plan")]
        public string plan { get; set; }

        [JsonProperty("privileges")]
        public string privileges { get; set; }

        [JsonProperty("primary")]
        public string primary { get; set; }

        [JsonProperty("extra")]
        public string extra { get; set; }

        // ////////////////////////////////////////////

        [JsonProperty("documentsDate")]
        public string documentsDate { get; set; }

        [JsonProperty("adress")]
        public string adress { get; set; }

        [JsonProperty("birthday")]
        public string birthday { get; set; }

        [JsonProperty("identityDocument")]
        public string identityDocument { get; set; }

        [JsonProperty("previouslyEducation")]
        public string previouslyEducation { get; set; }

        [JsonProperty("averageScoreCertificate")]
        public double averageScoreCertificate { get; set; }

        [JsonProperty("scoreRussian")]
        public int scoreRussian { get; set; }

        [JsonProperty("scoreMath")]
        public int scoreMath { get; set; }

        [JsonProperty("profileSubject")]
        public string profileSubject { get; set; }

        [JsonProperty("scoreProfileSubject")]
        public int scoreProfileSubject { get; set; }

        [JsonProperty("averageScoreDegree")]
        public double averageScoreDegree { get; set; }

        [JsonProperty("scoreGIA")]
        public int scoreGIA { get; set; }

        [JsonProperty("scoreForeign")]
        public int scoreForeign { get; set; }

        [JsonProperty("averageScoreMiddle")]
        public double averageScoreMiddle { get; set; }

        [JsonProperty("foreignLang")]
        public string foreignLang { get; set; }

        [JsonProperty("parent")]
        public string parent { get; set; }

        [JsonProperty("createdAt")]
        public DateTime createdAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime updatedAt { get; set; }
    }
}
