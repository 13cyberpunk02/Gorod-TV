namespace GorodTV
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Экраны, открываемые поверх табов (не описаны в AppShell.xaml):
            Routing.RegisterRoute("channellist", typeof(Pages.ChannelListPage));
            Routing.RegisterRoute("player", typeof(Pages.PlayerPage));
        }
    }
}
