# password_generator
A console application that generates secure passwords based off of a word list

## How to use:
1. Build project using Visual Studio (optionally rename executeable to "pswd_gen" for less typing)
2. Navigate to the directory with a shell and run the program to generate the config file "config.txt"
3. Execute the program with the parameter "-path" to find the path of the config.txt (unnecessary if using "-set")
4. Download your word list of choice and specify it in the "file" field of the config file 
   or run the progam with the parameter "-set" with the arguments "file" and the file name afterwards
5. Set your preferences for password generation using either the config file or the parameter "-set"
   followed by the arguments field name and value (or use the defaults)
6. Execute the program as many times as needed to generate a satisfactory password
