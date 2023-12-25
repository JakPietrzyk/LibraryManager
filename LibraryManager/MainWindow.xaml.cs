using LibraryManager.Dtos;
using System;
using System.Collections.Generic;
using System.Data;
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
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WyszukiwaneKsiazkiGrid.Height = ActualHeight - 200;
        }



        public async void SetDataToComboBoxes()
        {
            await DisplayAuthors();
            await DisplayPublishers();
            await DisplayGenres();
            await WyswietlCzytelnikow();
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
            var genresSortowanie = new List<DziedzinaDto>();
            genresSortowanie.AddRange(genres);
            NewBookGenre.ItemsSource = genres;
            NewBookGenre.DisplayMemberPath = "Nazwa";
            NewBookGenre.SelectedValuePath = "Id";
            genresSortowanie.Insert(0, new DziedzinaDto()
            {
                Id = 0,
                Nazwa = "<Brak>"
            });
            SortowaniePoDziedzinie.ItemsSource = genresSortowanie;
            SortowaniePoDziedzinie.SelectedIndex = 0;

            SortowaniePoDziedzinie.DisplayMemberPath = "Nazwa";
            SortowaniePoDziedzinie.SelectedValuePath = "Id";
        }
        async Task WyswietlCzytelnikow()
        {
            var czytelnicy = await _database.GetCzytelnicy();

            CzytelnicyComboBox.ItemsSource = czytelnicy.Select(c => new
            {
                Id = c.Id,
                PelneImieNazwisko = $"{c.Imie} {c.Nazwisko}"
            }).ToList();

            CzytelnicyComboBox.DisplayMemberPath = "PelneImieNazwisko";
            CzytelnicyComboBox.SelectedValuePath = "Id";
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
            try
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
            catch(Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }

        }

        private async void PokazKsiazki_Click(object sender, RoutedEventArgs e)
        {
            var books = await _database.GetAllBooks(); 

            //AllBooksDataGrid.ItemsSource = books;
            //AllBooksDataGrid.Visibility = Visibility.Visible;
        }

        private async void PokazWypozyczenia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<RentalDto> rentals = await _database.GetAllRentals();
                //AllBooksDataGrid.ItemsSource = rentals;
                //AllBooksDataGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void PokazWyszukiwarke_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
            WyszukiwarkaKsiazek.Visibility = Visibility.Visible;
        }
        private async void WyszukajKsiazke_Click(object sender, RoutedEventArgs e)
        {
            var tytulDoWyszukania = WyszukiwanyTytul.Text;
            List<KsiazkaDto> books;
            if (SortowaniePoDziedzinie.SelectedIndex > 0)
                books = await _database.GetAllBooks((int)SortowaniePoDziedzinie.SelectedValue);
            else if (!string.IsNullOrEmpty(tytulDoWyszukania))
                books = await _database.GetAllBooks(null, tytulDoWyszukania);
            else
                books = await _database.GetAllBooks();

            WyszukiwaneKsiazkiGrid.ItemsSource = books;
            WyszukiwaneKsiazkiGrid.Visibility = Visibility.Visible;
        }
        
        private void AnulujWyszukiwanie_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            MainGrid.Visibility = Visibility.Visible;

        }
        private async void WyszukiwaneKsiazkiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var wybranaKsiazka = (KsiazkaDto)WyszukiwaneKsiazkiGrid.SelectedItem;
                var czyDostepna = await _database.GetAvailability(wybranaKsiazka.Id);
                string dostepnosc = "";
                if(czyDostepna)
                {
                    dostepnosc = "Dostepna";
                    PrzyciskWypozycz.Visibility = Visibility.Visible;
                }
                else
                {
                    dostepnosc = "Brak w bibliotece";
                    PrzyciskWypozycz.Visibility = Visibility.Collapsed;
                }
                WybraneWypozyczenieTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nRok: {wybranaKsiazka.RokWydania}\nStan: {dostepnosc}";
            }
            else
            {
                PrzyciskWypozycz.Visibility = Visibility.Collapsed;
            }
        }

        private void PrzyciskWypozycz_Click(object sender, RoutedEventArgs e)
        {

        }
        private void CollapseAll()
        {
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
            WyszukiwarkaKsiazek.Visibility = Visibility.Collapsed;
            RejestracjaGrid.Visibility = Visibility.Collapsed;
            LogowanieGrid.Visibility = Visibility.Collapsed;
        }
        private void Zarejestruj_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            RejestracjaGrid.Visibility = Visibility.Visible;
        }
        private async void PrzyciskZarejestruj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string imie = NowyCzytelnikImie.Text;
                string nazwisko = NowyCzytelnikNazwisko.Text;
                string adres = NowyCzytelnikAdres.Text;
                string email = NowyCzytelnikEmail.Text;
                string telefon = NowyCzytelnikTelefon.Text;
                await _database.DodajCzytelnika(imie, nazwisko, adres, email, telefon);
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {

            }
        }
        private void PrzyciskZaloguj_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            MainGrid.Visibility = Visibility.Visible;
        }
        private void PokazLogowanie_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            LogowanieGrid.Visibility = Visibility.Visible;
        }
    }
}
