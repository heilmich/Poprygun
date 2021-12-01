using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using System.Globalization;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace Poprygun
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public class SellsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<ProductSale> ps = ((IEnumerable<ProductSale>)value).Where(p => p.SaleDate > DateTime.Now.AddDays(-365)); // не выводит продажи, так как последние продажи были в 2019
            if (ps.Count() == 0) return "Нет продаж за год";
            else 
            return ps.Count() + " продаж за год";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }

    public class DiscountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashSet<ProductSale> ps = (HashSet<ProductSale>)value;
            var query = ps.Sum( p => p.ProductCount) * 1000;

            if (query >= 500000) return 25;
            else if (query >= 150000) return 20;
            else if (query >= 50000) return 10;
            else if (query >= 10000) return 5;
            else if (query >= 0) return 0;
            else return "Ошибка в расчете скидки";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }

    public partial class MainWindow : Window
    {
        public static List<Agent> pageList;
        public static List<Agent> agentList = new List<Agent>();
        public Entities db = new Entities();

        
        public MainWindow()
        {
            InitializeComponent();
            for (int i = 1; i <= 130; i++) 
            {
                SerializeImages(i);
            }
            
            TakeAgentList();
            UpdateList();
        }

        public void TakeAgentList()
        {
            var agents = from a in db.Agent
                         select a;

            foreach (var item in agents)
            {
                agentList.Add(item);
            }
        }

        public void UpdateList()
        {
            dataList.ItemsSource = agentList;
        }

        public void SerializeImages(int num)
        {
            string path = @"C:\Users\HeilMich\source\repos\Poprygun\agents\";
            string pic;
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            using (FileStream fs = File.OpenRead($"{path}agent_{num}.png"))
            {
                byte[] array = new byte[fs.Length];
                fs.Read(array, 0, array.Length);

                pic = JsonSerializer.Serialize(array);
                string search_str = "agent_" + Convert.ToString(num) + ".png";
                using (Entities DB = new Entities())
                {
                    Agent agent = DB.Agent.Where(p => p.Logo == search_str).FirstOrDefault();
                    if (agent == null) return;
                    agent.Logo = pic;
                    DB.SaveChanges();
                }
                
                
            }
            /* string path2 = @"C:\Users\HeilMich\source\repos\Poprygun\";
            using (FileStream fs = File.Open(($"{path2}agent_{num}.png"), FileMode.OpenOrCreate))
            {
                byte[] array = new byte[fs.Length];
                array = JsonSerializer.Deserialize<byte[]>(pic);
                fs.Write(array);
            } */
        }
    }
}
