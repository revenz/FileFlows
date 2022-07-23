using System.Text.RegularExpressions;

class CommandLine
{
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    public static CommandLine Parse(string[] args)
    {
        CommandLine cl = new CommandLine();
        for(int i=0;i<args.Length - 1;i++)
        {
            if(args[i].StartsWith("--") == false)
                continue;
            bool variable = args[i].StartsWith("--var:");
            string arg = args[i][(variable ? 6 : 2)..];
            object value;
            string strValue = args[++i];
            if(Regex.IsMatch(strValue, "^[\\d]+$") && int.TryParse(strValue, out int iValue))
                value = iValue;
            else if(strValue == "true")
                value = true;
            else if(strValue == "false")
                value = false;
            else
                value = strValue;
                
            if(variable)
                cl.Variables.Add(arg, value);
            else
                cl.Parameters.Add(arg, value);
        }
        return cl;
    }
}