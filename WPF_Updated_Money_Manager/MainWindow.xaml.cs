using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Microsoft.VisualBasic;

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
    }  
}
