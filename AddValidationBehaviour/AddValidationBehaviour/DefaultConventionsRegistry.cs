using StructureMap.Configuration.DSL;

namespace AddValidationBehaviour
{
    public class DefaultConventionsRegistry : Registry
    {
        public DefaultConventionsRegistry()
        {
            Scan(x =>
            {
                x.TheCallingAssembly();
                x.WithDefaultConventions();
            });

            For<IProductRepository>().Singleton().Use<ProductRepository>();
        }
    }
}