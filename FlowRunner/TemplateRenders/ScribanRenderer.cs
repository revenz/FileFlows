using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Scriban;
using Scriban.Runtime;

namespace FileFlows.FlowRunner.TemplateRenders;

/// <summary>
/// Renderer for scriban
/// </summary>
public class ScribanRenderer: ITemplateRenderer
{
    /// <inheritdoc />
    public string Render(NodeParameters args, string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if(text.IndexOf("{{", StringComparison.Ordinal) < 0)
        {
            // not a scriban template, use older variable replacements
            return args.ReplaceVariables(text, stripMissing: true);
        }
        try
        {
            string tcode = text;
            
            var dict = new Dictionary<string, object>();
            foreach(string key in args.Variables.Keys)
            {
                string newKey = key.Replace(".", "");
                tcode = tcode.Replace(key, "Variables." + newKey);
                if (dict.ContainsKey(newKey) == false)
                    dict.Add(newKey, args.Variables[key]);
            }

            if (VariablesHelper.StartedAt != null)
            {
                tcode = tcode.Replace("time.processing", "Variables.timeprocessing");
                tcode = tcode.Replace("time.processingRaw", "Variables.timeprocessingRaw");
                dict["timeprocessing"] = DateTime.Now.Subtract(VariablesHelper.StartedAt.Value).ToString();
                dict["timeprocessingRaw"] = DateTime.Now.Subtract(VariablesHelper.StartedAt.Value);
            }
            dict["timenow"] = DateTime.Now.ToShortTimeString();
            tcode = tcode.Replace("time.now", "Variables.timenow");
            // foreach (string k in args.Variables.Keys.OrderByDescending(x => x.Length))
            // {
            //     string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");
            //     string replacement = "Variables[\"" + k + "\"]";
            //     tcode = Regex.Replace(tcode, keyRegex, replacement);
            // }
            
            if(string.IsNullOrWhiteSpace(args.FailureReason))
            {
                tcode = tcode.Replace("FailureReason", "Variables.FailureReason");
                dict["FailureReason"] = args.FailureReason;
            }


            var scriptObject1 = new ScriptObject();
            scriptObject1.Import(typeof(FileFlowFunctions));
            scriptObject1.Add("Variables", dict);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject1);
            
            var template = Scriban.Template.Parse(tcode);
            if (template.HasErrors)
            {
                string errors = template.Messages.ToString();
                args.Logger?.ELog("Template Errors: " + errors);
                return text + "\n\n" + new string('-', 30) + "\n\n" + errors;
            }
            var rendered = template.Render(context);
            return rendered.Trim();
        }
        catch (Exception ex)
        {
            args.Logger?.ELog("Failed rendering template: " + ex.Message);
            return text + "\n\n" + new string('-', 30) + "\n\n" + ex.Message;
        }
    }
    
    

    /// <summary>
    /// Contains custom functions for FileFlows.
    /// </summary>
    public static class FileFlowFunctions
    {
        /// <summary>
        /// Formats a number as a file size with up to two decimal places and the largest unit.
        /// </summary>
        /// <param name="number">The number representing the file size in bytes.</param>
        /// <returns>A string representing the formatted file size.</returns>
        public static string FileSize(object number)
        {
            if (number == null || !(number is double || number is decimal || number is long || number is int || number is float))
            {
                return "0 Bytes"; // Handle invalid input gracefully
            }

            double bytes = Convert.ToDouble(number);

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            
            var order = 0;
            double num = bytes;
            while (num >= 1000 && order < sizes.Length - 1) {
                order++;
                num /= 1000;
            }
            return num.ToString("0.##") + ' ' + sizes[order];
        }
    }
}