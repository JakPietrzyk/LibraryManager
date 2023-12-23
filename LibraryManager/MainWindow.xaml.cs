using LibraryManager.Dtos;
using System;
using System.Collections.Generic;
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

namespace projekt
{
    public partial class MainWindow : Window
    {
        Database _database = new Database();
        public MainWindow()
        {
            InitializeComponent();
            DisplayAuthors();
        }
        async Task DisplayAuthors()
        {
            var authors = await _database.GetAuthors();
            NewBookAuthor.ItemsSource = authors;
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void DodajKsiazke_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Visible;
        }

        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Visible;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
        }

        private async void Dodaj_Click(object sender, RoutedEventArgs e)
        {
            string title = NewBookTitle.Text;
            string date = NewBookDate.Text;
            string author = NewBookAuthor.Text;
            string publisher = NewBookPublisher.Text;
            string genre = NewBookGenre.Text;
            //ZAMIAST WPISYWAC AUTORA Z KSIAZKI TO SELECT NA AUTOROW I POKAZAC JACY SA W BAZIE 
            await _database.AddBook(title, date, author, publisher, genre);
            MainGrid.Visibility = Visibility.Visible;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
        }

        private async void PokazKsiazki_Click(object sender, RoutedEventArgs e)
        {
            var books = await _database.GetAllBooks(); 

            AllBooksDataGrid.ItemsSource = books;
            AllBooksDataGrid.Visibility = Visibility.Visible;
        }

        private async void PokazWypozyczenia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<RentalDto> rentals = await _database.GetAllRentals();
                AllBooksDataGrid.ItemsSource = rentals;
                AllBooksDataGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
