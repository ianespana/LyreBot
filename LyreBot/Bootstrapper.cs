using System.Windows;
using Caliburn.Micro;
using NarakaMidiBot.ViewModels;

namespace NarakaMidiBot
{
    public class Bootstrapper : BootstrapperBase
    {
        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<MainViewModel>();
        }
    }
}
