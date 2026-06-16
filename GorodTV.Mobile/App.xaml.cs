using Microsoft.Extensions.DependencyInjection;

namespace GorodTV
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();        
        }

        protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
    }
}