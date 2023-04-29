using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Squirrel;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Documents;
using System.Reflection;
using System.Collections.ObjectModel;

namespace DatabaseApp
{
    public partial class MainWindow : Window
    {

        UpdateManager manager;
        public string version;

        public ObservableCollection<Data> datas = new ObservableCollection<Data>();
  
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

            OutputLog(OutputState.None);
            updateBtn.IsEnabled = false;
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(searchByID.Text))
                return true;
            else
                return ((item as Data).results[0].personalID.IndexOf(searchByID.Text, StringComparison.OrdinalIgnoreCase) >= 0);
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

        void OutputLog(OutputState state)
        {
            switch (state)
            {
                case OutputState.None:
                    txtDebug.Text = $"Здесь будут отображаться последние действия.";
                    break;
                case OutputState.Selected:
                    txtDebug.Text = $"Выбрана {_dbList.SelectedIndex + 1} строка (Всего: {_dbList.Items.Count})";
                    break;
                case OutputState.Debug:
                    txtDebug.Text = searchByID.Text == "" ? "Test" : "_";
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
                        _dbList.Items.Filter = IDFilter;
                        _dbList.Items.Refresh();
                    }
                }
                else
                {
                    _dbList.SelectedIndex = -1;
                    datas.Clear();
                    datas.Add(result);

                    _dbList.ItemsSource = datas[0].results;
                    _dbList.Items.Filter = IDFilter;
                    _dbList.Items.Refresh();
                }

                reader.Close();
                return;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "(*.xls)|*.xls";
            saveFileDialog1.DefaultExt = ".xls";

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
            OutputLog(OutputState.Selected);

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

