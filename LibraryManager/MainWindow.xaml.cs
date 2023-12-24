using LibraryManager.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
            SetDataToComboBoxes();
            SizeChanged += MainWindow_SizeChanged;
        }
        private async void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await Task.Delay(100); // Odroczenie przetwarzania na później, aby uniknąć natychmiastowych zmian

            if (MainGrid.RowDefinitions.Count > 0 && ActualHeight > 0)
            {
                double availableHeight = ActualHeight - MainGrid.Margin.Top - MainGrid.Margin.Bottom - MainGrid.RowDefinitions[0].ActualHeight;

                if (availableHeight > 0)
                {
                    int rowHeight = 25; // Wysokość wiersza

                    int rowsCount = (int)(availableHeight / rowHeight);

                    AllBooksDataGrid.MaxHeight = rowsCount * rowHeight;
                }
            }
        }



        public async void SetDataToComboBoxes()
        {
            await DisplayAuthors();
            await DisplayPublishers();
            await DisplayGenres();
        }
        async Task DisplayAuthors()
        {
            var authors = await _database.GetAuthors();
            NewBookAuthor.ItemsSource = authors;
            NewBookAuthor.DisplayMemberPath = "Name"; 
            NewBookAuthor.SelectedValuePath = "Id";  
        }

        async Task DisplayPublishers()
        {
            var publishers = await _database.GetPublishers();
            NewBookPublisher.ItemsSource = publishers;
            NewBookPublisher.DisplayMemberPath = "Nazwa";
            NewBookPublisher.SelectedValuePath = "Id";
        }
        async Task DisplayGenres()
        {
            var genres = await _database.GetGenres();
            NewBookGenre.ItemsSource = genres;
            NewBookGenre.DisplayMemberPath = "Nazwa";
            NewBookGenre.SelectedValuePath = "Id";
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
            int authorId = (int)NewBookAuthor.SelectedValue;
            int publisherId = (int)NewBookPublisher.SelectedValue;
            int genreId = (int)NewBookGenre.SelectedValue;
            string title = NewBookTitle.Text;
            string date = NewBookDate.Text;
            await _database.AddBook(title, date, authorId, publisherId, genreId);
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
