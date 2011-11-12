using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuMVC.Core;
using FubuMVC.Core.Behaviors;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Registration.ObjectGraph;
using FubuMVC.Core.Runtime;
using HtmlTags;

namespace AddValidationBehaviour.Validation
{
    public class ValidationController
    {
        private readonly IFubuRequest _request;

        public ValidationController(IFubuRequest request)
        {
            _request = request;
        }

        public HtmlDocument GetStringRequest(ValidationFailure failure)
        {
            var document = new HtmlDocument();
            document.Add("h1").Text("Something bad happened: " + _request.Get<ValidationResult>().Message);
            return document;
        }

    }
    public class ValidationBehavior<T> : BasicBehavior
         where T : class
    {
        private readonly IFubuRequest _request;
        private readonly ActionCall _target;
        private readonly IPartialFactory _factory;

        public ValidationBehavior(IFubuRequest request, ActionCall target, IPartialFactory factory)
            : base(PartialBehavior.Executes)
        {
            _request = request;
            _target = target;
            _factory = factory;
        }

        protected override DoNext performInvoke()
        {
            var inputModel = _request.Get<T>();
            var notification = Validate(inputModel);
            if (notification.IsValid())
            {
                return DoNext.Continue;
            }

            _request.Set(notification);
            _factory.BuildPartial(typeof(ValidationFailure)).InvokePartial();

            return DoNext.Stop;
        }

        private ValidationResult Validate(T inputModel)
        {
            var propertyInfo = typeof (T).GetProperty("Price");
            if(propertyInfo != null)
            {
                var getMethod = propertyInfo.GetGetMethod();
                var value = (decimal) getMethod.Invoke(inputModel, new object[0]);

                if(value <= 0)
                {
                    return new ValidationResult(false, "Price must be greater than zero");
                }
            }

            return new ValidationResult(true);
        }
    }

    public class ValidationFailure
    {
    }

    public class ValidationResult
    {
        public string Message { get; set; }
        private readonly bool _valid;

        public ValidationResult(bool valid)
        {
            _valid = valid;
        }

        public ValidationResult(bool valid, string message) : this(valid)
        {
            Message = message;
        }

        public bool IsValid()
        {
            return _valid;
        }
    }

    public class ValidationNode : Wrapper
    {
        private readonly ActionCall _call;

        public ValidationNode(ActionCall call)
            : base(typeof(ValidationBehavior<>).MakeGenericType(call.InputType()))
        {
            _call = call;
        }

        protected override ObjectDef buildObjectDef()
        {
            var def = base.buildObjectDef();
            def.DependencyByValue(_call);
            return def;
        }
    }

    public static class ActionCallExtensions
    {
        public static void WrapWithValidation(this ActionCall call)
        {
            call.AddBefore(new ValidationNode(call));
        }
    }

    public class ValidationConvention : IConfigurationAction
    {
        private readonly Func<ActionCall, bool> _predicate;

        public ValidationConvention(Func<ActionCall, bool> predicate)
        {
            _predicate = predicate;
        }


        public void Configure(BehaviorGraph graph)
        {
            graph
                .Actions()
                .Where(call => _predicate(call))
                .Each(call =>
                {
                    var log = graph.Observer;
                    if (log.IsRecording)
                    {
                        log.RecordCallStatus(call, "Wrapping {0} with Validation Behavior".ToFormat(call));
                    }

                    call.WrapWithValidation();
                });
        }
    }
}