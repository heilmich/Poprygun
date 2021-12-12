using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data.Entity;
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
        public Entities db;
        public List<AgentType> agentTypes = new List<AgentType>();
        public ObservableCollection<ProductSale> sellsList = new ObservableCollection<ProductSale>();
        public static Agent currentAgent = new Agent();
        public static bool isCreate = true;
        public AgentWindow(Entities db)
        {
            this.db = db;
            InitializeComponent();
            UpdateLists();
            
        }

        public AgentWindow(Agent agent, Entities db)
        {
            this.db = db;
            isCreate = false;
            InitializeComponent();
            currentAgent = agent;
            UpdateLists();
        }

        public void UpdateLists() 
        {
            dataGrid.DataContext = currentAgent;
            agentSells.ItemsSource = new ObservableCollection<ProductSale>(currentAgent.ProductSale);
                
        }


        private void Click_SaveAgent(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isCreate == true)
                {
                    
                    currentAgent.AgentTypeID = db.AgentType.Where(p => p.Title == tbAgentType.Text).FirstOrDefault().ID;
                    db.Agent.Add(currentAgent);
                }
                db.SaveChanges();
                Close();
            }
            catch (Exception ex)
            {
                
                MessageBox.Show("Возникла ошибка при сохранении агента. Проверьте все поля и попробуйте ещё раз.\nКод ошибки: " + ex.Message, "\n" + ex.InnerException);
            }
        }
        private void Click_RemoveAgent(object sender, RoutedEventArgs e)
        {
            if (db.ProductSale.Any( p => p.AgentID == currentAgent.ID) != false) 
            {
                MessageBox.Show("Нельзя удалить агента, если у него есть записи в истории продаж");
                return;
            }

            try {
            db.AgentPriorityHistory.RemoveRange(currentAgent.AgentPriorityHistory);
            db.Shop.RemoveRange(currentAgent.Shop);
            db.Agent.Remove(currentAgent);
            db.SaveChanges();
            }
            catch (Exception ex)
            {

                MessageBox.Show("Возникла ошибка при удалении агента. \nКод ошибки: " + ex.Message, "\n" + ex.InnerException);
            }

            Close();
        }

        private void Click_RemoveSell(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductSale ps = (ProductSale)agentSells.SelectedItem;
                db.ProductSale.Remove(ps);
                db.SaveChanges();
                agentSells.ItemsSource = null;
                UpdateLists();
            }
            catch (Exception ex)
            {

                MessageBox.Show("Возникла ошибка при удалении продажи. \nКод ошибки: " + ex.Message, "\n" + ex.InnerException);
            }
        }

        private void AddAgentSells(object sender, DataGridRowEditEndingEventArgs e)
        {
            ProductSale productsale = ((ProductSale)e.Row.Item);
            productsale.AgentID = currentAgent.ID;
            productsale.Agent = currentAgent;
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