        private void AddStudent()
        {
            Results r = new Results() { 
                documentsDate = inputDocumentsDate.Text, 
                personalID = inputPersonalCode.Text, 
                name = inputName.Text,
                adress = inputAdress.Text,
                birthday = inputBirthday.Text,
                icode = inputIcode.Text,
                identityDocument = inputIdentityDocument.Text,
                previouslyEducation = inputPreviouslyEducation.Text,
                averageScoreCertificate = Convert.ToDouble(inputAverageScoreCertificate.Text),
                scoreRussian = Convert.ToInt32(inputScoreRussian.Text),
                scoreMath = Convert.ToInt32(inputScoreMath.Text),
                profileSubject = inputProfileSubject.Text,
                scoreProfileSubject = Convert.ToInt32(inputScoreProfileSubject.Text),
                averageScoreDegree = Convert.ToDouble(inputAverageScoreDegree.Text),
                scoreGIA = Convert.ToInt32(inputScoreGIA.Text),
                scoreForeign = Convert.ToInt32(inputScoreForeign.Text),
                averageScoreMiddle = Convert.ToDouble(inputAverageScoreMiddle.Text),
                score = 0,
                //score = Convert.ToInt32(inputAverageScoreCertificate.Text) 
                //        + Convert.ToInt32(inputScoreRussian.Text) 
                //        + Convert.ToInt32(inputScoreMath.Text) 
                //        + Convert.ToInt32(inputScoreProfileSubject.Text)
                //        + Convert.ToInt32(inputAverageScoreDegree.Text)
                //        + Convert.ToInt32(inputScoreGIA.Text) 
                //        + Convert.ToInt32(inputScoreForeign.Text)
                //        + Convert.ToInt32(inputAverageScoreMiddle.Text),
                faculty = inputFaculty.Text,
                formEducation = inputFormEducation.Text,
                category = inputCategory.Text,
                program = inputProgram.Text,
                plan = inputPlan.Text,
                privileges = inputPrivileges.Text,
                primary = inputPrimary.Text,
                foreignLang = inputForeignLang.Text,
                phone = inputPhone.Text,
                parent = inputParent.Text,
                status = inputStatus.Text,
                extra = inputExtra.Text,
            };

            datas[0].results.Add(r);
            _dbList.ItemsSource = datas[0].results;
            _dbList.Items.Filter = IDFilter;
            _dbList.Items.Refresh();

        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"ADI PC v{version}\n\nРазработана на помощью: WPF & C#\nРазработчик: Дробный Павел", "ADI PC", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowMargin.Margin = new Thickness(10);
            }
            else
            {
                WindowMargin.Margin = new Thickness(1);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void searchByID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchByID.Text == null)
                _dbList.Items.Filter = null;
            else
                _dbList.Items.Filter = IDFilter;   
        }

        private bool IDFilter(object obj)
        {
            var filterObj = obj as Results;

            return filterObj.personalID.Contains(searchByID.Text);
        }

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            AddStudent();
        }

        private void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Редактировать текущее личное дело",
                     "РЕДАКТИРОВАНИЕ ЛИЧНОГО ДЕЛА",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Results editData = datas[0].results[_dbList.SelectedIndex];
                inputDocumentsDate.Text = editData.documentsDate;
                inputPersonalCode.Text = editData.personalID;
                inputName.Text = editData.name;
                inputAdress.Text = editData.adress;
                inputBirthday.Text = editData.birthday;
                inputIcode.Text = editData.icode;
                inputIdentityDocument.Text = editData.identityDocument;
                inputPreviouslyEducation.Text = editData.previouslyEducation;
                inputAverageScoreCertificate.Text = editData.averageScoreCertificate.ToString();
                inputScoreRussian.Text = editData.scoreRussian.ToString();
                inputScoreMath.Text = editData.scoreMath.ToString();
                inputProfileSubject.Text = editData.profileSubject;
                inputScoreProfileSubject.Text = editData.scoreProfileSubject.ToString();
                inputAverageScoreDegree.Text = editData.averageScoreDegree.ToString();
                inputScoreGIA.Text = editData.scoreGIA.ToString();
                inputScoreForeign.Text = editData.scoreForeign.ToString();
                inputAverageScoreMiddle.Text = editData.averageScoreMiddle.ToString();
                inputFaculty.Text = editData.faculty;
                inputFormEducation.Text = editData.formEducation;
                inputCategory.Text = editData.category;
                inputProgram.Text = editData.program;
                inputPlan.Text = editData.plan;
                inputPrivileges.Text = editData.privileges;
                inputPrimary.Text = editData.primary;
                inputForeignLang.Text = editData.foreignLang;
                inputPhone.Text = editData.phone;
                inputParent.Text = editData.parent;
                inputStatus.Text = editData.status;
                inputExtra.Text = editData.extra;
            }
        }

        private void ApplyEditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Применить редактирование текущего личного дела",
                     "ПРИМЕНИТЬ РЕДАКТИРОВАНИЕ ЛИЧНОГО ДЕЛА",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Results editData = datas[0].results[_dbList.SelectedIndex];
                editData.documentsDate = inputDocumentsDate.Text;
                editData.personalID = inputPersonalCode.Text;
                editData.name = inputName.Text;
                editData.adress = inputAdress.Text;
                editData.birthday = inputBirthday.Text;
                editData.icode = inputIcode.Text;
                editData.identityDocument = inputIdentityDocument.Text;
                editData.previouslyEducation = inputPreviouslyEducation.Text;
                editData.averageScoreCertificate = Convert.ToDouble(inputAverageScoreCertificate.Text);
                editData.scoreRussian = Convert.ToInt32(inputScoreRussian.Text);
                editData.scoreMath = Convert.ToInt32(inputScoreMath.Text);
                editData.profileSubject = inputProfileSubject.Text;
                editData.scoreProfileSubject = Convert.ToInt32(inputScoreProfileSubject.Text);
                editData.averageScoreDegree = Convert.ToDouble(inputAverageScoreDegree.Text);
                editData.scoreGIA = Convert.ToInt32(inputScoreGIA.Text);
                editData.scoreForeign = Convert.ToInt32(inputScoreForeign.Text);
                editData.averageScoreMiddle = Convert.ToDouble(inputAverageScoreMiddle.Text);
                //editData.score = Convert.ToInt32(inputAverageScoreCertificate.Text)
                //        + Convert.ToInt32(inputScoreRussian.Text)
                //        + Convert.ToInt32(inputScoreMath.Text)
                //        + Convert.ToInt32(inputScoreProfileSubject.Text)
                //        + (int)Convert.ToDouble(inputAverageScoreDegree.Text)
                //        + Convert.ToInt32(inputScoreGIA.Text)
                //        + Convert.ToInt32(inputScoreForeign.Text)
                //        + (int)Convert.ToDouble(inputAverageScoreMiddle.Text);
                editData.faculty = inputFaculty.Text;
                editData.formEducation = inputFormEducation.Text;
                editData.category = inputCategory.Text;
                editData.program = inputProgram.Text;
                editData.plan = inputPlan.Text;
                editData.privileges = inputPrivileges.Text;
                editData.primary = inputPrimary.Text;
                editData.foreignLang = inputForeignLang.Text;
                editData.phone = inputPhone.Text;
                editData.parent = inputParent.Text;
                editData.status = inputStatus.Text;
                editData.extra = inputExtra.Text;

            }
        }

        private void DuplicateStudent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Дублировать текущее личное дело",
                     "СОЗДАНИЕ ДУБЛИКАТА",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {

                Results newProfile = datas[0].results[_dbList.SelectedIndex];
                datas[0].results.Add(newProfile);
                _dbList.ItemsSource = datas[0].results;
                _dbList.Items.Filter = IDFilter;
                _dbList.Items.Refresh();
                _dbList.SelectedIndex = 0;
            }
        }

        private void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить личное дело",
                     "УДАЛЕНИЯ ЛИЧНОГО ДЕЛА",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {

                datas[0].results.RemoveAt(_dbList.SelectedIndex);
                _dbList.ItemsSource = datas[0].results;
                _dbList.Items.Filter = IDFilter;
                _dbList.Items.Refresh();
                _dbList.SelectedIndex = 0;
            }
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Все личные дела будут удалены. Продолжить?",
                     "УДАЛЕНИЯ ВСЕХ ЛИЧНЫХ ДЕЛ",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                datas[0].results.Clear();
                _dbList.ItemsSource = datas[0].results;
                _dbList.Items.Filter = IDFilter;
                _dbList.Items.Refresh();
                _dbList.SelectedIndex = -1;
                _dbListPreview.Items.Clear();
            }
        }
    }

    public enum OutputState
    {
        None,
        Selected,
        Debug
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
