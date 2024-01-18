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
using System.Threading;
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
            InitializeComponentsAndGetDataFromDatabase();
            IsConnectionAlive();
            SizeChanged += MainWindow_SizeChanged;
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                WyszukiwaneKsiazkiGrid.Height = ActualHeight - 275;

            }
            catch(Exception ex)
            {
                WyszukiwaneKsiazkiGrid.Height = 1;
            }
        }
        public async Task DropDatabaseAndCreateFromFile()
        {
            await _database.CreateDatabase();
            await SetDataToComboBoxes();
            await CreateRankings();
        }
        public async void InitializeComponentsAndGetDataFromDatabase()
        {
            await SetDataToComboBoxes();
            await CreateRankings();

        }
        public void IsConnectionAlive()
        {
            if(_database.IsConnection())
            {
                    DatabaseConnectionStatusTB.Text = "Połączenie z bazą danych: POŁĄCZONO";
            }
            else
            {
                DatabaseConnectionStatusTB.Text = "Połączenie z bazą danych: UTRACONO POŁĄCZENIE!";
            }
        }

        public async Task CreateRankings()
        {
            RankingiStackPanel.Children.Clear();
            await CreateTopBookTextBlock();
            await CreateTopReaderOfMonthTextBlock();
        }
        public async Task CreateTopBookTextBlock()
        {
            TextBlock topBooksTextBlock = new TextBlock();
            topBooksTextBlock.Text = "Top 3 książki:";
            RankingiStackPanel.Children.Add(topBooksTextBlock);

            var books = await _database.GetTopBooks();

            int index = 1;
            foreach (var book in books.Take(3))
            {
                TextBlock bookInfoTextBlock = new TextBlock();
                bookInfoTextBlock.Text = $"\t{index}: Ocena: {book.Opinia.ToString("0.0")}. Książka: {book.Tytul}, {book.Autor.FullName}, {book.RokWydania.Year}";
                RankingiStackPanel.Children.Add(bookInfoTextBlock);
                index++;
            }
        }
        public async Task CreateTopReaderOfMonthTextBlock()
        {
            TextBlock topReaderTextBlock = new TextBlock();
            topReaderTextBlock.Text = "Najbardziej aktywny czytelnik miesiąca:";
            RankingiStackPanel.Children.Add(topReaderTextBlock);
            var reader = await _database.GetTopReader();
            if (reader != null)
            {
                TextBlock topReaderInfoTextBlock = new TextBlock();
                topReaderInfoTextBlock.Text = $"\t{reader.Nazwisko} {reader.Imie}\nIlość przeczytanych książek: {reader.IloscWypozyczen}";
                RankingiStackPanel.Children.Add(topReaderInfoTextBlock);
            }
        }
        public async Task SetDataToComboBoxes()
        {
            await DisplayPublishers();
            await DisplayGenres();
            //await WyswietlCzytelnikow();
            await DisplayBooks();
            await CreateComboBoxes();
            await CreateComboBoxesAuthors();
        }
        async Task DisplayBooks()
        {
            var books = await _database.GetAllBooks();
            NowyEgzemplarzKsiazka.ItemsSource = books;
            NowyEgzemplarzKsiazka.DisplayMemberPath = "Tytul";
            NowyEgzemplarzKsiazka.SelectedValuePath = "Id";
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
            var genres = await _database.GetGenresDistinct();
            var genresSortowanie = new List<DziedzinaDto>();
            genresSortowanie.AddRange(genres);
            genresSortowanie.Insert(0, new DziedzinaDto()
            {
                Nazwa = ""
            });
            SortowaniePoDziedzinie.ItemsSource = genresSortowanie;
            SortowaniePoDziedzinie.SelectedIndex = 0;

            SortowaniePoDziedzinie.DisplayMemberPath = "Nazwa";

            NowaDziedzinaNadrzednaId.ItemsSource = genresSortowanie;
            NowaDziedzinaNadrzednaId.SelectedIndex = 0;

            NowaDziedzinaNadrzednaId.DisplayMemberPath = "Nazwa";
        }
        async Task WyswietlCzytelnikow()
        {
            var czytelnicy = await _database.GetCzytelnicy();

            CzytelnicyComboBox.ItemsSource = czytelnicy;

            CzytelnicyComboBox.DisplayMemberPath = "PelneImieNazwisko";
            CzytelnicyComboBox.SelectedValuePath = "Id";
        }



        private List<ComboBox> comboBoxesGenres = new List<ComboBox>();
        private async Task CreateComboBoxes(string nazwa = "", StackPanel parentPanel = null)
        {
            List<DziedzinaDto> dziedziny = await _database.GetGenresDistinct();

            dziedziny.Insert(0, new DziedzinaDto { Id = 0, Nazwa = "" });

            StackPanel currentPanel;
            if (parentPanel == null)
            {
                currentPanel = MainStackPanel;
                foreach (var cb in comboBoxesGenres)
                {
                    currentPanel.Children.Remove(cb);
                }
                comboBoxesGenres.Clear();
            }
            else
            {
                currentPanel = parentPanel;
            }

            var comboBox = new ComboBox
            {
                ItemsSource = dziedziny,
                DisplayMemberPath = "Nazwa",
                Margin = new Thickness(0, 0, 0, 5),
                Width = 200
            };


            comboBox.SelectionChanged += async (sender, e) =>
            {
                var childComboBox = sender as ComboBox;
                var selectedDziedzinaNazwa = ((DziedzinaDto)childComboBox.SelectedValue).Nazwa;

                var isLastComboBox = comboBoxesGenres.LastOrDefault() == childComboBox;
                if (isLastComboBox && !string.IsNullOrEmpty(selectedDziedzinaNazwa))
                {
                    await CreateComboBoxes(selectedDziedzinaNazwa, childComboBox.Parent as StackPanel);
                }
            };
            comboBoxesGenres.Add(comboBox);
            currentPanel.Children.Add(comboBox);

            foreach (var cb in comboBoxesGenres)
            {
                cb.Visibility = Visibility.Visible;
            }
        }
        private List<ComboBox> comboBoxesAuthors = new List<ComboBox>();
        private async Task CreateComboBoxesAuthors(string nazwa = "", StackPanel parentPanel = null)
        {
            List<AutorDto> authors = await _database.GetAuthors();

            authors.Insert(0, new AutorDto { Id = 0, FullName = "" });

            StackPanel currentPanel;
            if (parentPanel == null)
            {
                currentPanel = AuthorsStackPanel;
                foreach (var cb in comboBoxesAuthors)
                {
                    currentPanel.Children.Remove(cb);
                }
                comboBoxesAuthors.Clear();
            }
            else
            {
                currentPanel = parentPanel;
            }

            var comboBox = new ComboBox
            {
                ItemsSource = authors,
                DisplayMemberPath = "FullName",
                Margin = new Thickness(0, 0, 0, 5),
                Width = 200
            };


            comboBox.SelectionChanged += async (sender, e) =>
            {
                var childComboBox = sender as ComboBox;
                var selectedAutorFullName = ((AutorDto)childComboBox.SelectedValue).FullName;
                    
                var isLastComboBox = comboBoxesAuthors.LastOrDefault() == childComboBox;
                if (isLastComboBox && !string.IsNullOrEmpty(selectedAutorFullName))
                {
                    await CreateComboBoxesAuthors(selectedAutorFullName, childComboBox.Parent as StackPanel);
                }
            };
            comboBoxesAuthors.Add(comboBox);
            currentPanel.Children.Add(comboBox);

            foreach (var cb in comboBoxesAuthors)
            {
                cb.Visibility = Visibility.Visible;
            }
        }
        private async void DodajKsiazke_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            await CreateComboBoxes();
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Visible;
        }
        private async void DodajAutoraShow_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            await CreateComboBoxesAuthors();
            CollapseAll();
            DodajAutoraGrid.Visibility = Visibility.Visible;
        }

        private void DodajWydawnictwoShow_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            CollapseAll();
            DodajWydawnictwoGrid.Visibility = Visibility.Visible;
        }
        private void DodajDziedzineShow_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            CollapseAll();
            DodajDziedzineGrid.Visibility = Visibility.Visible;
        }
        private void DodajEgzemplarzShow_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            CollapseAll();
            DodajEgzemplarzGrid.Visibility = Visibility.Visible;
        }
        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            CollapseAll();
            MainGrid.Visibility = Visibility.Visible;
        }

        private async void Dodaj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsConnectionAlive();
                var genreNames = comboBoxesGenres
                    .Where(cb => cb.SelectedValue != null && cb.SelectedValue is DziedzinaDto) 
                    .Select(cb => ((DziedzinaDto)cb.SelectedValue).Nazwa) 
                    .ToList();
                var authors_id = comboBoxesAuthors
                    .Where(cb => cb.SelectedValue != null && cb.SelectedValue is AutorDto)
                    .Select(cb => ((AutorDto)cb.SelectedValue).Id)
                    .ToList();
                if (genreNames.Count != genreNames.Distinct().Count())
                {
                    MessageBox.Show("Wprowadź unikalne wartości dla wszystkich dziedzin.");
                    return;
                }
                if (authors_id.Count != authors_id.Distinct().Count())
                {
                    MessageBox.Show("Wprowadź unikalne wartości dla wszystkich autorów.");
                    return;
                }
                int? genreId = await _database.GetExistingHierarchyId(genreNames);
                if (genreNames.Any() && genreId == null)
                {
                    genreId = await _database.AddGenreWithSubTypes(genreNames);
                }

                int publisherId = (int)NewBookPublisher.SelectedValue;
                string title = NewBookTitle.Text;
                string date = NewBookDate.Text;
                await _database.AddBook(title, date, authors_id, publisherId, (int)genreId);
                InitializeComponentsAndGetDataFromDatabase();
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
                IsConnectionAlive();
                string authorName = NewAutorName.Text;
                string authorSurname= NewAutorSurname.Text;
                await _database.AddAuthor(authorName, authorSurname);
                InitializeComponentsAndGetDataFromDatabase();
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
                IsConnectionAlive();
                string name = NoweWydawnictwoNazwa.Text;
                string street = NoweWydawnictwoUlica.Text;
                string apartmentNumber = NoweWydawnictwoNumerBudynku.Text;
                string postcode = NoweWydawnictwoKodPocztowy.Text;
                string city = NoweWydawnictwoMiasto.Text;
                string adress = $"{street} {apartmentNumber} {postcode} {city}";
                await _database.AddPublisher(name, adress);
                InitializeComponentsAndGetDataFromDatabase();
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
                IsConnectionAlive();
                string name = NowaDziedzinaNazwa.Text;
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidDataException();
                int genreId = ((DziedzinaDto)NowaDziedzinaNadrzednaId.SelectedValue).Id;
                await _database.AddGenre(name, genreId);
                InitializeComponentsAndGetDataFromDatabase();
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
            string pattern = @"^(?:\d{1,5}-)?\d{1,}-\d{1,7}-\d{1,7}-[\dX]$";
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
                IsConnectionAlive();
                int bookId = (int)NowyEgzemplarzKsiazka.SelectedValue;
                string isbn = NowyEgzemplarzISBN.Text;
                if (!ValidateISBN(isbn))
                    throw new InvalidDataException();
                await _database.AddCopyOfBook(bookId, isbn);
                //await SetDataToComboBoxes();
                await DisplayBooks();
                await CreateRankings();
                await AktualizujInformacjeOKsiazce();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola poprawnie.");
            }
        }
        private async void NowyEgzemplarzKsiazka_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                IsConnectionAlive();
                if (NowyEgzemplarzKsiazka.SelectedValue is null)
                    return;
                int selectedBookId = (int)NowyEgzemplarzKsiazka.SelectedValue;
                KsiazkaDto selectedBook = (KsiazkaDto)NowyEgzemplarzKsiazka.SelectedItem;

                if (selectedBook != null)
                {
                    InformacjeOKsiazceTextBlock.Text = $"Tytuł: {selectedBook.Tytul}\n" +
                                                       $"Autorzy: {selectedBook.Autor.FullName}\n" +
                                                       $"Wydawnictwo: {selectedBook.Wydawnictwo}\n" +
                                                       $"Rok Wydania: {selectedBook.RokWydania.Year}\n";
                }
            }
            catch (Exception ex)
            {
                
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


        private void PokazWyszukiwarke_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            MainGrid.Visibility = Visibility.Collapsed;
            DodajKsiazkeGrid.Visibility = Visibility.Collapsed;
            WyszukiwarkaKsiazek.Visibility = Visibility.Visible;
        }
        private async void WyszukajKsiazke_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            var tytulDoWyszukania = WyszukiwanyTytul.Text;
            List<KsiazkaDto> books;
            string bookGenre;
            try
            {
                bookGenre = ((DziedzinaDto)SortowaniePoDziedzinie.SelectedValue).Nazwa;
            }
            catch(Exception ex)
            {
                bookGenre = null;
            }
            if(!string.IsNullOrEmpty(bookGenre) && !string.IsNullOrEmpty(tytulDoWyszukania))
            {
                books = await _database.GetAllBooks(bookGenre, tytulDoWyszukania);
            }
            else if (!string.IsNullOrEmpty(bookGenre))
            {
                books = await _database.GetAllBooks(bookGenre);
            }
            else if (!string.IsNullOrEmpty(tytulDoWyszukania))
                books = await _database.GetAllBooks(null, tytulDoWyszukania);
            else
                books = await _database.GetAllBooks();

            WyszukiwaneKsiazkiGrid.ItemsSource = books;
            WyszukiwaneKsiazkiGrid.Visibility = Visibility.Visible;
        }
        
        private void AnulujWyszukiwanie_Click(object sender, RoutedEventArgs e)
        {
            IsConnectionAlive();
            CollapseAll();
            MainGrid.Visibility = Visibility.Visible;

        }
        private async Task AktualizujInformacjeOKsiazce()
        {
            var wybranaKsiazka = (KsiazkaDto)WyszukiwaneKsiazkiGrid.SelectedItem;
            if (wybranaKsiazka is null)
                return;
            var czyDostepna = await _database.GetAvailability(wybranaKsiazka.Id);
            string dostepnosc = "";
            var ocena = await _database.GetRating(wybranaKsiazka.Id);
            var genres = await _database.GetGenresOfBook(wybranaKsiazka.Id);
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
                WybraneWypozyczenieTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nAutor: {wybranaKsiazka.Autor.FullName}\nRok: {wybranaKsiazka.RokWydania.Year}\nWydawnictwo: {wybranaKsiazka.Wydawnictwo}\nStan: {dostepnosc}\nOcena: {ocena.ToString("0.0")}/5\nDziedziny: {genres}";
            else
                WybraneWypozyczenieTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}\nAutor: {wybranaKsiazka.Autor.FullName}\nRok: {wybranaKsiazka.RokWydania.Year}\nWydawnictwo: {wybranaKsiazka.Wydawnictwo}\nStan: {dostepnosc}\nOcena: brak ocen\nDziedziny: {genres}";

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
                IsConnectionAlive();
                var ksiazkaId = ((KsiazkaDto)WyszukiwaneKsiazkiGrid.SelectedItem).Id;
                var czytelnikId = (int)CzytelnicyComboBox.SelectedValue;
                var egzemplarzId = await _database.GetEgzemplarzKsiazki(ksiazkaId);
                await _database.WypozyczEgzemplarzKsiazki(czytelnikId, egzemplarzId);
                await CreateRankings();
                await AktualizujInformacjeOKsiazce();
                CollapseAll();
                MainGrid.Visibility = Visibility.Visible;
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("Proszę się zalogować.");
                CollapseAll();
                await WyswietlCzytelnikow();
                LogowanieGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
            }
        }
        private async void CzytelnicyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                IsConnectionAlive();
                int selectedReaderId = (int)CzytelnicyComboBox.SelectedValue;
                CzytelnikDto selectedReader = (CzytelnikDto)CzytelnicyComboBox.SelectedItem;

                if (selectedReader != null)
                {
                    InformacjeOCzytelnikuTextBlock.Text = $"Imie i Nazwisko: {selectedReader.PelneImieNazwisko}\n" +
                                                       $"Adres: {selectedReader.Adres}\n" +
                                                       $"Email: {selectedReader.Email}\n" +
                                                       $"Telefon: {selectedReader.Telefon}\n";
                }
            }
            catch (Exception ex)
            {
                
            }
        }
        private void CollapseAll()
        {
            IsConnectionAlive();
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
                await SetDataToComboBoxes();
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
            try
            {
                var czytelnik = (CzytelnikDto)CzytelnicyComboBox.SelectedItem;
                LoginStatusTB.Text = $"Wybrano czytelnika: {czytelnik.PelneImieNazwisko}";
            }
            catch(Exception ex) 
            {
                
            }
            MainGrid.Visibility = Visibility.Visible;
        }
        private async void PokazLogowanie_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            await WyswietlCzytelnikow();
            LogowanieGrid.Visibility = Visibility.Visible;
        }
        private async void WypozyczoneKsiazki_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var wybranaKsiazka = (RentalDto)WypozyczoneKsiazki.SelectedItem;
                if(wybranaKsiazka.DataZwrotu is null)
                {
                    PrzyciskZwrotEgzemplarza.Visibility = Visibility.Visible;
                }
                else
                {
                    PrzyciskZwrotEgzemplarza.Visibility = Visibility.Collapsed;
                }
                WybranyZwrotTextBlock.Text = $"Tytuł: {wybranaKsiazka.Tytul}";
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
                await WyswietlCzytelnikow();
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
                        await CreateRankings();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void ButtonDropSchema_Click(object sender, RoutedEventArgs e)
        {
            await DropDatabaseAndCreateFromFile();
        }
    }
}
