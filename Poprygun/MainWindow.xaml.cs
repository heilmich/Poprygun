﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var query = ps.Sum(p => p.ProductCount) * 15000;        // умножил на 15000, чтобы был больший разброс в скидках

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

    public class RangeConverter : IValueConverter // НЕ ИСПОЛЬЗУЕТСЯ конвертер значения элемента рассчёта скидки для изменения фона Item в ListView
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
        private string sortDir;     // направление сортировки
        public string SortDir                                             
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
        private string sortProperty;      // свойство(запрос) сортировки                                  
        public string SortProperty                                             
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
        private string sortTitle;   // наименование сортировки
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

        public static ObservableCollection<Agent> agentList = new ObservableCollection<Agent>();    // список агентов
        public Entities db = new Entities();                                                        // создание контекста базы данных
        public PageInfo pageInfo = new PageInfo();                                                  // объект pageInfo с информацией о страницах
        public SortItem currentSort;                                                                // индекс сортировки
        public int filterIndex = 0;                                                                 // индекс фильтра, 0 - без фильтрации
        public string searchQuery;                                                                  // поисковый запрос
        public List<AgentType> agentTypes = new List<AgentType>();                                  // лист с типами агентов
        public List<SortItem> sortList = new List<SortItem>                                         // лист с объектами сортировки
        {
            new SortItem("ASC", "Agent.ID", "Без сортировки"),
            new SortItem("ASC", "Agent.Title", "По возрастанию: наименование"),
            new SortItem("DESC", "Agent.Title", "По убыванию: наименование"),
            new SortItem("ASC", "( SELECT SUM(ProductCount) FROM ProductSale WHERE AgentID = Agent.ID )", "По возрастанию: скидка"),
            new SortItem("DESC", "( SELECT SUM(ProductCount) FROM ProductSale WHERE AgentID = Agent.ID  )", "По убыванию: скидка"),
            new SortItem("ASC", "Agent.Priority", "По возрастанию: приоритет"),
            new SortItem("DESC", "Agent.Priority", "По убыванию: приоритет")
        };

        public static ObservableCollection<T> ToObservableCollection<T>(List<T> col)
        {
            return new ObservableCollection<T>(col);
        }

        public MainWindow()
        {
            InitializeComponent();
            sortCB.ItemsSource = sortList;
            UpdatePage();
            TakeAgentTypesList();
        }

        public async void TakeAgentTypesList()  // функция для получения типов агентов из БД
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
            agentList = TakeData();
            dataList.ItemsSource = agentList;
            Pagination();
        }

        public ObservableCollection<Agent> TakeData()   // функция для получения данных об агентах из БД
        {
            if (filterIndex == 0)
            {
                return new ObservableCollection<Agent>( db.Agent.SqlQuery($"SELECT * FROM Agent " + 
                        $"WHERE ( Agent.Title LIKE N'%{searchQuery}%' OR Agent.Email LIKE N'%{searchQuery}%' ) " +
                        $"ORDER BY {currentSort.SortProperty} {currentSort.SortDir} " +
                        $"OFFSET {(pageInfo.pageIndex - 1) * pageInfo.pageSize} ROWS " +
                        $"FETCH NEXT {pageInfo.pageSize} ROWS ONLY; "));
            }
            else 
            {
                return new ObservableCollection<Agent>( db.Agent.SqlQuery($"SELECT * FROM Agent " +
                        $"WHERE Agent.AgentTypeID = {filterIndex} AND ( Agent.Title LIKE N'%{searchQuery}%' OR Agent.Email LIKE N'%{searchQuery}%' ) " +
                        $"ORDER BY {currentSort.SortProperty} {currentSort.SortDir} " +
                        $"OFFSET {(pageInfo.pageIndex - 1) * pageInfo.pageSize} ROWS " +
                        $"FETCH NEXT {pageInfo.pageSize} ROWS ONLY; "));
            }
        }

        public void Pagination()    // функция разбивки на страницы
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
                    page.MouseLeftButtonDown += SelectPageClick;
                    if (i == pageInfo.pageIndex) page.TextDecorations = TextDecorations.Underline;

                    pageList.Children.Add(page);
                }
            }


        }
        private void Search_field_KeyDown(object sender, KeyEventArgs e) // поиск, вызывается при нажатии на Enter
        {
            if (e.Key != Key.Enter) return;
            searchQuery = search_field.Text;
            UpdatePage();
        }
        private void FilterChanged(object sender, SelectionChangedEventArgs e) // изменение фильтра
        {
            pageInfo.pageIndex = 1;
            filterIndex = ((ComboBox)sender).SelectedIndex;
            UpdatePage();
        }
        private void SortChanged(object sender, SelectionChangedEventArgs e) // изменение сортировки
        {
            currentSort = (SortItem)((ComboBox)sender).SelectedItem;
            UpdatePage();
        }
        private void PrevPageClick(object sender, MouseButtonEventArgs e)
        {
            if (pageInfo.pageIndex != 0)
            {
                pageInfo.pageIndex -= 1;
                UpdatePage();
            }
        }
        private void NextPageClick(object sender, MouseButtonEventArgs e)
        {
            if (pageInfo.pageIndex != pageInfo.totalPages)
            {
                pageInfo.pageIndex += 1;
                UpdatePage();
            }
        }
        private void SelectPageClick(object sender, MouseButtonEventArgs e)
        {
            pageInfo.pageIndex = Convert.ToInt32(((TextBlock)sender).Text);
            UpdatePage();

        }
        private void Click_AddAgent(object sender, RoutedEventArgs e)
        {
            AgentWindow agentWindow = new AgentWindow(db);
            agentWindow.ShowDialog();
        }

        private void Click_EditAgent(object sender, RoutedEventArgs e)
        {
            if ((Agent)dataList.SelectedItem == null) return;
                AgentWindow agentWindow = new AgentWindow((Agent)dataList.SelectedItem, db);
            agentWindow.ShowDialog();
            UpdatePage();
        }

        public static BitmapImage DeserializeImage(string value) // десериализация json строки из БД в изображение
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
