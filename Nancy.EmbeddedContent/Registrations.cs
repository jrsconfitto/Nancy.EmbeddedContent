namespace Nancy.EmbeddedContent
{
    using Nancy.Bootstrapper;
    using Nancy.ViewEngines;

    /// <summary>
    /// Application registrations supporting views embedded in an assembly
    /// </summary>
    public class EmbeddedRegistrations : Registrations
    {
        /// <summary>
        /// Registers the <see cref="EmbeddedViewLocationProvider"/> with the container to enable
        /// embedded views
        /// </summary>
        public EmbeddedRegistrations()
        {
            this.Register<IResourceAssemblyProvider>(typeof(ResourceAssemblyProvider));
            this.Register<IResourceReader>(typeof(DefaultResourceReader));
            this.Register<IViewLocationProvider>(typeof(EmbeddedViewLocationProvider));
        }

    }
}
