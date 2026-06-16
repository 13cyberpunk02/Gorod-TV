using GorodTv.Tv.Pages;

namespace GorodTv.Tv
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("channellist", typeof(ChannelListTvPage)); 
            Routing.RegisterRoute("player", typeof(PlayerTvPage));
        }
    }
}
