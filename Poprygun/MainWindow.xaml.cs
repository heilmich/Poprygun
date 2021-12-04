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
using System.Text.Json;
using System.Drawing;
using System.Runtime.CompilerServices;


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

    public class RangeConverter : IValueConverter // конвертер значения элемента рассчёта скидки для изменения фона Item в ListView
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (System.Convert.ToInt32(value) > 25) return true;
            else return false;
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
        public int visiblePagesCount = 4;                           // количество отображаемых страниц в pageList
        public int totalItems;                                      // кол-во объектов
        public int totalPages                                      // кол-во страниц
        {
            get { return (int)Math.Ceiling((decimal)totalItems / pageSize); }
        }
    }

    public class SortItem : INotifyPropertyChanged
    {
        private string sortDir;
        public string SortDir                                             // направление сортировки
        {
            get
            {  return sortDir; }
            set
            {
                if (sortDir != value)
                {
                    sortDir = value;
                    OnPropertyChanged("sortDir");
                }
            }
        }
        private string sortProperty;                                        // свойство сортировки
        public string SortProperty                                             // направление сортировки
        {
            get
            { return sortProperty; }
            set
            {
                if (sortProperty != value)
                {
                    sortProperty = value;
                    OnPropertyChanged("sortProperty");
                }
            }
        }
        private string sortTitle;
        public string SortTitle
        {
            get
            { return sortTitle; }
            set
            {
                if (sortTitle != value)
                {
                    sortTitle = value;
                    OnPropertyChanged("sortTitle");
                }
            }
        }
        public SortItem(string dir, string prop, string title) 
        {
            sortDir = dir;
            sortProperty = prop;
            sortTitle = title;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public partial class MainWindow : Window
    {

        public static List<Agent> agentList = new List<Agent>();    // список агентов
        public Entities db = new Entities();                        // создание контекста базы данных
        public PageInfo pageInfo = new PageInfo();                  // объект pageInfo с информацией о страницах
        public SortItem currentSort;                                // индекс сортировки
        public int filterIndex = 0;                                 // индекс фильтра, 0 - без фильтрации
        public string searchQuery;                             // поисковый запрос
        public List<AgentType> agentTypes = new List<AgentType>();  // лист с типами агентов
        public List<SortItem> sortList = new List<SortItem>
        {
            new SortItem("ASC", "ID", "Без сортировки"),
            new SortItem("ASC", "Title", "По возрастанию: наименование"),
            new SortItem("DESC", "Title", "По убыванию: наименование"),
            new SortItem("ASC", "ID", "По возрастанию: скидка"),
            new SortItem("DESC", "ID", "По убыванию: скидка"),
            new SortItem("ASC", "Priority", "По возрастанию: приоритет"),
            new SortItem("DESC", "Priority", "По убыванию: приоритет")
        };
        
        public MainWindow()
        {
            InitializeComponent();
            sortCB.ItemsSource = sortList;
            UpdatePage();
            TakeAgentTypesList();
        }

        public async void TakeAgentTypesList() 
        {
            AgentType type = new AgentType { ID = 0, Title = "Все типы" };
            agentTypes.Add(type);
            await db.AgentType.ForEachAsync(p => 
            { 
                agentTypes.Add(p); 
            });
            
            filterCB.ItemsSource = agentTypes;
        }

        public void UpdatePage() 
        {
            dataList.ItemsSource = TakeData();
            Pagination();
        }

        public List<Agent> TakeData() 
        {
            if (filterIndex == 0)
            {
                return db.Agent.SqlQuery($"SELECT * FROM Agent " +
                        $"WHERE ( Agent.Title LIKE N'%{searchQuery}%' OR Agent.Email LIKE N'%{searchQuery}%' ) " +
                        $"ORDER BY Agent.{currentSort.SortProperty} {currentSort.SortDir} " +
                        $"OFFSET {(pageInfo.pageIndex - 1) * pageInfo.pageSize} ROWS " +
                        $"FETCH NEXT {pageInfo.pageSize} ROWS ONLY; ").ToList();
            }
            else 
            {
                return db.Agent.SqlQuery($"SELECT * FROM Agent " +
                        $"WHERE Agent.AgentTypeID = {filterIndex} AND ( Agent.Title LIKE N'%{searchQuery}%' OR Agent.Email LIKE N'%{searchQuery}%' ) " +
                        $"ORDER BY Agent.{currentSort.SortProperty} {currentSort.SortDir} " +
                        $"OFFSET {(pageInfo.pageIndex - 1) * pageInfo.pageSize} ROWS " +
                        $"FETCH NEXT {pageInfo.pageSize} ROWS ONLY; ").ToList();
            }
        }

        public void Pagination() 
        {
            pageList.Children.Clear(); // чистим список элементов

            // расчёт кол-ва элементов в запросе
            if (filterIndex == 0) pageInfo.totalItems = db.Agent.Count();
            else pageInfo.totalItems = db.Agent.SqlQuery($"SELECT * FROM Agent a " + $"WHERE a.AgentTypeID = {filterIndex} ").ToList().Count();


            for (int i = pageInfo.pageIndex; i <= pageInfo.totalPages; i++) // рассчёт кол-ва страниц в списке страниц
            {
                if (i >= pageInfo.pageIndex && i <= pageInfo.pageIndex + 3)
                {
                    TextBlock page = new TextBlock();
                    page.Text = Convert.ToString(i);
                    page.Style = this.FindResource("PageLabel") as Style;
                    page.MouseLeftButtonDown += selectPageClick;
                    if (i == pageInfo.pageIndex) page.TextDecorations = TextDecorations.Underline;

                    pageList.Children.Add(page);
                }
            }


        }
        private void search_field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            searchQuery = search_field.Text;
            UpdatePage();
        }
        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            pageInfo.pageIndex = 1;
            filterIndex = ((ComboBox)sender).SelectedIndex;
            UpdatePage();
        }
        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            currentSort = (SortItem)((ComboBox)sender).SelectedItem;
            UpdatePage();
        }
        private void prevPageClick(object sender, MouseButtonEventArgs e)
        {
            if (pageInfo.pageIndex != 0)
            {
                pageInfo.pageIndex -= 1;
                UpdatePage();
            }
        }
        private void nextPageClick(object sender, MouseButtonEventArgs e)
        {
            if (pageInfo.pageIndex != pageInfo.totalPages)
            {
                pageInfo.pageIndex += 1;
                UpdatePage();
            }
        }
        private void selectPageClick(object sender, MouseButtonEventArgs e)
        {
            pageInfo.pageIndex = Convert.ToInt32(((TextBlock)sender).Text);
            UpdatePage();

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
                using (Entities db = new Entities())
                {
                    Agent agent = db.Agent.Where(p => p.Logo == "agent_" + Convert.ToString(num) + ".png").FirstOrDefault();
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

        private void Click_AddAgent(object sender, RoutedEventArgs e)
        {
            
            AgentWindow agentWindow = new AgentWindow();
            agentWindow.ShowDialog();
        }

        private void Click_EditAgent(object sender, RoutedEventArgs e)
        {
            AgentWindow agentWindow = new AgentWindow((Agent)dataList.SelectedItem);
            agentWindow.ShowDialog();
        }
    }
}
