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


namespace wildfire_MultiFuctionalSerial_assistant
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FunctionTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            if (functionTabControl.SelectedIndex == 2  )
            { 
                softWareMainWindow.Width = 1250;
                softWareMainWindow.Height = 680;

                //GPSItem.Content = new wildfire_MultiFuctionalSerial_assistant.SerialBasic(); 

            
               
            }
            else if (functionTabControl.SelectedIndex == 3  )
            {
                softWareMainWindow.Width = 1250;
                softWareMainWindow.Height = 680;

                //FourmItem.Content = new wildfire_MultiFuctionalSerial_assistant.GPSMap();
               


            }
            else
            {
                softWareMainWindow.Width = 800;
                softWareMainWindow.Height = 600;
            }
        }

      

       
    }
}
