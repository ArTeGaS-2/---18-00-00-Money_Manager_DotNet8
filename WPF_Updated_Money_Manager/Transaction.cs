using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Updated_Money_Manager
{
    public class Transaction
    {
        public int Id { get; set; }

        // Дата транзакції
        public string Date { get; set; }
        // Тип транзакції (Дохід або Витрата)
        public string Type { get; set; }
        // Категорія транзакції
        public string Category { get; set; }
        // Сума транзакції
        public decimal Amount { get; set; }
    }
}
