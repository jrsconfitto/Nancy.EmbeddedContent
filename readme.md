# Nancy.Embedded

A NuGet package to aid in serving embedded content over a Nancy application. Sometimes you just want to ship a `dll` instead of a bunch of files.

## Usage

To use embedded views and static content, just `Install-Package Nancy.Embedded`.  

Views will be discovered automatically as long as the build action is set to **Embedded Resource**.  
To serve static content, you will have to add a static content convention:

```csharp
public class Bootstrapper : DefaultNancyBootstrapper
{
    protected override void ConfigureConventions(NancyConventions nancyConventions)
    {
        nancyConventions.StaticContentsConventions.AddEmbeddedDirectory("/Content");
    }
}
```

