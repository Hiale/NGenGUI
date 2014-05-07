/* Lightweight C# Command line parser
 *
 * Author  : Christian Bolterauer
 * Date    : 8-Aug-2009
 * Version : 1.0
 * Changes : 
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hiale.NgenGui.Helper
{
    /// <summary>
    /// Command Line Parser for creating and parsing command line options
    /// </summary>
    /// <remarks> Throws: MissingOptionException, DuplicateOptionException and if set InvalidOptionsException.
    /// </remarks>
    /// <seealso cref="Parse"/>
    /// <example> 
    ///   
    ///     //using CmdLine OUTDATED!
    /// 
    ///     //create parser
    ///     CmdLineParser parser = new CmdLineParser();
    ///     
    ///     //add default help "-help",..
    ///     parser.AddHelpOption();
    ///      
    ///     //add Option to parse
    ///     CmdLineParser.Option DebugOption = parser.AddBoolSwitch("-Debug", "Print Debug information");
    ///     
    ///     //add Alias option name
    ///     DebugOption.AddAlias("/Debug");
    /// 
    ///     CmdLineParser.NumberOption NegNumOpt = parser.AddDoubleParameter("-NegNum", "A required negativ Number", true);
    ///     
    ///     try
    ///     {
    ///         //parse 
    ///         parser.Parse(args);
    ///     }
    ///     catch (CmdLineParser.CMDLineParserException e)
    ///     {
    ///         Console.WriteLine("Error: " + e.Message);
    ///         parser.HelpMessage();
    ///     }
    ///     parser.Debug();    
    ///    
    ///</example>
    internal class CmdLineParser
    {
        protected const string IS_NOT_A_SWITCH_MSG = "The Switch name does not start with an switch identifier '-' or '/'  or contains space!";

        private readonly List<Option> _switchesStore;
        private string[] _cmdlineArgs;
        private Option _help;
        private readonly List<String> _invalidArgs;
        private readonly List<Option> _matchedSwitches;
        //private readonly ArrayList _matchedSwitches;
        private readonly List<string> _unmatchedArgs;

        private static readonly string[] SwitchPrefixes = {"/", "-"};

        public static bool AutoAddPrefix { get; set; }

        /// <summary>
        ///throw an exception if not matched (invalid) command line options were detected
        /// </summary>
        public bool ThrowInvalidOptionsException { get; set; }

        /// <summary>
        ///collect not matched (invalid) command line options as invalid args
        /// </summary>
        public bool CollectInvalidOptions { get; set; }

        public bool IsConsoleApplication { get; set; }

        public CmdLineParser()
        {
            _switchesStore = new List<Option>();
            _invalidArgs = new List<string>();
            //_matchedSwitches = new  ArrayList();
            _matchedSwitches = new List<Option>();
            _unmatchedArgs = new List<string>();
            CollectInvalidOptions = true;
            IsConsoleApplication = true;
            AutoAddPrefix = true;
        }

        /// <summary>
        /// Add a default help switch "-help","-h","-?","/help"
        /// </summary>
        public Option AddHelpOption()
        {
            _help = AddBoolSwitch("-help", "Command line help");
            _help.AddAlias("-h");
            _help.AddAlias("-?");
            _help.AddAlias("/help");
            return (_help);
        }

        /// <summary>
        /// Parses the command line and sets the values of each registered switch 
        /// or parameter option.
        /// </summary>
        /// <param name="args">The arguments array sent to Main(string[] args)</param>
        /// <returns>'true' if all parsed options are valid otherwise 'false'</returns>
        /// <exception cref="DuplicateOptionException"></exception>
        /// <exception cref="InvalidOptionsException"></exception>
        public bool Parse(string[] args)
        {
            Clear();
            for (var i = 0; i < args.Length; i++)
               args[i] = args[i].ToLower();
            _cmdlineArgs = args;
            ParseOptions();
            if (_invalidArgs.Count > 0)
            {
                if (ThrowInvalidOptionsException)
                {
                    var iopts = string.Empty;
                    foreach (string arg in _invalidArgs)
                    {
                        iopts += "'" + arg + "';";
                    }
                    throw new InvalidOptionsException("Invalid command line argument(s): " + iopts);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reset Parser and values of registed options.
        /// </summary>
        public void Clear()
        {
            _matchedSwitches.Clear();
            _unmatchedArgs.Clear();
            _invalidArgs.Clear();

            foreach (var s in _switchesStore)
                s.Clear();
        }

        /// <summary>
        /// Add (a custom) Option (Optional)
        /// </summary>
        /// <remarks> 
        /// To add instances (or subclasses) of 'CmdLineParser.Option'
        /// that implement: 
        /// <code>'public override object parseValue(string parameter)'</code>
        /// </remarks>
        /// <param name="opt">subclass from 'CmdLineParser.Option'</param>
        /// <seealso cref="AddBoolSwitch"/>
        /// <seealso cref="AddStringParameter"/>
        public void AddOption(Option opt)
        {
            //var switchName = string.Empty;
            //if (AutoAddPrefix)
            //{
            //    foreach (var switchPrefix in SwitchPrefixes.Where(switchPrefix => opt.Name.StartsWith(switchPrefix)))
            //        opt.Name = opt.Name.Remove(0, 1);
            //    switchName = opt.Name;
            //    opt.Name = SwitchPrefixes[0] + opt.Name;
            //}
            CheckCmdLineOption(opt.Name);
            //if (AutoAddPrefix)
            //{
            //    for (var i = 1; i < SwitchPrefixes.Length; i++)
            //        opt.AddAlias(SwitchPrefixes[i] + switchName);
            //}
            _switchesStore.Add(opt);
        }

        /// <summary>
        /// Add a basic command line switch. 
        /// (exist = 'true' otherwise 'false').
        /// </summary>
        public Option AddBoolSwitch(string name, string description)
        {
            var opt = new Option(name, description, typeof(bool), false, false);
            AddOption(opt);
            return opt;
        }

        /// <summary>
        /// Add a string parameter command line option.
        /// </summary>
        public Option AddStringParameter(string name, string description, bool required)
        {
            var opt = new Option(name, description, typeof (string), true, required);
            AddOption(opt);
            return opt;
        }

        /// <summary>
        /// Add a Integer parameter command line option.
        /// </summary>
        public NumberOption AddIntParameter(string name, string description, bool required)
        {
            var opt = new NumberOption(name, description, typeof (int), true, required) {NumberStyle = NumberStyles.Integer};
            AddOption(opt);
            return opt;
        }

        /// <summary>
        /// Add a Double parameter command line option.
        /// </summary>
        public NumberOption AddDoubleParameter(string name, string description, bool required)
        {
            var opt = new NumberOption(name, description, typeof (double), true, required) {NumberStyle = NumberStyles.Float};
            AddOption(opt);
            return (opt);
        }

        /// <summary>
        /// Add a Double parameter command line option.
        /// </summary>
        public NumberOption AddDoubleParameter(string name, string description, bool required, NumberFormatInfo numberformat)
        {
            var opt = new NumberOption(name, description, typeof (double), true, required) {NumberFormat = numberformat, ParseDecimalSeperator = false, NumberStyle = NumberStyles.Float | NumberStyles.AllowThousands};
            AddOption(opt);
            return (opt);
        }

        /// <summary>
        /// Check if name is a valid Option name
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="CmdLineParserException"></exception>
        private static void CheckCmdLineOption(string name)
        {
            if (!IsASwitch(name))
                throw new CmdLineParserException("Invalid Option: '" + name + "'");
        }

        protected static bool IsASwitch(string arg)
        {
            //var isValid = SwitchPrefixes.Any(arg.StartsWith);
            //return isValid & !arg.Contains(" ");
            return !arg.Contains(" ");
        }

        private void ParseOptions()
        {
            for (var i = 0; i < _cmdlineArgs.Length; i++)
            {
                var arg = _cmdlineArgs[i];
                var found = false;
                foreach (var s in _switchesStore)
                {
                    if (Compare(s, arg))
                    {
                        s.IsMatched = found = true;
                        _matchedSwitches.Add(s);
                        i = ProcessMatchedSwitch(s, _cmdlineArgs, i);
                    }
                }
                if (found == false)
                    ProcessUnmatchedArg(arg);
            }
            CheckReqired();
        }

        private void CheckReqired()
        {
            foreach (var s in _switchesStore)
            {
                if (s.IsRequired && (!s.IsMatched))
                    throw new MissingRequiredOptionException("Missing Required Option:'" + s.Name + "'");
            }
        }

        private static bool Compare(Option option, string arg)
        {
            if (!option.NeedsValue)
            {
                foreach (var optname in option.Names)
                {
                    if (optname.Equals(arg))
                    {
                        //option.Name = optname; //set name in case we match an alias name
                        return true;
                    }
                }
                return false;
            }
            foreach (var optname in option.Names)
            {
                if (arg.StartsWith(optname)) //if (arg.StartsWith(optname))
                {
                    CheckDuplicateAndSetName(option, optname);
                    return true;
                }
            }
            return false;
        }

        private static void CheckDuplicateAndSetName(Option s, string optname)
        {
            if (s.IsMatched && s.NeedsValue)
                throw new DuplicateOptionException("Duplicate: The Option: '" + optname + "' already exists on the comand line as '" + s.Name + "'");
            //s.Name = optname; //set name in case we match an alias name //NOT SURE IF IT HAS SIDEEFFECT IF IT'S COMMENTED OUT
        }

        private static int RetrieveParameter(ref string parameter, string optname, IList<string> cmdlineArgs, int pos)
        {
            if (cmdlineArgs[pos].Length == optname.Length) // arg must be in next cmdlineArg
            {
                if (cmdlineArgs.Count > pos + 1)
                {
                    pos++; //change command line index to next cmdline Arg.
                    parameter = cmdlineArgs[pos];
                }
            }
            else
            {
                parameter = (cmdlineArgs[pos].Substring(optname.Length));
            }
            return pos;
        }

        protected int ProcessMatchedSwitch(Option s, string[] cmdlineArgs, int pos)
        {
            //if help switch is matched give help .. only works for console apps
            if (s.Equals(_help))
            {
                if (IsConsoleApplication)
                {
                    Console.Write(HelpMessage());
                }
            }
            //process bool switch
            if (s.Type == typeof (bool))
            {
                ((IParsableOptionParameter)s).ParseValue(string.Empty);
                //string parameter = "";
                //pos = RetrieveParameter(ref parameter, s.Name, cmdlineArgs, pos);
                //s.Value = true;
               //((IParsableOptionParameter)s).ParseValue(parameter);
                return pos;
            }

            if (s.NeedsValue)
            {
                //retrieve parameter value and adjust pos
                string parameter = "";
                pos = RetrieveParameter(ref parameter, s.Name, cmdlineArgs, pos);
                //parse option using 'IParsableOptionParameter.parseValue(parameter)'
                //and set parameter value
                try
                {
                    if (s.Type != null)
                    {
                        //((IParsableOptionParameter) s).Value = ((IParsableOptionParameter) s).ParseValue(parameter);
                        ((IParsableOptionParameter) s).ParseValue(parameter);
                        return pos;
                    }
                }
                catch (Exception ex)
                {
                    throw new ParameterConversionException(ex.Message);
                }
            }
            //unsupported type ..
            throw new CmdLineParserException("Unsupported Parameter Type:" + s.Type);
        }

        protected void ProcessUnmatchedArg(string arg)
        {
            if (CollectInvalidOptions && IsASwitch(arg)) //assuming an invalid comand line option
            {
                _invalidArgs.Add(arg); //collect first, throw Exception later if set..
            }
            else
            {
                _unmatchedArgs.Add(arg);
            }
        }

        /// <summary>
        /// String array of remaining arguments not identified as command line options
        /// </summary>
        public String[] RemainingArgs()
        {
            if (_unmatchedArgs == null)
                return null;
            return _unmatchedArgs.ToArray();
        }

        /// <summary>
        /// String array of matched command line options
        /// </summary>
        public String[] MatchedOptions()
        {
            var names = new List<string>();
            for (int s = 0; s < _matchedSwitches.Count; s++)
                names.Add(_matchedSwitches[s].Name);
            return names.ToArray();
        }

        /// <summary>
        /// String array of not identified command line options
        /// </summary>
        public String[] InvalidArgs()
        {
            if (_invalidArgs == null)
                return null;
            return _invalidArgs.ToArray();
        }

        /// <summary>
        /// Create usage: A formated help message with a list of registered command line options.
        /// </summary>
        public string HelpMessage()
        {
            const string indent = "  ";
            int ind = indent.Length;
            const int spc = 3;
            int len = 0;
            foreach (var s in _switchesStore)
            {
                foreach (var name in s.Names)
                {
                    var nlen = name.Length;
                    if (s.NeedsValue) nlen += (" [..]").Length;
                    len = Math.Max(len, nlen);
                }
            }
            var help = "\nCommand line options are:\n\n";
            var req = false;
            foreach (var s in _switchesStore)
            {
                var line = indent + s.Names[0];
                if (s.NeedsValue) line += " [..]";
                while (line.Length < len + spc + ind)
                    line += " ";
                if (s.IsRequired)
                {
                    line += "(*) ";
                    req = true;
                }
                line += s.Description;

                help += line + "\n";
                if (s.Aliases != null && s.Aliases.Length > 0)
                {
                    foreach (string name in s.Aliases)
                    {
                        line = indent + name;
                        if (s.NeedsValue) line += " [..]";
                        help += line + "\n";
                    }
                }
                help += "\n";
            }
            if (req)
                help += "(*) Required.\n";
            return help;
        }

        // ReSharper disable LocalizableElement
        /// <summary>
        /// Print debug information of this CMDLineParser to the system console. 
        /// </summary>
        public void Debug()
        {
            Console.WriteLine();
            Console.WriteLine("\n------------- DEBUG CMDLineParser -------------\n");
            if (_switchesStore.Count > 0)
            {
                Console.WriteLine("There are {0} registered switches:", _switchesStore.Count);
                foreach (var s in _switchesStore)
                {
                    Console.WriteLine("Command : {0} : [{1}]", s.Names[0], s.Description);
                    Console.Write("Type    : {0} ", s.Type);
                    Console.WriteLine();

                    if (s.Aliases.Length  > 0)
                    {
                        Console.Write("Aliases : [{0}] : ", s.Aliases.Length);
                        foreach (string alias in s.Aliases)
                            Console.Write(" {0}", alias);
                        Console.WriteLine();
                    }
                    Console.WriteLine("Required: {0}", s.IsRequired);

                    Console.WriteLine("Value is: {0} \n", s.Value ?? "(Unknown)");
                }
            }
            else
            {
                Console.WriteLine("There are no registered switches.");
            }

            if (_matchedSwitches.Count > 0)
            {
                Console.WriteLine("\nThe following switches were found:");
                foreach (Option s in _matchedSwitches)
                    Console.WriteLine("  {0} Value:{1}", s.Name ?? "(Unknown)", s.Value ?? "(Unknown)");
            }
            else
                Console.WriteLine("\nNo Command Line Options detected.");
            Console.Write(InvalidArgsMessage());
            Console.WriteLine("\n----------- DEBUG CMDLineParser END -----------\n");
        }
        // ReSharper restore LocalizableElement

        private string InvalidArgsMessage()
        {
            const string indent = "  ";
            string msg = "";
            if (_invalidArgs != null)
            {
                msg += "\nThe following args contain invalid (unknown) options:";
                if (_invalidArgs.Count > 0)
                {
                    foreach (string s in _invalidArgs)
                        msg += "\n" + indent + s;
                }
                else
                    msg += "\n" + indent + "- Non -";
            }
            return msg + "\n";
        }

        /// <summary>
        /// Interface supporting parsing and setting of string parameter Values to objects
        /// </summary>
        private interface IParsableOptionParameter
        {
            /// <summary>
            /// Get or Set the value
            /// </summary>
            object Value { get; }

            /// <summary>
            /// parse string parameter to convert to an object
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns>an object</returns>
            object ParseValue(string parameter);
        }

        /// <summary>
        /// A comand line Option: A switch or a string parameter option.
        /// </summary>
        /// <remarks> Use AddBoolSwitch(..) or  AddStringParameter(..) (Factory) 
        /// Methods to create and store a new parsable 'CMDLineParser.Option'. 
        /// </remarks>
        public class Option : IParsableOptionParameter
        {
            private readonly bool _needsVal;
            private readonly Type _switchType;
            private readonly List<string> _names;
            private bool _matched;
            private string _name;
            private object _value;

            public Option(string name, string description, Type type, bool needsValue, bool required)
            {
                name = name.ToLower();
                _names = AnalizeName(name);
                _switchType = type;
                _needsVal = needsValue;
                IsRequired = required;
                //_name = _names[0];
                Description = description;
            }

            //getters and setters
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public string Description { get; set; }

            /// <summary>
            /// Object Type of Option Value (e.g. typeof(int))
            /// </summary>
            public Type Type
            {
                get { return _switchType; }
            }

            public bool NeedsValue
            {
                get { return _needsVal; }
            }

            public bool IsRequired { get; set; }

            /// <summary>
            /// set to 'true' if Option has been detected on the command line
            /// </summary>
            public bool IsMatched
            {
                get { return _matched; }
                set { _matched = value; }
            }

            public string[] Names
            {
                get { return _names.ToArray(); }
            }

            public string[] Aliases
            {
                get
                {
                    var list = new List<string>(Names);
                    list.RemoveAt(0); //remove 'name' (first element) from the list to leave aliases only
                    return list.ToArray();
                }
            }

            public object Value
            {
                get { return (_value); }
                private set { _value = value; }
            }

            private List<string> AnalizeName(string name)
            {
                var names = new List<string>();
                if (AutoAddPrefix)
                {
                    foreach (var switchPrefix in SwitchPrefixes)
                    {
                        if (name.StartsWith(switchPrefix))
                            name = name.Remove(0, switchPrefix.Length);
                    }
                    Name = name;
                    names.AddRange(SwitchPrefixes.Select(switchPrefix => switchPrefix + name));
                }
                else
                    names.Add(name);
                return names;
            }

            /// <summary>
            /// Default implementation of parseValue: 
            /// Subclasses should override this method to provide a method for converting
            /// the parsed string parameter to its Object type
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns>converted value</returns>
            /// <see cref="ParseValue"/>
            public virtual object ParseValue(string parameter)
            {
                //set string parameter
                if (Type == typeof (string) && NeedsValue)
                {
                    Value = parameter;
                    return parameter; //string needs no parsing (conversion) to string...
                }
                if (Type == typeof(bool))
                {
                    Value = true;
                    return true;
                }

                //throw Exception when parseValue has not been implemented by a subclass 
                throw new Exception("Option is missing an method to convert the value.");
            }

            public void AddAlias(string alias)
            {
                if (!IsASwitch(alias))
                    throw new CmdLineParserException("Invalid Option: '" + alias + "'");
                var aliasLow = alias.ToLower();
                if (AutoAddPrefix)
                {
                    foreach (var switchPrefix in SwitchPrefixes)
                    {
                        if (aliasLow.StartsWith(switchPrefix))
                            aliasLow = aliasLow.Remove(0, switchPrefix.Length);
                    }
                    foreach (var switchPrefix in SwitchPrefixes.Where(switchPrefix => !_names.Contains(switchPrefix + aliasLow)))
                    {
                        _names.Add(switchPrefix + aliasLow);
                    }
                }
                else
                {
                    if (!_names.Contains(aliasLow))
                        _names.Add(aliasLow);
                }
            }

            public void Clear()
            {
                _matched = false;
                _value = null;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// An command line option with a Number parameter.
        /// </summary>
        ///<remarks>
        /// To avoid unpredictable results on plattforms that use different 'Culture' settings 
        /// the default is set to 'invariant Culture' and parseDecimalSeperator=true;
        /// The number format can be changed for each CMDLineParser.NumberOption individually for
        /// more strict parsing.
        ///</remarks>
        public class NumberOption : Option
        {
            private NumberFormatInfo _numberformat;
            private NumberStyles _numberstyle;

            public NumberOption(string name, string description, Type type, bool hasval, bool required)
                : base(name, description, type, hasval, required)
            {
                NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
                ParseDecimalSeperator = true;
            }

            /// <summary>
            /// Get or Set the NumberFormat Information for parsing the parameter 
            /// </summary>
            public NumberFormatInfo NumberFormat
            {
                get { return _numberformat; }
                set { _numberformat = value; }
            }

            /// <summary>
            /// Get or Set the NumberStyle for parsing the parameter 
            /// </summary>
            public NumberStyles NumberStyle
            {
                get { return _numberstyle; }
                set { _numberstyle = value; }
            }

            /// <summary>
            /// If set to true the parser tries to detect and set the Decimalseparetor ("." or ",")
            /// automaticly. (default=true)
            /// </summary>
            public bool ParseDecimalSeperator { get; set; }

            public override object ParseValue(string parameter)
            {
                // int parameter
                if (Type == typeof(int))
                {
                    return ParseIntValue(parameter);
                }
                // double parameter
                if (Type == typeof(double))
                {
                    return ParseDoubleValue(parameter);
                }
                throw new ParameterConversionException("Invalid Option Type: " + Type);
            }

            //
            private int ParseIntValue(string parameter)
            {
                try
                {
                    return (Int32.Parse(parameter, _numberstyle, _numberformat));
                }
                catch (Exception e)
                {
                    throw new ParameterConversionException("Invalid Int Parameter:" + parameter + " - " + e.Message);
                }
            }

            //
            private double ParseDoubleValue(string parameter)
            {
                if (ParseDecimalSeperator) SetIdentifiedDecimalSeperator(parameter);
                try
                {
                    return (Double.Parse(parameter, _numberstyle, _numberformat));
                }
                catch (Exception e)
                {
                    throw new ParameterConversionException("Invalid Double Parameter:" + parameter + " - " + e.Message);
                }
            }

            //
            private void SetIdentifiedDecimalSeperator(string parameter)
            {
                if (_numberformat.NumberDecimalSeparator == "." && parameter.Contains(",") && !(parameter.Contains(".")))
                {
                    _numberformat.NumberDecimalSeparator = ",";
                    if (_numberformat.NumberGroupSeparator == ",") _numberformat.NumberGroupSeparator = ".";
                }
                else
                {
                    if (_numberformat.NumberDecimalSeparator == "," && parameter.Contains(".") &&
                        !(parameter.Contains(",")))
                    {
                        _numberformat.NumberDecimalSeparator = ".";
                        if (_numberformat.NumberGroupSeparator == ".") _numberformat.NumberGroupSeparator = ",";
                    }
                }
            }
        }

        #region Nested types: Exceptions

        /// <summary>
        /// Command line parsing Exception.
        /// </summary>
        public class CmdLineParserException : Exception
        {
            public CmdLineParserException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Thrown when duplicate option was detected
        /// </summary>
        public class DuplicateOptionException : CmdLineParserException
        {
            public DuplicateOptionException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Thrown when invalid (not registered) options have been detected
        /// </summary>
        public class InvalidOptionsException : CmdLineParserException
        {
            public InvalidOptionsException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Thrown when parameter value conversion to specified type failed 
        /// </summary>
        public class ParameterConversionException : CmdLineParserException
        {
            public ParameterConversionException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Thrown when required option was not detected
        /// </summary>
        public class MissingRequiredOptionException : CmdLineParserException
        {
            public MissingRequiredOptionException(string message) : base(message)
            {
            }
        }

        #endregion
    }
}