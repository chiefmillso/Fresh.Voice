using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Fresh.Voice.Startup))]
namespace Fresh.Voice
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
