﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MrCMS.Website.Optimization
{
    public class ResourceBundler : IResourceBundler
    {
        public void AddScript(IHtmlHelper helper, string url)
        {
            var path = helper.ViewContext.View.Path;
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var scriptData = GetScriptData(helper.ViewContext.HttpContext);
            if (!scriptData.ContainsKey(path))
                scriptData[path] = new List<ResourceData>();
            scriptData[path].Add(ResourceData.Get(uri.IsAbsoluteUri, url));
        }

        private IDictionary<string, List<ResourceData>> GetScriptData(HttpContext context)
        {
            const string currentScriptlist = "current.scriptlist";
            if (context.Items[currentScriptlist] == null)
            {
                context.Items[currentScriptlist] =
                    new Dictionary<string, List<ResourceData>>();
            }
            return context.Items[currentScriptlist] as Dictionary<string, List<ResourceData>>;
        }

        public void AddCss(IHtmlHelper helper, string url)
        {
            var path = helper.ViewContext.View.Path;
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var cssData = GetStyleData(helper.ViewContext.HttpContext);
            if (!cssData.ContainsKey(path))
                cssData[path] = new List<ResourceData>();
            cssData[path].Add(ResourceData.Get(uri.IsAbsoluteUri, url));
        }
        private IDictionary<string, List<ResourceData>> GetStyleData(HttpContext context)
        {
            const string currentStylelist = "current.stylelist";
            if (context.Items[currentStylelist] == null)
            {
                context.Items[currentStylelist] =
                    new Dictionary<string, List<ResourceData>>();
            }
            return context.Items[currentStylelist] as Dictionary<string, List<ResourceData>>;
        }

        public void GetScripts(ViewContext viewContext)
        {
            foreach (string path in GetScriptData(viewContext.HttpContext).Values.SelectMany(x => x).Select(data => data.Url).Distinct())
            {
                viewContext.Writer.Write("<script src=\"{0}\" type=\"text/javascript\"></script>",
                    path.StartsWith("~") ? path.Substring(1) : path);
            }
        }

        public void GetCss(ViewContext viewContext)
        {
            foreach (string path in GetStyleData(viewContext.HttpContext).Values.SelectMany(x => x).Select(x => x.Url).Distinct())
            {
                viewContext.Writer.Write("<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />",
                    path.StartsWith("~") ? path.Substring(1) : path);
            }
        }
    }
}