using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using MrCMS.ACL;
using MrCMS.Helpers;
using MrCMS.Web.Apps.Admin.Helpers;
using MrCMS.Web.Apps.Admin.Infrastructure.Routing;
using MrCMS.Web.Apps.Admin.Models;
using MrCMS.Website;
using MrCMS.Website.Auth;

namespace MrCMS.Web.Apps.Admin.Services
{
    public class GetAclOptions : IGetAclOptions
    {
        private readonly IGetAllAdminDescriptors _getAllAdminDescriptors;
        private readonly IGetExplicitAclOperations _getExplicitAclOperations;

        public GetAclOptions(IGetAllAdminDescriptors getAllAdminDescriptors, IGetExplicitAclOperations getExplicitAclOperations)
        {
            _getAllAdminDescriptors = getAllAdminDescriptors;
            _getExplicitAclOperations = getExplicitAclOperations;
        }

        public List<AclInfo> GetInfos()
        {
            var aclOptions = new List<AclInfo>();

            aclOptions.AddRange(GetExplicitOptions());

            var descriptors = _getAllAdminDescriptors.GetDescriptors()
                .FindAll(x => !x.ControllerTypeInfo.IsDefined(typeof(AclAttribute)));

            foreach (var controller in descriptors.GroupBy(x => x.ControllerName))
            {
                aclOptions.AddRange(GetControllerOptions(controller.Key, controller.AsEnumerable()));
            }
            return aclOptions;
        }

        private IEnumerable<AclInfo> GetExplicitOptions()
        {
            var aclRules = TypeHelper.GetAllConcreteTypesAssignableFrom<ACLRule>().ToList();
            foreach (var rule in aclRules)
            {
                foreach (var operation in _getExplicitAclOperations.GetOperations(rule))
                {
                    yield return new AclInfo
                    {
                        Type = AclType.ExplicitRule,
                        Rule = rule.Name,
                        Operation = operation
                    };
                }
            }
        }

        private static IEnumerable<AclInfo> GetControllerOptions(string controllerName, IEnumerable<ControllerActionDescriptor> descriptors)
        {
            yield return new AclInfo
            {
                Type = AclType.Controller,
                Rule = controllerName,
                Operation = "All"
            };

            var actions = descriptors.Select(x => x.ActionName).Distinct().ToList();
            if (actions.Count <= 1)
                yield break;

            foreach (var action in actions)
                yield return new AclInfo
                {
                    Type = AclType.Action,
                    Rule = controllerName,
                    Operation = action
                };
        }
    }

    public interface IGetExplicitAclOperations
    {
        IEnumerable<string> GetOperations(Type type);
    }

    public class GetExplicitAclOperations : IGetExplicitAclOperations
    {
        private readonly IServiceProvider _serviceProvider;

        public GetExplicitAclOperations(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IEnumerable<string> GetOperations(Type type)
        {
            if (!type.IsImplementationOf(typeof(ACLRule)))
                return Enumerable.Empty<string>();

            var customResolverType = TypeHelper
                .GetAllConcreteTypesAssignableFrom(typeof(AclOperationResolver<>).MakeGenericType(type))
                .FirstOrDefault();
            var resolver = customResolverType != null
                ? _serviceProvider.GetService(customResolverType) as IAclOperationResolver
                : _serviceProvider.GetService(typeof(DefaultAclOperationResolver<>).MakeGenericType(type)) as
                    IAclOperationResolver;

            return resolver?.GetOperations() ?? Enumerable.Empty<string>();
        }
    }


    public class DefaultAclOperationResolver<TAclRule> : AclOperationResolver<TAclRule> where TAclRule : ACLRule
    {
        public override IEnumerable<string> GetOperations()
        {
            return typeof(TAclRule)
                .GetTypeInfo()
                .GetFields()
                .Where(x => x.IsPublic)
                .Select(x => x.GetRawConstantValue() as string)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }
    }

    public interface IAclOperationResolver
    {
        IEnumerable<string> GetOperations();
    }

    public abstract class AclOperationResolver<TAclRule> : IAclOperationResolver where TAclRule : ACLRule
    {
        public abstract IEnumerable<string> GetOperations();
    }
}