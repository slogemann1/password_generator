using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Password_Generator
{
    class Program
    {
        static bool errorFound = false;

        //Fields with default values
        static string file = "words.txt"; //Name of word list file; default: words.txt 
        static int minLength; //Between minWordLength and maxWordLength - 1; default: 5 or minWordLength
        static int maxLength; //Between minWordLength + 1 and maxWordLength; default: 10 or maxWordLength
        static int wordNum = 2; //Between 0 and 5; default: 2
        static string upperCaseConfig = "front"; //Possible values: front, end, random, none; default: front
        static string numberConfig = "between"; //Possible values: front, end, between, random, none; default: between 
        static string specialCharacterConfig = "between"; //Possible values: front, end, between, random, none; default: between
        static int wordSeperatorLength = 2; //Between 0 and 5; default: 2

        static void Main(string[] args)
        {
            if (!File.Exists("config.txt")) CreateConfigFile();

            if (HandleArgs(args) == 1) return; //Handle input arguments

            Field[] fields = GetFields(); //Parse config file for fields 

            if (!File.Exists(file))
            {
                Error(@"The file """ + file + @""" could not be found", false);
                Console.WriteLine("For help use the parameter -h or -help");
                return;
            }

            string[] words = File.ReadAllLines(file); //Array of all possible words

            //Find max/min word Lengths and set defaults
            int maxWordLength = 0;
            int minWordLength = 256;
            foreach (string word in words)
            {
                if (word.Length > maxWordLength) maxWordLength = word.Length;
                if (word.Length < minWordLength) minWordLength = word.Length;
            }
            minLength = (minWordLength > 5) ? minWordLength : 5;
            maxLength = (maxWordLength < 10) ? maxWordLength : 10;

            SetFields(fields, minWordLength, maxWordLength); //Get values from determined Fields

            if (wordSeperatorLength == 0)
            {
                if (numberConfig != "between") Error(@"The value of the field ""numberConfig"" will not be used as ""wordSeperatorLength"" is 0", true);
                if (specialCharacterConfig != "between") Error(@"The value of the field ""numberConfig"" will not be used as ""wordSeperatorLength"" is 0", true);
            }
            else if (numberConfig != "between" && specialCharacterConfig != "between")
            {
                Error(@"The value of the field ""wordSeperatorLength"" will not be used as no seperator characters were choosen", true);
            }

            string[] filteredWords = GetFilteredWords(words);

            if (errorFound) //If an error has occurred, stop the program
            {
                Console.WriteLine("For help use the parameter -h or -help");
                return;
            }

            //Creation of password:
            string[] randWords = GetRandomWords(filteredWords); //Get Random Words from filteredWords
            randWords = ModifyWords(randWords); //Modify Words based on upperCaseConfig
            string[] additions = GetWordAdditions(); //Get Additions to the words based on specialCharacterConfig and numberConfig
            string front = additions[0];
            string end = additions[additions.Length - 1];
            string[] seperators = additions.Skip(1).SkipLast(1).ToArray();

            //Combine words with front, seperators, and end into one password
            string finalWord = front + randWords[0];
            for (int i = 0; i < seperators.Length; i++)
            {
                finalWord += seperators[i] + randWords[i + 1];
            }
            finalWord += end;

            Console.WriteLine(finalWord); //Display Password
        }

        static int HandleArgs(string[] args)
        {
            if (args.Length == 0) return 0;
            else if (args[0].Trim() == "-h" || args[0].Trim() == "-help")
            {
                string msg = "";
                msg += "Help for Password Generator:\n";
                msg += "Password Generator will generate a random password based on the settings of the config file\n\n";
                msg += "Parameters:\n";
                msg += "\t\"-default\" or \"-d\": restores the default config file\n";
                msg += "\t\"-config\" or \"-c\": displays the current config file\n";
                msg += "\t\"-path\" or \"-p\": displays the absolute path of the config file\n";
                msg += "\t\"-set\" or \"-s\" [field] [value]: sets the value of the given field in the config file\n";
                msg += "\t\"-help\" or \"-h\": displays the help section for this program\n";
                Console.WriteLine(msg);
            }
            else if (args[0].Trim() == "-d" || args[0].Trim() == "-default") 
            {
                if (args.Length > 1) Console.WriteLine("The parameter \"-default\" expects no arguments");
                else CreateConfigFile(); 
            }
            else if (args[0].Trim() == "-c" || args[0].Trim() == "-config") 
            {
                if (args.Length > 1) Console.WriteLine("The parameter \"-config\" expects no arguments");
                else Console.WriteLine(File.ReadAllText("config.txt")); 
            }
            else if (args[0].Trim() == "-p" || args[0].Trim() == "-path") 
            {
                if (args.Length > 1) Console.WriteLine("The parameter \"-path\" expects no arguments");
                else Console.WriteLine(Path.GetFullPath("config.txt")); 
            }
            else if (args[0].Trim() == "-set" || args[0].Trim() == "-s")
            {
                if (args.Length != 3) Console.WriteLine("The parameter \"-set\" expects 2 arguments");
                else if (!EqualsAny(args[1], new string[] { "file", "minWordLength", "maxWordLength", "wordNumber", "upperCaseConfig", "numberConfig", "specialCharacterConfig", "wordSeperatorLength" }))
                {
                    Console.WriteLine("The given field name is not a valid field");
                }
                else
                {
                    string[] lines = File.ReadAllLines("config.txt");
                    bool found = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains('#'))
                        {
                            if (GetField(lines[i].Substring(0, lines[i].IndexOf('#')).Trim()).name == args[1])
                            {
                                lines[i] = args[1] + ": " + args[2] + " " + lines[i].Substring(lines[i].IndexOf('#'));
                                found = true;
                            }
                        }
                        else if (GetField(lines[i].Trim()).name == args[1])
                        {
                            lines[i] = args[1] + ": " + args[2];
                            found = true;
                        }
                    }

                    if (!found) File.AppendAllText("config.txt", args[1] + ": " + args[2]);
                    else
                    {
                        string newText = "";
                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            newText += lines[i] + "\n";
                        }
                        newText += lines[lines.Length - 1];
                        File.WriteAllText("config.txt", newText);
                    }
                }
            }
            else Console.WriteLine("The specified parameter was not found\nFor help use the parameter -h or -help");

            return 1;
        }
        static string[] GetWordAdditions()
        {
            Random rand = new Random();
            List<string> seperators = new List<string>();
            string front = "";
            string end = "";

            if (wordSeperatorLength != 0)
            {
                if (!(numberConfig == "between" && specialCharacterConfig == "between"))
                {
                    if (numberConfig != "none")
                    {
                        if (numberConfig == "front") front += rand.Next(1, 100);
                        if (numberConfig == "end") end += rand.Next(1, 100);
                        if (numberConfig == "between")
                        {
                            for (int i = 0; i < wordNum - 1; i++)
                            {
                                string sep = "";
                                for (int j = 0; j < wordSeperatorLength; j++) { sep += rand.Next(0, 10); }
                                seperators.Add(sep);
                            }
                        }
                    }
                    if (specialCharacterConfig != "none")
                    {
                        if (specialCharacterConfig == "front") front += RandomSpChar();
                        if (specialCharacterConfig == "end") end += RandomSpChar();
                        if (specialCharacterConfig == "between")
                        {
                            for (int i = 0; i < wordNum - 1; i++)
                            {
                                string sep = "";
                                for (int j = 0; j < wordSeperatorLength; j++) { sep += RandomSpChar(); }
                                seperators.Add(sep);
                            }
                        }
                    }
                }
                else //Combination of both seperator types
                {
                    for (int i = 0; i < wordNum - 1; i++)
                    {
                        string sep = "";
                        string sep2 = "";
                        for (int j = 0; j < wordSeperatorLength; j++) { sep += RandomSpChar(); }
                        for (int j = 0; j < wordSeperatorLength; j++) { sep2 += rand.Next(0, 10); }

                        string sepFinal = "";
                        for (int j = 0; j < sep.Length; j++) sepFinal += (rand.Next(0, 100) < 50) ? sep[j] : sep2[j];
                        seperators.Add(sepFinal);
                    }
                }
            }

            return (new string[] { front }).Concat(seperators).Append(end).ToArray();
        }

        static string[] ModifyWords(string[] randWords)
        {
            Random rand = new Random();
            if (upperCaseConfig != "none")
            {
                if (upperCaseConfig == "front")
                {
                    for (int i = 0; i < randWords.Length; i++) if (rand.Next(100) < 50) randWords[i] = (char)(randWords[i][0] - 32) + randWords[i].Substring(1);
                }
                else if (upperCaseConfig == "end")
                {
                    for (int i = 0; i < randWords.Length; i++) if (rand.Next(100) < 50) randWords[i] = randWords[i].Substring(0, randWords[i].Length - 1) + (char)(randWords[i][randWords[i].Length - 1] - 32);
                }
                else if (upperCaseConfig == "random")
                {
                    for (int i = 0; i < randWords.Length; i++)
                    {
                        char[] randWordArr = randWords[i].ToCharArray();
                        for (int j = 0; j < randWordArr.Length; j++) if (rand.Next(100) < 20) randWordArr[j] = (char)(randWordArr[j] - 32);
                        randWords[i] = new string(randWordArr);
                    }
                }
            }
            return randWords;
        }

        static string[] GetRandomWords(string[] filteredWords)
        {
            Random rand = new Random();
            string[] randWords = new string[wordNum];
            for (int i = 0; i < wordNum; i++)
            {
                randWords[i] = filteredWords[rand.Next(0, filteredWords.Length)];
            }
            return randWords;
        }

        static string[] GetFilteredWords(string[] words)
        {
            List<string> filteredWords = new List<string>();
            foreach (string word in words)
            {
                if (word.Length <= maxLength && word.Length >= minLength) filteredWords.Add(word);
            }

            if (filteredWords.Count == 0) Error("No words between specified minimum and maximum lengths were found", false);
            if (filteredWords.Count < 300) Error("Less than 300 words between specified minimum and maximum lengths were found which could result in a less safe password", false); //300 was choosen arbitrarily

            return filteredWords.ToArray();
        }

        static Field[] GetFields()
        {
            string[] lines = File.ReadAllLines("config.txt");
            List<Field> fields = new List<Field>();
            foreach (string line in lines) //Parse config into fields
            {
                if (line.Contains('#'))
                {
                    if (line.Substring(0, line.IndexOf('#')).Trim() == "") ;
                    else fields.Add(GetField(line.Substring(0, line.IndexOf('#'))));
                }
                else if (line.Trim() != "") fields.Add(GetField(line));

                if (fields.Count > 0 && fields[fields.Count - 1].name == "file") file = fields[fields.Count - 1].value; //check for word list file since it is needed earlier
            }

            return fields.ToArray();
        }

        static void SetFields(Field[] fields, int minWordLength, int maxWordLength)
        {
            foreach (Field field in fields)
            {
                if (field.name == "minWordLength")
                {
                    try
                    {
                        minLength = int.Parse(field.value);
                        if (minLength < minWordLength) Error(@"The value of the field ""minWordLength"" is less than the smallest word found", false);
                        if (minLength > maxWordLength) Error(@"The value of the field ""minWordLength"" is greater than the longest word found", false);
                    }
                    catch { Error(@"The value of the field ""minWordLength"" could not be resolved to a number", false); }
                }
                else if (field.name == "maxWordLength")
                {
                    try
                    {
                        maxLength = int.Parse(field.value);
                        if (maxLength < minWordLength) Error(@"The value of the field ""maxWordLength"" is less than the smallest word found", false);
                        if (maxLength > maxWordLength) Error(@"The value of the field ""maxWordLength"" is greater than the longest word found", false);
                        if (maxLength <= minLength) Error(@"The value of the field ""maxWordLength"" must be greater than ""minWordLength""", false);
                    }
                    catch { Error(@"The value of the field ""maxWordLength"" could not be resolved to a number", false); }
                }
                else if (field.name == "upperCaseConfig")
                {
                    if (EqualsAny(field.value, new string[] { "front", "end", "random", "none" })) upperCaseConfig = field.value;
                    else Error(@"The value of the field ""upperCaseConfig"" is not a valid option", false);
                }
                else if (field.name == "numberConfig")
                {
                    if (EqualsAny(field.value, new string[] { "front", "end", "between", "none" })) numberConfig = field.value;
                    else Error(@"The value of the field ""numberConfig"" is not a valid option", false);
                }
                else if (field.name == "specialCharacterConfig")
                {
                    if (EqualsAny(field.value, new string[] { "front", "end", "between", "none" })) specialCharacterConfig = field.value;
                    else Error(@"The value of the field ""specialCharacterConfig"" is not a valid option", false);
                }
                else if (field.name == "wordNumber")
                {
                    try
                    {
                        wordNum = int.Parse(field.value);
                        if (wordNum < 1) Error(@"The value of the field ""wordNum"" must be greater than 0", false);
                        else if (wordNum > 5) Error(@"The value of the field ""wordNum"" must be less than than 6", false);
                    }
                    catch { Error(@"The value of the field ""wordNumber"" could not be resolved to a number", false); }
                }
                else if (field.name == "wordSeperatorLength")
                {
                    try
                    {
                        wordSeperatorLength = int.Parse(field.value);
                        if (wordSeperatorLength < 0) Error(@"The value of the field ""wordSeperatorLength"" must be greater than or equal to 0", false);
                        else if (wordNum > 5) Error(@"The value of the field ""wordSeperatorLength"" must be less than than 6", false);
                    }
                    catch { Error(@"The value of the field ""wordSeperatorLength"" could not be resolved to a number", false); }
                }
                else if (field.name != "file")
                {
                    Error(@"The field """ + field.name + @""" is not a valid field and was ignored", true);
                }
            }
        }

        static void CreateConfigFile()
        {
            string contents = "#This is the configuration file for the password generator \"pswd_gen\"\n";
            contents += "#The information is formatted in the form of \"[fieldname]: [value]\"\n";
            contents += "#The \"#\" symbol denotes a comment which will be ignored\n\n";
            contents += "#The field file specifies the path of the word list file which should have words seperated by line\n";
            contents += "file: words.txt #Default value is \"words.txt\" which will likely need to be changed\n";
            contents += "#The field minWordLength can contain values between the determined minimum word length and the maximum - 1\n";
            contents += "minWordLength: 5 #Default value is 5\n";
            contents += "#The field maxWordLength can contain values between the determined minimum word length + 1 and the maximum word length\n";
            contents += "maxWordLength: 10 #Default value is 10\n";
            contents += "#The field wordNumber can contain values between 1 and 5\n";
            contents += "wordNumber: 2 #Default value is 2\n";
            contents += "#The field upperCaseConfig can contain the values \"front\", \"end\", \"random\", and \"none\"\n";
            contents += "upperCaseConfig: front #Default value is \"front\"\n";
            contents += "#The field numberConfig can contain the values \"front\", \"between\", \"end\", and \"none\"\n";
            contents += "numberConfig: between #Default value is \"between\"\n";
            contents += "#The field specialCharacterConfig can contain the values \"front\", \"between\", \"end\", and \"none\"\n";
            contents += "specialCharacterConfig: between #Default value is \"between\"\n";
            contents += "#The field wordSeperatorLength can contain values between 0 and 5\n";
            contents += "wordSeperatorLength: 2 #Default value is 2";

            File.WriteAllText("config.txt", contents);
        }

        static Field GetField(string field)
        {
            string value = "";
            string name = "";
            int i = 0;
            while (i < field.Length && field[i] != ':')
            {
                name += field[i];
                i++;
            }
            i++;
            while (i < field.Length)
            {
                value += field[i];
                i++;
            }
            return new Field { name = name.Trim(), value = value.Trim() };
        }

        static bool EqualsAny(string str, string[] possibilities)
        {
            foreach (string possibility in possibilities)
            {
                if (str == possibility) return true;
            }
            return false;
        }

        static char RandomSpChar()
        {
            List<char> chars = new List<char>();
            for (int i = 33; i <= 47; i++) chars.Add((char)i);
            for (int i = 58; i <= 64; i++) chars.Add((char)i);
            for (int i = 91; i <= 96; i++) chars.Add((char)i);
            for (int i = 123; i <= 126; i++) chars.Add((char)i);

            return chars[new Random().Next(0, chars.Count)];
        }

        static void Error(string error, bool warning)
        {
            errorFound = (warning) ? errorFound : true;

            Console.ForegroundColor = (warning) ? ConsoleColor.DarkMagenta : ConsoleColor.Red;
            Console.WriteLine((warning) ? "Warning: " + error : "Error: " + error);
            Console.ForegroundColor = ConsoleColor.White;
        }

        struct Field
        {
            public string name;
            public string value;
        }
    }
}
