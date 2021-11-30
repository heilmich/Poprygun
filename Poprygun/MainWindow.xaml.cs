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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Poprygun
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public class AgentItem 
    {
        public string image;
        public string typeName;
        public string sells;
        public string phone;
        public string priority;
        public string percent;
    }
    public partial class MainWindow : Window
    {
        public static List<AgentItem> agents = new List<AgentItem>();
        public static List<AgentItem> pageList;
        public static List<Agent> agentList = new List<Agent>();
        public Entities db = new Entities();
        public MainWindow()
        {
            InitializeComponent();
            UpdateAgentList();
            
            dataList.ItemsSource = agentList;
        }

        public void UpdateAgentList()
        {
            
            var agentss = from a in db.Agent
                            select a;
            foreach (var item in agentss) 
            {
                agentList.Add(item);
                /* AgentItem agent = new AgentItem();
                agent.image = item.Logo;
                agent.typeName = String.Concat(item.AgentType.Title + "|" + item.Title);
                agent.phone = item.Phone;
                agent.sells = "";
                agent.priority = Convert.ToString(item.Priority);
                agent.percent = "";
                agentList.Add(agent); */
            }
                
            
        }
    }
}
