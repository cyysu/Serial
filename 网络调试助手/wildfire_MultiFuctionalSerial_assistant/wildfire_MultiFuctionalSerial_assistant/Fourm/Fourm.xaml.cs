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

using System.Reflection;

namespace wildfire_MultiFuctionalSerial_assistant
{
    /// <summary>
    /// Fourm.xaml 的交互逻辑
    /// </summary>
    public partial class Fourm : UserControl
    {
        public Fourm()
        {
          InitializeComponent();           
        }


        //用于屏蔽webbrowser脚本错误
        public void SuppressScriptErrors(object webBrowser, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(object).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }


        private void CommondFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            SuppressScriptErrors(sender, true);
        }

        private void CommondFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (e.Exception is System.Net.WebException)
            {
                MessageBox.Show("\r\n站点不可达\r\n");
                 e.Handled = true;
            }
           
        }
    }
}
