using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Globalization;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace Poprygun
{
    /// <summary>
    /// Логика взаимодействия для AgentWindow.xaml
    /// </summary>
    /// 

    public class ProductConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Entities db = new Entities();
            if (value == null)
                return db.ProductSale.Max(p => p.ProductID) + 1;
            else return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }

    public partial class AgentWindow : Window
    {
        Entities db = new Entities();
        ObservableCollection<ProductSale> sellsList = new ObservableCollection<ProductSale>();
        Agent currentAgent = new Agent();
        public AgentWindow()
        {
            InitializeComponent();
            dataGrid.DataContext = currentAgent;
        }

        public AgentWindow(Agent agent)
        {
            InitializeComponent();
            currentAgent = db.Agent.Where(p => p.ID == agent.ID).FirstOrDefault();
            agentSells.ItemsSource = currentAgent.ProductSale;
            dataGrid.DataContext = currentAgent;
        }

        private void Click_SaveAgent(object sender, RoutedEventArgs e)
        {
            db.SaveChanges();
            Close();
        }

        private void Add_AgentSells(object sender, AddingNewItemEventArgs e)
        {
            
        }

        private void AddAgentSells(object sender, DataGridRowEditEndingEventArgs e)
        {
            ProductSale productsale = ((ProductSale)e.Row.Item);
            productsale.AgentID = currentAgent.ID;
            db.ProductSale.Add(productsale);
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg; *.png; *.jpeg";
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Выберите изображение";
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != null)
                Serialize(openFileDialog.FileName);
            else MessageBox.Show("Вы не выбрали изображение");
            
        }

        public void Serialize(string path)
        {
            string pic;
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] array = new byte[fs.Length];
                fs.Read(array, 0, array.Length);

                pic = JsonSerializer.Serialize(array);
                currentAgent.Image = pic;
            }
        }
    }
}
