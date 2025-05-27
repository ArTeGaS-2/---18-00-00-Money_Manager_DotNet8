using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Text;
using ControlzEx.Standard;
using System.Globalization;

namespace WPF_Updated_Money_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;

        // ObservableCollection для зберігання транзакцій,
        // автоматично оновлює UI при додаванні/видаленні елементів
        private ObservableCollection<Transaction> Transactions_;
        // Змінна для зберігання поточного балансу
        private decimal Balance;

        // Змінні для відстеженян стану сортування
        private GridViewColumnHeader _lastHeaderClicked = null; // Заголовок
        private ListSortDirection _lastDirection = 
            ListSortDirection.Ascending; // Напрямок сортування

        private readonly List<string> incomeCategories = new List<string>
        {
            "Зарплата",
            "Інше"
        };
        private readonly List<string> expenseCategories = new List<string>
        {
            "Комунальні платежі",
            "Освіта",
            "Медицина",
            "Продукти",
            "Розваги",
            "Волонтерство",
            "Гардероб",
            "Інше"
        };

        // Відображає стан темної теми
        private bool isDarkTheme = false;

        // Змінні, що зберігають світлу тему
        private Brush origWindowBg;
        private Brush origTextFg;
        private Brush origListBg;
        private Brush origListFg;

        public MainWindow()
        {
            Instance = this;
            

            InitializeComponent();
            // Ініціалізація колекції транзакцій
            Transactions_ = new ObservableCollection<Transaction>();

            using (var db = new AppDbContext())
            {
                // Створення БД, якщо не існує
                db.Database.EnsureCreated();

                // Завантаження усіх транзакцій з БД
                var savedTransactions = db.Transactions.ToList();
                foreach (var transaction in savedTransactions)
                {
                    Transactions_.Add(transaction);
                    Balance += transaction.Amount;
                }
            }
            // Прив'язка ListView до колекції Transactions
            TransactionHistoryListView.ItemsSource = Transactions_;
            // Відображення початкового балансу
            BalanceTextBlock.Text = Balance.ToString("0.00 грн");

            TransactionHistoryListView.SelectionChanged +=
                TransactionHistoryListView_SelectionChanged;

        }
        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            // Отримання вибраного типу транзакції з ComboBox
            string type = ((ComboBoxItem)TransactionTypeCombobox.SelectedItem
                )?.Content.ToString();
            // Отримання вибраної категорії з ComboBox
            string category = ((ComboBoxItem)CategoryComboBox.SelectedItem
                )?.Content.ToString();
            // Отримання введеної користувачем суми
            string amountText = AmountTextBox.Text;
            // Отримання вибраної дати з DataPicker
            DateTime? date = TransactionDatePicker.SelectedDate;

            bool canBeAdded = true; // Перевіряє можливість додати транзакцію

            // Перевірка що всі поля заповнені
            if (string.IsNullOrEmpty(type) ||
                string.IsNullOrEmpty(category) ||
                string.IsNullOrEmpty(amountText) ||
                !date.HasValue)
            {
                MessageBox.Show("Введіть корректну суму.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                canBeAdded = false;
            }
            // Спроба перетворити введену суму на значення типу decimal
            if (!decimal.TryParse(amountText, out decimal amount))
            {
                MessageBox.Show("Введіть коректну суму", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                canBeAdded = false;
            }
            if (type == "Витрати")
            {
                amount = -amount;
            }

            if (canBeAdded)
            {
                // Створення нового об'єкта Transaction з наданими даними
                Transaction transaction = new Transaction
                {
                    Date = date.Value.ToString("dd.MM.yyyy"),
                    Type = type,
                    Category = category,
                    Amount = amount,
                };

                // Збереження транзакції в БД
                using (var db = new AppDbContext())
                {
                    db.Transactions.Add(transaction);
                    db.SaveChanges();
                }

                // Додавання транзакції до колекції
                Transactions_.Add(transaction);
                // Оновлення балансу з урахуванням нової транзакції
                Balance += amount;
                // Оновлення BalabceTextBlock для відображуння нового балансу
                BalanceTextBlock.Text = Balance.ToString("0.00 грн");
                // Очищення полів введення для наступного запису
                AmountTextBox.Clear();
                TransactionDatePicker.SelectedDate = null;
            }
        }
        private void Exit_Button(object sender, RoutedEventArgs e)
        {
            // Закриття застосунку
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process unityGame = new Process();
            var exePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Clicker", "ВТ-18-00-C#.exe");
            unityGame.StartInfo.FileName = exePath;
            //unityGame.StartInfo.FileName = @"..\..\Clicker\ВТ-18-00-C#.exe";
            unityGame.StartInfo.UseShellExecute = false;
            unityGame.Start();
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка, чи вибрано транзакцію
            if (TransactionHistoryListView.SelectedItem is Transaction transaction)
            {
                // Відображення вікна підтвердженян видалення
                MessageBoxResult result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити вибрану транзакцію?",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Видалення транзакції з бази даних
                    using (var db = new AppDbContext())
                    {
                        db.Attach(transaction);
                        db.Transactions.Remove(transaction);
                        db.SaveChanges();
                    }
                }

                // Видалення транзакції з колекції, що оновлює UI
                Transactions_.Remove(transaction);

                // Оновлення балансу
                Balance -= transaction.Amount;
                BalanceTextBlock.Text = Balance.ToString("0.00 грн");
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть транзакцію для видалення.",
                    "Інформація",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void TransactionTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCategoryComboBox();
        }

        private void UpdateCategoryComboBox()
        {
            // Очистити існуючі елементи
            CategoryComboBox.Items.Clear();

            // Визначити обраний тип транзакції
            string selectedType = ((ComboBoxItem)
                TransactionTypeCombobox.SelectedItem)?.Content.ToString();

            // Заповнити ComboBox відповідно до типу
            if (selectedType == "Доходи")
            {
                foreach (var category in incomeCategories)
                {
                    CategoryComboBox.Items.Add(new ComboBoxItem() { Content = category });
                }
            }
            else if (selectedType == "Витрати")
            {
                foreach (var category in expenseCategories)
                {
                    CategoryComboBox.Items.Add(new ComboBoxItem { Content = category });
                }
            }
            // (Опціонально) встановити перший елемент як вибраний
            if (CategoryComboBox.Items.Count > 0)
            {
                CategoryComboBox.SelectedIndex = 0;
            }
        }

        private void Change_Theme_Button(object sender, RoutedEventArgs e)
        {
            // 1-й виклик - запам'ятовуємо базові кольори
            if (origWindowBg == null)
            {
                origWindowBg = this.Background;
                origTextFg = this.Title_Text.Foreground;
                origListBg = TransactionHistoryListView.Background;
                origListFg = TransactionHistoryListView.Foreground;
            }
            isDarkTheme = !isDarkTheme; // Інвертуємо значення змінної
            // Dark theme
            if (isDarkTheme)
            {
                // Загальний колір фону
                this.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                // Заголовок
                this.Title_Text.Foreground = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));
                // Группи
                this.Group_1.Foreground = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));

                this.Group_2.Foreground = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));

                // Группи
                TransactionHistoryListView.Background = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));
                TransactionHistoryListView.Foreground = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));

                // Зміна кольору балансу
                BalanceTextBlock.Foreground = new SolidColorBrush(
                    Color.FromRgb(200, 200, 200));
                // Змінити колір для всіх TextBlock в StackPanel
                foreach (var child in LogicalTreeHelper.GetChildren(
                    Group_1.Content as StackPanel))
                {
                    if (child is TextBlock)
                    {
                        (child as TextBlock).Foreground = new SolidColorBrush(
                            Color.FromRgb(200, 200, 200));
                    }
                }

                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                theme.SetPrimaryColor((Color)ColorConverter.ConvertFromString("#23AF00"));
                paletteHelper.SetTheme(theme);
            }
            else
            {
                this.Background = origWindowBg;
                this.Title_Text.Foreground = origTextFg;
                this.Group_1.Foreground = this.Group_2.Foreground = 
                    BalanceTextBlock.Foreground = origTextFg;

                TransactionHistoryListView.Background = origListBg;
                TransactionHistoryListView.Foreground = origListFg;

                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetPrimaryColor((
                    Color)ColorConverter.ConvertFromString("#673ab7"));
                paletteHelper.SetTheme(theme);
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            // Отримуємо заголовок стовпця, по якому клікнули
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            // Перевіряємо, що це дійсно заголовок і не поронжній об'єкт
            if (headerClicked != null && headerClicked.Role !=
                GridViewColumnHeaderRole.Padding)
            {
                ListSortDirection direction;

                // Якщо клікнули на новий стовпець - сортуємо за зростанням
                if (headerClicked != _lastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    // Якщо клікнули на той самий стовпець - змінюємо
                    // напрямок сортування
                    direction = _lastDirection == ListSortDirection.Ascending ?
                        ListSortDirection.Descending : ListSortDirection.Ascending;
                }
                // Отримуємо ім'я властивості для сортування
                var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                string sortBy = columnBinding?.Path.Path ??
                    headerClicked.Column.Header.ToString();
                // Виконуємо сортування
                Sort(sortBy, direction);
                // Запам'ятовуємо поточний стано для наступного кліку
                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
        }

        // Метод що виконує сортування
        private void Sort(string sortBy, ListSortDirection direction)
        {
            // Отримуємо представлення колекції для сортування
            ICollectionView dataView = CollectionViewSource.GetDefaultView(
                TransactionHistoryListView.ItemsSource);
            // Очищуємо попередні сортування
            dataView.SortDescriptions.Clear();
            // Додаємо нове сортування
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            // Оновлюємо відображення
            dataView.Refresh();
        }

        private void TransactionHistoryListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            decimal selectedSum = 0m;

            foreach (Transaction t in
                TransactionHistoryListView.SelectedItems)
            {
                selectedSum += t.Amount;
                SelectedSumTextBlock.Text = selectedSum.ToString(
                    "0.00 грн");
            }
        }
        private void SaveToExcel_Click(object sender, RoutedEventArgs e)
        {
            // Вікриваємо діалог для вибору шляху та імені файлу
            var dlg = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx", // Формат файлу
                FileName = "transactions.xlsx" // Початкове ім'я файлу
            };
            // Якщо користувач відмінив діалог, виходимо
            if (dlg.ShowDialog() != true) return;
            // Стврюємо нову книгу Excel
            using (var wb = new XLWorkbook())
            {
                // Додаємо робочий аркуш з назвою
                var ws = wb.Worksheets.Add("Transactions");

                // Заповнюємо шапку таблиці
                ws.Cell(1, 1).Value = "Id";
                ws.Cell(1, 2).Value = "Date";
                ws.Cell(1, 3).Value = "Type";
                ws.Cell(1, 4).Value = "Category";
                ws.Cell(1, 5).Value = "Amount";

                int row = 2; // Початковий рядок для даних (після заголовка)
                foreach (var t in Transactions_)
                {
                    // Заповнюємо кожен рядок аркуша данними транзакції
                    ws.Cell(row, 1).Value = t.Id;
                    ws.Cell(row, 2).Value = t.Date;
                    ws.Cell(row, 3).Value = t.Type;
                    ws.Cell(row, 4).Value = t.Category;
                    ws.Cell(row, 5).Value = t.Amount;
                    row++;
                }
                wb.SaveAs(dlg.FileName);
            }
            MessageBox.Show("Excel-файл збережено!");
        }
        private void SaveToCSV_Click(object sender, RoutedEventArgs e)
        {
            // Налаштовуємо діалог збереження файлу
            var dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",     // Дозволені формати
                FileName = "transactions.csv"              // Початкове ім'я файлу
            };
            // Якщо користувач натиснув "Скасувати", припиняємо виконання
            if (dlg.ShowDialog() != true) return;

            // Будуємо вміст CSV за допомогою StringBuilder
            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Type,Category,Amount");  // Додаємо заголовок стовпців
            foreach (var t in Transactions_)
            {
                // Для кожної транзакції формуємо рядок та додаємо в буфер
                sb.AppendLine($"{t.Id},{t.Date},{t.Type},{t.Category},{t.Amount}");
            }

            // Записуємо зібраний текст у файл у кодуванні UTF-8
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            // Повідомляємо користувача про успішне збереження
            MessageBox.Show("CSV збережено!");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // 2.1. Відкриваємо діалог вибору файлу CSV
            var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
            if (dlg.ShowDialog() != true)
                return;  // користувач скасував — нічого не робимо

            // 2.2. Спершу повністю очищаємо старі дані
            ClearData();

            // 2.3. Зчитуємо всі рядки CSV (UTF-8)
            var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8);

            // 2.4. Перебираємо всі рядки, пропускаючи перший (заголовок)
            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');     // розбиваємо на поля по комі
                if (parts.Length < 5) continue;  // якщо мало стовпців — пропускаємо

                // 2.5. Створюємо новий об’єкт Transaction, парсимо поля
                var t = new Transaction
                {
                    Id = int.Parse(parts[0]),
                    Date = parts[1],
                    Type = parts[2],
                    Category = parts[3],
                    Amount = decimal.Parse(parts[4], CultureInfo.InvariantCulture)
                };
                // 2.6. Додаємо в колекцію для UI
                Transactions_.Add(t);
            }

            // 2.7. Масово зберігаємо нові записи в БД
            using (var db = new AppDbContext())
            {
                db.Transactions.AddRange(Transactions_);
                db.SaveChanges();
            }

            // 2.8. Перераховуємо та відображаємо новий баланс
            Balance = Transactions_.Sum(x => x.Amount);
            BalanceTextBlock.Text = Balance.ToString("0.00 грн");

            // 2.9. Сповіщаємо користувача про успішний імпорт
            MessageBox.Show("CSV імпортовано!");
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // 3.1. Відкриваємо діалог вибору файлу Excel
            var dlg = new OpenFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx" };
            if (dlg.ShowDialog() != true)
                return;  // користувач скасував

            // 3.2. Очищаємо старі дані перед завантаженням нових
            ClearData();

            // 3.3. Відкриваємо книгу та беремо перший аркуш
            using var wb = new XLWorkbook(dlg.FileName);
            var ws = wb.Worksheet(1);

            // 3.4. Перебираємо всі заповнені рядки, пропускаючи перший (заголовок)
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                // 3.5. Створюємо Transaction з даних комірок
                var t = new Transaction
                {
                    Id = row.Cell(1).GetValue<int>(),
                    Date = row.Cell(2).GetValue<string>(),
                    Type = row.Cell(3).GetValue<string>(),
                    Category = row.Cell(4).GetValue<string>(),
                    Amount = row.Cell(5).GetValue<decimal>()
                };
                Transactions_.Add(t);  // додаємо до колекції UI
            }

            // 3.6. Зберігаємо всі нові записи в БД
            using (var db = new AppDbContext())
            {
                db.Transactions.AddRange(Transactions_);
                db.SaveChanges();
            }

            // 3.7. Оновлюємо баланс і відображаємо його
            Balance = Transactions_.Sum(x => x.Amount);
            BalanceTextBlock.Text = Balance.ToString("0.00 грн");

            // 3.8. Повідомляємо про успіх
            MessageBox.Show("Excel імпортовано!");
        }

        private void ClearData()
        {
            // 1.1. Через контекст EF Core видаляємо всі записи з таблиці Transactions
            using (var db = new AppDbContext())
            {
                db.Transactions.RemoveRange(db.Transactions);
                db.SaveChanges();
            }

            // 1.2. Очищаємо колекцію, що прив’язана до UI (ObservableCollection)
            Transactions_.Clear();

            // 1.3. Скидаємо баланс і оновлюємо відповідний текстовий блок
            Balance = 0;
            BalanceTextBlock.Text = "0.00 грн";
        }
    }  
}
