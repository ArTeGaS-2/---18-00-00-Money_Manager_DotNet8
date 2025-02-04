using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Updated_Money_Manager
{
    internal class AppDbContext : DbContext
    {
        // DbSet для зберігання транзакцій
        public DbSet<Transaction> Transactions { get; set; }

        // Методи конфігурації - вказуємо, що будемо використовувати SQLite
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=transactions.db");
        }
    }
}
