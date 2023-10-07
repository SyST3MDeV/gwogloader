Scuffed ahh readme:

To build:

create a folder named "Dependencies", then copy all dll's from Wormtown's "Managed" directory into it

create a folder named Harmony, then copy the latest version of the libharmony dlls into it

then run "dotnet build -c Release" in the project root

the built DLL "wormtown.dll" needs to be hosted somewhere, and then change the GwogLoader endpoint in bootstrap.txt to point to it

to manually patch wormtown, open the Assembly-CSharp in dnspy, and replace the contents of the ConsoleToGUI class with the bootstrap.txt contents

yes it's scuffed I know I'm new to this ok