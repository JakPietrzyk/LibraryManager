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
using System.Windows.Shapes;

namespace LibraryManager
{
    /// <summary>
    /// Interaction logic for BookReviewWindow.xaml
    /// </summary>
    public partial class BookReviewWindow : Window
    {
        public int SelectedRating { get; private set; }
        public BookReviewWindow()
        {
            InitializeComponent();
        }
        private async void WystawOcene_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedRating = OcenaComboBox.SelectedIndex + 1; 
                DialogResult = true; 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async void AnulujOcene_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
    }
}
