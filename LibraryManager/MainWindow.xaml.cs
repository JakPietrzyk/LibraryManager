using LibraryManager;
using LibraryManager.Dtos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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
            await DisplayBooks();
            await CreateComboBoxes();
        }
        async Task DisplayBooks()
        {
            var books = await _database.GetAllBooks();
            NowyEgzemplarzKsiazka.ItemsSource = books;
            NowyEgzemplarzKsiazka.DisplayMemberPath = "Tytul";
            NowyEgzemplarzKsiazka.SelectedValuePath = "Id";
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

            NowaDziedzinaNadrzednaId.ItemsSource = genresSortowanie;
            NowaDziedzinaNadrzednaId.SelectedIndex = 0;

            NowaDziedzinaNadrzednaId.DisplayMemberPath = "Nazwa";
            NowaDziedzinaNadrzednaId.SelectedValuePath = "Id";
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

        private List<ComboBox> comboBoxes = new List<ComboBox>();

        private async Task CreateComboBoxes(int parentId = 0, StackPanel parentPanel = null)
        {
            List<DziedzinaDto> dziedziny = await _database.GetGenres();

            dziedziny.Insert(0, new DziedzinaDto { Id = 0, Nazwa = "" });

            StackPanel currentPanel;
            if (parentPanel == null)
            {
                currentPanel = MainStackPanel;
                foreach (var cb in comboBoxes)
                {
                    currentPanel.Children.Remove(cb); 
                }
                comboBoxes.Clear();
            }
            else
            {
                currentPanel = parentPanel;
            }

            var comboBox = new ComboBox
            {
                ItemsSource = dziedziny.Where(d => !comboBoxes.Any(cb => (int)cb.SelectedValue == d.Id)),
                DisplayMemberPath = "Nazwa",
                SelectedValuePath = "Id",
            };
            comboBox.SelectionChanged += async (sender, e) =>
            {
                var childComboBox = sender as ComboBox;
                var selectedDziedzinaId = (int)childComboBox.SelectedValue;

                var isLastComboBox = comboBoxes.LastOrDefault() == childComboBox;
                if (isLastComboBox && selectedDziedzinaId != 0)
                {
                    await CreateComboBoxes(selectedDziedzinaId, childComboBox.Parent as StackPanel);
                }
            };

            comboBoxes.Add(comboBox);
            currentPanel.Children.Add(comboBox);

            foreach (var cb in comboBoxes)
            {
                cb.Visibility = Visibility.Visible;
            }
        }

        private async void DodajKsiazke_Click(object sender, RoutedEventArgs e)
        {
            await CreateComboBoxes();
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Visible;
        }
        private void DodajAutoraShow_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            DodajAutoraGrid.Visibility = Visibility.Visible;
        }

        private void DodajWydawnictwoShow_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            DodajWydawnictwoGrid.Visibility = Visibility.Visible;
        }
        private void DodajDziedzineShow_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            DodajDziedzineGrid.Visibility = Visibility.Visible;
        }
        private void DodajEgzemplarzShow_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            DodajEgzemplarzGrid.Visibility = Visibility.Visible;
        }
        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            MainGrid.Visibility = Visibility.Visible;
        }

        private async void Dodaj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var genreIds = comboBoxes.Where(cb => cb.SelectedValue != null && (int)cb.SelectedValue != 0)
                                         .Select(cb => (int)cb.SelectedValue)
                                         .ToList();
                if (genreIds.Any())
                {
                    await _database.AddGenreWithSubTypes(genreIds);
                }

                int authorId = (int)NewBookAuthor.SelectedValue;
                int publisherId = (int)NewBookPublisher.SelectedValue;
                int genreId = (int)NewBookGenre.SelectedValue;
                string title = NewBookTitle.Text;
                string date = NewBookDate.Text;
                await _database.AddBook(title, date, authorId, publisherId, genreId);
                SetDataToComboBoxes();
                MainGrid.Visibility = Visibility.Visible;
                DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }

        }
        private async void DodajAutora_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string authorName = NewAutorName.Text;
                string authorSurname= NewAutorSurname.Text;
                await _database.AddAuthor(authorName, authorSurname);
                SetDataToComboBoxes();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }
        }
        private async void DodajWydawnictwo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = NoweWydawnictwoNazwa.Text;
                string street = NoweWydawnictwoUlica.Text;
                string apartmentNumber = NoweWydawnictwoNumerBudynku.Text;
                string postcode = NoweWydawnictwoKodPocztowy.Text;
                string city = NoweWydawnictwoMiasto.Text;
                string adress = $"{street} {apartmentNumber} {postcode} {city}";
                await _database.AddPublisher(name, adress);
                SetDataToComboBoxes();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }
        }
        private async void DodajDziedzine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = NowaDziedzinaNazwa.Text;
                int genreId = (int)NowaDziedzinaNadrzednaId.SelectedValue;
                await _database.AddGenre(name, genreId);
                SetDataToComboBoxes();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }
        }
        private bool ValidateISBN(string text)
        {
            string pattern = @"^(?:\d{3}-)?\d{1,5}-\d{1,7}-\d{1,7}-[\dX]$";
            Regex regex = new Regex(pattern);

            if (!regex.IsMatch(text))
            {
                InformacjaOBledzie.Text = "Nieprawidłowy format numeru ISBN! Przykład: 978-3-16-148410-0";
                return false;
            }
            else
            {
                InformacjaOBledzie.Text = "";
                return true;
            }
        }
        private async void DodajEgzemplarz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int bookId = (int)NowyEgzemplarzKsiazka.SelectedValue;
                string isbn = NowyEgzemplarzISBN.Text;
                if (!ValidateISBN(isbn))
                    throw new InvalidDataException();
                await _database.AddCopyOfBook(bookId, isbn);
                SetDataToComboBoxes();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }
        }
        private void NoweEgzemplarzeIlosc_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string proposedText = textBox.Text + e.Text;

            string pattern = @"^(?:\d{3}-)?\d{1,5}-\d{1,7}-\d{1,7}-[\dX]$";
            Regex regex = new Regex(pattern);

            if (!regex.IsMatch(proposedText))
            {
                e.Handled = true;
                InformacjaOBledzie.Text = "Nieprawidłowy format numeru ISBN! Przykład: 978-3-16-148410-0";
            }
            else
            {
                InformacjaOBledzie.Text = "";
            }
        }

        private async void PokazKsiazki_Click(object sender, RoutedEventArgs e)
        {
            var books = await _database.GetAllBooks(); 

            //AllBooksDataGrid.ItemsSource = books;
            //AllBooksDataGrid.Visibility = Visibility.Visible;
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
        private async Task AktualizujInformacjeOKsiazce()
        {
            var wybranaKsiazka = (KsiazkaDto)WyszukiwaneKsiazkiGrid.SelectedItem;
            var czyDostepna = await _database.GetAvailability(wybranaKsiazka.Id);
            string dostepnosc = "";
            var ocena = await _database.GetRating(wybranaKsiazka.Id);
            Brush color = Brushes.Black;
            if (czyDostepna)
            {
                dostepnosc = "Dostepna";
                PrzyciskWypozycz.Visibility = Visibility.Visible;
            }
            else
            {
                dostepnosc = "Brak w bibliotece";
                PrzyciskWypozycz.Visibility = Visibility.Collapsed;
                color = Brushes.Red;
            }
            if (ocena >= 0)
                WybraneWypozyczenieTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nRok: {wybranaKsiazka.RokWydania}\nStan: {dostepnosc}\nOcena: {ocena}/5";
            else
                WybraneWypozyczenieTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nRok: {wybranaKsiazka.RokWydania}\nStan: {dostepnosc}\nOcena: brak ocen";

            WybraneWypozyczenieTextBlock.Foreground = color;
        }
        private async void WyszukiwaneKsiazkiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                await AktualizujInformacjeOKsiazce();
            }
            else
            {
                PrzyciskWypozycz.Visibility = Visibility.Collapsed;
            }
        }

        private async void PrzyciskWypozycz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ksiazkaId = ((KsiazkaDto)WyszukiwaneKsiazkiGrid.SelectedItem).Id;
                var czytelnikId = (int)CzytelnicyComboBox.SelectedValue;
                var egzemplarzId = await _database.GetEgzemplarzKsiazki(ksiazkaId);
                await _database.WypozyczEgzemplarzKsiazki(czytelnikId, egzemplarzId);
                SetDataToComboBoxes();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("Proszę się zalogować.");
                CollapseAll();
                LogowanieGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
            }
        }
        private void CollapseAll()
        {
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
            WyszukiwarkaKsiazek.Visibility = Visibility.Collapsed;
            RejestracjaGrid.Visibility = Visibility.Collapsed;
            LogowanieGrid.Visibility = Visibility.Collapsed;
            WypozyczeniaGrid.Visibility = Visibility.Collapsed;
            DodajAutoraGrid.Visibility = Visibility.Collapsed;
            DodajDziedzineGrid.Visibility = Visibility.Collapsed;
            DodajWydawnictwoGrid.Visibility = Visibility.Collapsed;
            DodajEgzemplarzGrid.Visibility = Visibility.Collapsed;
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
        private async void WypozyczoneKsiazki_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var wybranaKsiazka = (RentalDto)WypozyczoneKsiazki.SelectedItem;
                PrzyciskZwrotEgzemplarza.Visibility = Visibility.Visible;
                WybranyZwrotTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nRok: {wybranaKsiazka.RokWydania}";
            }
            else
            {
                PrzyciskZwrotEgzemplarza.Visibility = Visibility.Collapsed;
            }
        }
        private async Task ShowRentals()
        {
            try
            {
                List<RentalDto> rentals = new();
                var czytelnikId = (int)CzytelnicyComboBox.SelectedValue;
                if (czytelnikId > 0)
                    rentals = await _database.GetAllRentals(czytelnikId);
                CollapseAll();
                WypozyczeniaGrid.Visibility = Visibility.Visible;
                WypozyczoneKsiazki.ItemsSource = rentals;
                WypozyczoneKsiazki.DisplayMemberPath = "Tytul";
                WypozyczoneKsiazki.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę się zalogować.");
                CollapseAll();
                LogowanieGrid.Visibility = Visibility.Visible;
            }
        }

        private async void PokazWypozyczenia_Click(object sender, RoutedEventArgs e)
        {
            await ShowRentals();
        }
        private int RateBook()
        {
            BookReviewWindow reviewWindow = new BookReviewWindow();
            bool? result = reviewWindow.ShowDialog();

            if (result == true)
            {
                int selectedRating = reviewWindow.SelectedRating;
                MessageBox.Show($"Dziękujemy za wystawienie oceny: {selectedRating}");
                return selectedRating;
            }
            return -1;
        }
        private async void PrzyciskZwrotEgzemplarza_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wypozyczenieId = ((RentalDto)WypozyczoneKsiazki.SelectedItem)?.Id;
                var czytelnik_id = (int)CzytelnicyComboBox.SelectedValue;
                if (wypozyczenieId != null)
                {
                    await _database.DodajDateZwrotu((int)wypozyczenieId);
                    await ShowRentals();
                    int rate = RateBook();
                    if (rate > 0)
                    {
                        await _database.DodajOcene((int)wypozyczenieId, czytelnik_id ,rate);
                        await AktualizujInformacjeOKsiazce();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
