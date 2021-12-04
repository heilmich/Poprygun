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

namespace Poprygun
{
    /// <summary>
    /// Логика взаимодействия для AgentWindow.xaml
    /// </summary>
    public partial class AgentWindow : Window
    {
        Entities db = new Entities();
        List<ProductSale> sellsList = new List<ProductSale>();
        
        public AgentWindow()
        {
            InitializeComponent();
            Agent agent = new Agent();
            dataGrid.DataContext = agent;
        }

        public AgentWindow(Agent agent)
        {
            InitializeComponent();
            sellsList = db.ProductSale.Where(p => p.AgentID == agent.ID).ToList();
            agentSells.ItemsSource = sellsList;
            dataGrid.DataContext = agent;
        }

        private void Click_SaveAgent(object sender, RoutedEventArgs e)
        {
            
            db.SaveChanges();
            Close();
        }
    }
}
