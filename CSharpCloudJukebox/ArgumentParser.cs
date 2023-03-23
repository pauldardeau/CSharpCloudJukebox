namespace CSharpCloudJukebox;

public class ArgumentParser
{
   private const string TypeBool = "bool";
   private const string TypeInt = "int";
   private const string TypeString = "string";

   private readonly Dictionary<string, string> _dictAllReservedWords;
   private readonly Dictionary<string, string> _dictBoolOptions;
   private readonly Dictionary<string, string> _dictIntOptions;
   private readonly Dictionary<string, string> _dictStringOptions;
   private readonly Dictionary<string, string> _dictCommands;
   private readonly List<string> _listCommands;

   public ArgumentParser() {
      _dictAllReservedWords = new Dictionary<string, string>();
      _dictBoolOptions = new Dictionary<string, string>();
      _dictIntOptions = new Dictionary<string, string>();
      _dictStringOptions = new Dictionary<string, string>();
      _dictCommands = new Dictionary<string, string>();
      _listCommands = new List<string>();
   }

   private void AddOption(string o, string optionType, string help) {
      _dictAllReservedWords[o] = optionType;

      if (optionType == TypeBool) {
         _dictBoolOptions[o] = help;
      } else if (optionType == TypeInt) {
         _dictIntOptions[o] = help;
      } else if (optionType == TypeString) {
         _dictStringOptions[o] = help;
      }
   }

   public void AddOptionalBoolFlag(string flag, string help) {
      AddOption(flag, TypeBool, help);
   }

   public void AddOptionalIntArgument(string arg, string help) {
      AddOption(arg, TypeInt, help);
   }

   public void AddOptionalStringArgument(string arg, string help) {
      AddOption(arg, TypeString, help);
   }

   public void AddRequiredArgument(string arg, string help) {
      _dictCommands[arg] = help;
      _listCommands.Add(arg);
   }

   public Dictionary<string, object> ParseArgs(string[] args) {

      Dictionary<string, object> dictArgs = new Dictionary<string, object>();

      int numArgs = args.Length;
      bool working = true;
      int i = 0;
      int commandsFound = 0;

      if (numArgs == 0) {
         working = false;
      }

      while (working) {
         string arg = args[i];

         if (_dictAllReservedWords.ContainsKey(arg)) {
            string argType = _dictAllReservedWords[arg];
            arg = arg.Substring(2);
            if (argType == TypeBool) {
               //Console.WriteLine("adding key={0} value=true", arg);
               dictArgs[arg] = true;
            } else if (argType == TypeInt) {
               i++;
               if (i < numArgs) { 
                  string nextArg = args[i];
                  int intValue = Int32.Parse(nextArg);
                  //Console.WriteLine("adding key={0} value={1}", arg, intValue);
                  dictArgs[arg] = intValue;
               } else {
                  // missing int value
               }
            } else if (argType == TypeString) {
               i++;
               if (i < numArgs) {
                  string nextArg = args[i];
                  //Console.WriteLine("adding key={0} value={1}", arg, nextArg);
                  dictArgs[arg] = nextArg;
               } else {
                  // missing string value
               }
            } else {
               // unrecognized type
            }
         } else {
            if (arg.StartsWith("--")) {
               // unrecognized option
            } else {
               if (commandsFound < _listCommands.Count) {
                  string commandName = _listCommands[commandsFound];
                  //Console.WriteLine("adding key={0} value={1}", commandName, arg);
                  dictArgs[commandName] = arg;
                  commandsFound++;
               } else {
                  // unrecognized command
               }
            }
         }

         i++;
         if (i >= numArgs) {
            working = false;
         }
      }

      return dictArgs;
   }
}