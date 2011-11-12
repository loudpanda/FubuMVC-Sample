using System;
using System.Collections.Generic;
using System.Linq;
using AddValidationBehaviour.Validation;
using FubuCore;
using FubuMVC.Core;
using FubuMVC.Core.Behaviors;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.Conventions;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Runtime;
using FubuMVC.Core.Urls;
using FubuMVC.Spark;

namespace AddValidationBehaviour
{
    public class ConfigureFubuMVC : FubuRegistry
    {
        public ConfigureFubuMVC()
        {
            // This line turns on the basic diagnostics and request tracing
            IncludeDiagnostics(true);

            // All public methods from concrete classes ending in "Controller"
            // in this assembly are assumed to be action methods
            Actions.IncludeClassesSuffixedWithController();

            // Policies
            Routes
                .IgnoreControllerNamesEntirely()
                .IgnoreMethodSuffix("Html")
                .RootAtAssemblyNamespace();

            Policies.Add<ContinuationHandlerConvention>();
            Policies.Add<SaveResultHandlerConvention>();
            ApplyConvention(new ValidationConvention(x => x.HasInput));

            Routes.HomeIs<ProductListInputModel>();
            
            this.UseSpark();

            // Match views to action methods by matching
            // on model type, view name, and namespace
            Views.TryToAttachWithDefaultConventions();
        }
    }

    public class SaveResult
    {
        public ValidationResult Notification { get; set; }

        public bool Valid { get; set; }

        public object Next { get; set; }
    }

    public class SaveResultHandlerConvention : IConfigurationAction
    {
        public void Configure(BehaviorGraph graph)
        {
            graph.Actions().Where(x => x.OutputType().CanBeCastTo<SaveResult>()).Each(call =>
            {
                call.AddAfter(new SaveResultNode()); 
                graph.Observer.RecordCallStatus(call, "Adding ContinuationNode directly after action call");
            });
        }
    }

    public class SaveResultNode : Wrapper
    {
        public SaveResultNode()
            : base(typeof(SaveResultHandler))
        {
        }
    }

    public class SaveResultHandler : BasicBehavior
    {
        private readonly IFubuRequest _request;
        private readonly IPartialFactory _factory;
        private readonly IUrlRegistry _registry;
        private readonly IOutputWriter _writer;

        public SaveResultHandler(IFubuRequest request, IPartialFactory factory, IUrlRegistry registry,
            IOutputWriter writer)
            : base(PartialBehavior.Ignored)
        {
            _request = request;
            _factory = factory;
            _registry = registry;
            _writer = writer;
        }

        protected override DoNext performInvoke()
        {
            var saveResult = _request.Get<SaveResult>();

            if(!saveResult.Valid)
            {
                var notification = saveResult.Notification;
                _request.Set(notification);
                _factory.BuildPartial(typeof(ValidationFailure)).InvokePartial();
            }
            else
            {
                string url = saveResult.Next as string ?? _registry.UrlFor(saveResult.Next);
                _writer.RedirectToUrl(url);
            }
            return DoNext.Stop;
        }
    }
}