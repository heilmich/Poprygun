using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
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
using System.Drawing;



namespace Poprygun
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public class SellsConverter : IValueConverter // конвертер расчёта кол-ва продаж для привязки
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

    public class DiscountConverter : IValueConverter // конвертер скидки для привязки
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashSet<ProductSale> ps = (HashSet<ProductSale>)value;
            var query = ps.Sum(p => p.ProductCount) * 1000;

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

    public class ImageConverter : IValueConverter // конвертер изображений для привязки
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "picture.png";
            
            return MainWindow.DeserializeImage((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class PageInfo
    {
        public int pageIndex = 1;                                   // индекс текущей страницы
        public int pageSize = 10;                                   // размер страницы
        public int totalItems;                                      // кол-во объектов
        public int totalPages                                      // кол-во страниц
        {
            get { return (int)Math.Ceiling((decimal)totalItems / pageSize); }
        }
    }

    public partial class MainWindow : Window
    {
        
        public static List<Agent> agentList = new List<Agent>();    // список агентов
        public Entities db = new Entities();                        // создание контекста базы данных
        public PageInfo pageInfo = new PageInfo();           
            
        
        public MainWindow()
        {
            InitializeComponent();
            UpdatePage();
            UpdatePage();
        }

        public async void TakeAgentList()
        {
            await db.Agent.ForEachAsync(p =>
               {
                   agentList.Add(p);
               });
            dataList.ItemsSource = agentList;
        }

        public void UpdatePage() 
        {
            var currentList = TakeData();
            // var currentList = agentList.Skip(pageInfo.pageIndex).Take(pageInfo.pageSize);

            for (int i = pageInfo.pageIndex; i <= pageInfo.totalPages; i++) 
            {
                TextBlock page = new TextBlock();
                page.Text = Convert.ToString(i);
                pageList.Children.Add(page);
                
            }

            dataList.ItemsSource = currentList;
        }

        public List<Agent> TakeData() 
        {
            return db.Agent.OrderBy( p => p.ID).Skip((pageInfo.pageIndex - 1) * pageInfo.pageSize).Take(pageInfo.pageSize).ToList();      
        }



        public void SerializeImages(int num)
        {
            string path = @"D:\agents\";
            string pic;
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            using (FileStream fs = File.OpenRead($"{path}agent_{num}.png"))
            {
                byte[] array = new byte[fs.Length];
                fs.Read(array, 0, array.Length);

                pic = JsonSerializer.Serialize(array);
                string search_str = "agent_" + Convert.ToString(num) + ".png";
                using (Entities db = new Entities())
                {
                    Agent agent = db.Agent.Where(p => p.Logo == search_str).FirstOrDefault();
                    if (agent == null) return;
                    agent.Image = pic;
                    
                    db.SaveChanges();
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

        public static BitmapImage DeserializeImage(string value) 
        {
            byte[] array = System.Convert.FromBase64String(JsonSerializer.Deserialize<string>((string)value));
            MemoryStream ms = new MemoryStream(array, 0, array.Length);
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }
    }
}
