namespace Demo
{
    using Nancy;

    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            this.Get["/"] = _ => this.View["index"];
        }
    }
}